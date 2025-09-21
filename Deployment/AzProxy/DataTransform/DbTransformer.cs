using AzProxy.Context;
using AzProxy.Entities;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AzProxy.DataTransform;

public class DbTransformer(GameStatsDbContext context, ILogger<DbTransformer> logger)
{
    private readonly GameStatsDbContext _context = context;
    private readonly ILogger<DbTransformer> _logger = logger;
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task TransformFromJson(string json, string installId, int trackedActions)
    {
        if (string.IsNullOrEmpty(json))
            throw new ArgumentException("Invalid JSON.");

        if (string.IsNullOrEmpty(installId))
            throw new ArgumentException("Invalid installation ID.");

        if (!Guid.TryParse(installId, out var installGuid))
            throw new ArgumentException("Invalid installation ID.");

        var sessionData = JsonSerializer.Deserialize<GameSessionDto>(json, _jsonSerializerOptions) ?? throw new InvalidDataException("Failed to deserialize GameSession from json.");

        int actualActions = sessionData.Attacks.Count + sessionData.Moves.Count + sessionData.Trades.Count;
        if (trackedActions > 0 && actualActions != trackedActions)
        {
            _logger.LogWarning("Action count mismatch for game {id} from install {inst}. Expected: {expect}, Actual {actual}.",
                sessionData.Id, installGuid, trackedActions, actualActions);
            trackedActions = actualActions;
        }

        // Errors collection, allowing logs and responses to accumulate and report all back when errors aren't fatal
        List<string> errorList = [];
        Dictionary<int, string> playerNumToNameMap;
        try
        {
            playerNumToNameMap = sessionData.PlayerNumsAndNames.ToDictionary(
                p => int.Parse(p.Key),
                p => p.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to parse player numbers from session data: {Message}", ex.Message);
            errorList.Add("GSE creation failed when player number to name mapping failed.");
            throw new InvalidOperationException("Player numbers list was invalid.", ex);
        }
        
        if (playerNumToNameMap == null || playerNumToNameMap.Count == 0)
        {
            logger.LogError("GSE creation failed when player numbers could not be read from GS dto.");
            errorList.Add("GSE creation failed when player number to name mapping failed.");
            throw new InvalidOperationException("Player numbers list was null.");
        }

        HashSet<string> sessionPlayerNames = [.. playerNumToNameMap.Values];

        /* Before updating, check if there is already a GameSessionEntity for this session. If the incoming number of actions 
        * (real SessionDto actions) is greater than the current Sessions action count, Update. Otherwise, don't. */

        // no previous Session found with this ID, create one
        if (await _context.GameSessions
            .Where(gs => gs.GameId == sessionData.Id && gs.InstallId == installGuid)
            .FirstOrDefaultAsync() 
            is not GameSessionEntity previousSession)
        {
            var newSession = CreateNewGameSession(installGuid, sessionData);

            // Create GamePlayers
            List<GamePlayerEntity> newGamePlayers = [];
            foreach (var dataPair in playerNumToNameMap)
            {
                var newGamePlayer = CreateNewGamePlayer(newSession.GameId, dataPair.Key, dataPair.Value);
                newGamePlayer.GameSession = newSession;
                newGamePlayers.Add(newGamePlayer);
            }

            // Create AttackActions
            List<AttackActionEntity> newAttackActions = [];
            foreach (var attackAction in sessionData.Attacks)
            {
                var newAttackAction = CreateAttackAction(sessionData.Id, attackAction, playerNumToNameMap);
                newAttackAction.GameSession = newSession;
                newAttackActions.Add(newAttackAction);
            }

            // Create MoveActions
            List<MoveActionEntity> newMoveActions = [];
            foreach (var moveAction in sessionData.Moves)
            {
                var newMoveAction = CreateMoveAction(sessionData.Id, moveAction, playerNumToNameMap);
                newMoveAction.GameSession = newSession;
                newMoveActions.Add(newMoveAction);
            }

            // Create TradeActions
            List<TradeActionEntity> newTradeActions = [];
            foreach (var tradeAction in sessionData.Trades)
            {
                var newTradeAction = CreateTradeAction(sessionData.Id, tradeAction, playerNumToNameMap);
                newTradeAction.GameSession = newSession;
                newTradeActions.Add(newTradeAction);
            }

            _context.GameSessions.Add(newSession);
            _context.AttackActions.AddRange(newAttackActions);
            _context.MoveActions.AddRange(newMoveActions);
            _context.TradeActions.AddRange(newTradeActions);
            _context.GamePlayers.AddRange(newGamePlayers);
        }
        else // previous session found; if sync data is more up-to-date, update session
        {
            int previousSessionActions = previousSession.AttackActions.Count + previousSession.MoveActions.Count + previousSession.TradeActions.Count;
            if (previousSessionActions >= trackedActions)
            {
                _logger.LogInformation("Game Session {gameID} on install {installID} already has {prevActions}, while sync has {syncActions} actions. Skipping.",
                    sessionData.Id, installGuid, previousSessionActions, trackedActions);
                return;
            }

            if (!UpdateGameSession(previousSession, sessionData, playerNumToNameMap, errorList))
            {
                throw new InvalidOperationException($"Game Session {previousSession.GameId} with install {previousSession.InstallId} failed to update! Aborting...");
            }
        }

        var installPlayerList = await GetPlayerStats(installGuid, errorList);

        if (installPlayerList == null || installPlayerList.Count == 0) // no PlayerStats for this Install
        {
            string winnerName = string.Empty;
            if (sessionData.Winner is int winner && playerNumToNameMap.TryGetValue(winner, out var mappedName))
                winnerName = mappedName;

            foreach (var playerData in playerNumToNameMap)
            {
                await _context.PlayerStats.AddAsync(CreateNewPlayerStats(installGuid, sessionData, playerData.Key, playerData.Value));
                await _context.PlayerSessionProcesses.AddAsync(CreateProcessingRecord(installGuid, sessionData.Id, plyrStatsDto.Name, actualActions));
            }
        }
        else 
        {
            HashSet<string> installPlayerNames = [.. installPlayerList.Select(p => p.Name)];
            // new players
            var newPlayerNames = sessionPlayerNames.ExceptBy(installPlayerNames, name => name);
            var previousPlayerNames = sessionPlayerNames.IntersectBy(installPlayerNames, name => name);

            var newPlayerStats = sessionData.PlayerStats.Where(ps => newPlayerNames.Contains(ps.Name));
            foreach (var playerStats in newPlayerStats) {
                await _context.PlayerStats.AddAsync(CreateNewPlayerStats(installGuid, sessionData, playerStats));
                await _context.PlayerSessionProcesses.AddAsync(CreateProcessingRecord(installGuid, sessionData.Id, playerStats.Name, actualActions));
            }

            var updatingPlayerStats = installPlayerList.Where(ps => previousPlayerNames.Contains(ps.Name));

            foreach (var playerStats in updatingPlayerStats)
            {
                var updatingPlayerDto = sessionData.PlayerStats.FirstOrDefault(ps => ps.Name == playerStats.Name);
                if (updatingPlayerDto == null)
                {
                    _logger.LogWarning("Failed to find the PlayerStatsDto expected for Player {name}. Skipping update.", playerStats.Name);
                    errorList.Add($"Failed to find the PlayerStatsDto expected for Player {playerStats.Name}.");
                    continue;
                }

                // Fetch Processing Record for Update
                var processingRecord = await GetProcessingRecord(sessionData.Id, playerStats.Name, errorList);

                // If none is found, proceed with Update and create new Process Record
                if (processingRecord == null)
                {
                    if (!UpdatePlayerStats(playerStats, sessionData, updatingPlayerDto, playerNumToNameMap, errorList))
                    {
                        errorList.Add($"Failed to Update stats for {playerStats.Name} with install ID {playerStats.InstallId}.");
                        _logger.LogWarning("Update failed for Player {plyrName} on install {installID}", playerStats.Name, playerStats.InstallId);
                        continue;
                    }
                    await _context.PlayerSessionProcesses.AddAsync(CreateProcessingRecord(installGuid, sessionData.Id, playerStats.Name, actualActions));
                }
                // Otherwise, check if this Player Stat is as-or-more-up-to-date as incoming sync Data, and Update. Otherwise, Don't
                else if (processingRecord.ProcessedActions < actualActions)
                {
                    if (!UpdatePlayerStats(playerStats, sessionData, updatingPlayerDto, playerNumToNameMap, errorList))
                    {
                        errorList.Add($"Failed to Update stats for {playerStats.Name} with install ID {playerStats.InstallId}.");
                        _logger.LogWarning("Update failed for Player {plyrName} on install {installID}", playerStats.Name, playerStats.InstallId);
                        continue;
                    }
                    UpdateProcessingRecord(processingRecord, installGuid, actualActions);
                }
                else
                {
                    _logger.LogInformation("Player Stats update was skipped for player {name} from {gameID} because their PSE was already up-to-date.", playerStats.Name, sessionData.Id);
                }
            }
        }

        if (errorList.Count != 0)
        {
            throw new PartialFailureException($"Sync partially completed, with {errorList.Count} errors: {string.Join("; ", errorList)}", errorList);
        }

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully synced game session {gameId} for install {installId}",
                sessionData.Id, installGuid);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to save changes for game session {gameId}: {Message}",
                sessionData.Id, ex.Message);
            throw; // Re-throw so the caller knows the operation failed
        }
    }

    private static GameSessionEntity CreateNewGameSession(Guid installId, GameSessionDto sessionDto)
    {
        return new GameSessionEntity()
        {
            InstallId = installId,
            GameId = sessionDto.Id,
            IsDemo = false,
            Version = sessionDto.Version,
            StartTime = sessionDto.StartTime,
            EndTime = sessionDto.EndTime,
            Winner = sessionDto.Winner,
        };
    }

    private bool UpdateGameSession(GameSessionEntity oldSession, GameSessionDto sessionDto, Dictionary<int, string> playerNumToNameMap, List<string> errors)
    {
        if (oldSession.GameId != sessionDto.Id)
        {
            _logger.LogError("Game Session Update failed. Provided Game ID {dtoID} did not match existing Entity Game ID {entityID}.",
                oldSession.GameId, sessionDto.Id);
            return false;
        }

        if (oldSession.IsDemo)
        {
            _logger.LogWarning("Game Session Update was called on a demo Entity: {gameID} on install {install}", oldSession.GameId, oldSession.InstallId);
            errors.Add($"GSE Update was called on a Demo Entity with game ID {oldSession.GameId} and install ID '{oldSession.InstallId}'.");
            return false;
        }

        try
        {
            LogDataChanges(oldSession, sessionDto);

            oldSession.Version = sessionDto.Version;
            oldSession.StartTime = sessionDto.StartTime;
            oldSession.EndTime = sessionDto.EndTime;
            oldSession.Winner = sessionDto.Winner;

            // Updating by clearing / repopulating is cleaner than attempting granular updates (no need to worry about colleciton order, etc)
            // And we do this on dbContext level to avoid any change tracking confusions

            _context.RemoveRange(oldSession.AttackActions);
            _context.RemoveRange(oldSession.MoveActions);
            _context.RemoveRange(oldSession.TradeActions);

            // Create AttackActions
            foreach (var attackAction in sessionDto.Attacks)
            {
                var newAttackAction = CreateAttackAction(sessionDto.Id, attackAction, playerNumToNameMap);
                newAttackAction.GameSession = oldSession;
                _context.AttackActions.Add(newAttackAction);
            }

            // Create MoveActions
            foreach (var moveAction in sessionDto.Moves)
            {
                var newMoveAction = CreateMoveAction(sessionDto.Id, moveAction, playerNumToNameMap);
                newMoveAction.GameSession = oldSession;
                _context.MoveActions.Add(newMoveAction);
            }

            // Create TradeActions
            foreach (var tradeAction in sessionDto.Trades)
            {
                var newTradeAction = CreateTradeAction(sessionDto.Id, tradeAction, playerNumToNameMap);
                newTradeAction.GameSession = oldSession;
                _context.TradeActions.Add(newTradeAction);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("An unexpected error occurred while attempting to update Game Session Entity with Game ID '{gameID}': {Message}", oldSession.GameId, ex.Message);
            errors.Add($"Update Error on GSE with Game ID '{oldSession.GameId}': " + ex.Message);           
            return false;
        }
    }
    private void LogDataChanges(GameSessionEntity oldSession, GameSessionDto sessionDto)
    {
        // Log warnings for unexpected data changes; log information for typical/expected data updates
        if (sessionDto.Version != oldSession.Version)
        {
            _logger.LogWarning("Game {gameId} version changed from {oldVer} to {newVer}.",
                sessionDto.Id, oldSession.Version, sessionDto.Version);
        }
        if (sessionDto.StartTime != oldSession.StartTime)
        {
            _logger.LogWarning("Game {gameId} Start Time changed from {oldTime} to {newTime}.",
                sessionDto.Id, oldSession.StartTime, sessionDto.StartTime);
        }
        if (sessionDto.EndTime != oldSession.EndTime)
        {
            if (oldSession.EndTime != null)
            {
                _logger.LogWarning("Game {gameId} End Time unexpectedly changed from {oldTime} to {newTime}.",
                    sessionDto.Id, oldSession.EndTime, sessionDto.EndTime);
            }
            else
                _logger.LogInformation("Game {gameId} End Time updated from {oldTime} to {newTime}.",
                    sessionDto.Id, oldSession.EndTime, sessionDto.EndTime);
        }
        if (sessionDto.Winner != oldSession.Winner)
        {
            if (oldSession.Winner != null)
            {
                _logger.LogWarning("Game {gameId} Winner unexpectedly changed from {oldWinner} to {newWinner}.",
                    sessionDto.Id, oldSession.Winner, sessionDto.Winner);
            }
            else
                _logger.LogInformation("Game {gameId} Winner updated to {newWinner}.",
                    sessionDto.Id, sessionDto.Winner);
        }

        // Action count reduction has already been checked against, but plausibly a Dto could come in with different players
        var oldPlayerCount = oldSession.Players.Count;
        var newPlayerCount = sessionDto.PlayerNumsAndNames.Count;
        if (oldPlayerCount != newPlayerCount)
        {
            _logger.LogWarning("Game {gameId} player count unexpectedly changed from {oldCount} to {newCount}.",
                sessionDto.Id, oldPlayerCount, newPlayerCount);
        }
    }

    private static GamePlayerEntity CreateNewGamePlayer(Guid gameID, int playerNumber, string playerName)
    {
        return new GamePlayerEntity()
        {
            GameId = gameID,
            PlayerNumber = playerNumber,
            Name = playerName,
            IsDemo = false
        };
    }

    private static AttackActionEntity CreateAttackAction(Guid gameID, AttackActionDto dto, Dictionary<int, string> playerNumToNameMap)
    {
        return new AttackActionEntity()
        {
            GameId = gameID,
            ActionId = dto.ActionId,
            IsDemo = false,
            PlayerName = playerNumToNameMap.TryGetValue(dto.Player, out string? attacker) && !string.IsNullOrEmpty(attacker)
                ? attacker
                : throw new InvalidDataException($"Player {dto.Player} not found in number to name map."),
            DefenderName = playerNumToNameMap.TryGetValue(dto.Defender, out string? defender) && !string.IsNullOrEmpty(defender)
                ? defender
                : throw new InvalidDataException($"Player {dto.Defender} not found in number to name map."),
            SourceTerritory = dto.SourceTerritory,
            TargetTerritory = dto.TargetTerritory,
            AttackerDice = dto.AttackerDice,
            DefenderDice = dto.DefenderDice,
            AttackerInitialArmies = dto.AttackerInitialArmies,
            DefenderInitialArmies = dto.DefenderInitialArmies,
            AttackerLoss = dto.AttackerLoss,
            DefenderLoss = dto.DefenderLoss,
            Retreated = dto.Retreated,
            Conquered = dto.Conquered,
        };
    }

    private static MoveActionEntity CreateMoveAction(Guid gameID, MoveActionDto dto, Dictionary<int, string> playerNumToNameMap)
    {
        return new MoveActionEntity()
        {
            GameId = gameID,
            ActionId = dto.ActionId,
            IsDemo = false,
            PlayerName = playerNumToNameMap.TryGetValue(dto.Player, out string? mover) && !string.IsNullOrEmpty(mover)
                ? mover
                : throw new InvalidDataException($"Player {dto.Player} not found in number to name map."),
            SourceTerritory = dto.SourceTerritory,
            TargetTerritory = dto.TargetTerritory,
            MaxAdvanced = dto.MaxAdvanced
        };
    }

    private static TradeActionEntity CreateTradeAction(Guid gameID, TradeActionDto dto, Dictionary<int, string> playerNumToNameMap)
    {
        return new TradeActionEntity()
        {
            GameId = gameID,
            ActionId = dto.ActionId,
            IsDemo = false,
            PlayerName = playerNumToNameMap.TryGetValue(dto.Player, out string? trader) && !string.IsNullOrEmpty(trader)
                ? trader
                : throw new InvalidDataException($"Player {dto.Player} not found in number to name map."),
            CardTargets = string.Join(",", dto.CardTargets),
            TradeValue = dto.TradeValue,
            OccupiedBonus = dto.OccupiedBonus,
        };
    }

    private async Task<List<PlayerStatsEntity>> GetPlayerStats(Guid installId, List<string> errors)
    {
        try
        {
            return await _context.PlayerStats.Where(p => p.InstallId == installId).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("There was an unexpected error while fetching PlayerStatsEntity associated with install ID {id}: {Message}", installId, ex.Message);
            errors.Add($"Fetch Error on PSE with install '{installId}': " + ex.Message);
            throw;
        }
    }

    private static PlayerStatsEntity CreateNewPlayerStats(Guid installId, GameSessionDto sessionDto, int playerNumber, string playerName)
    {
        return new PlayerStatsEntity()
        {
            InstallId = installId,
            IsDemo = false,
            FirstGameStarted = sessionDto.StartTime,
            FirstGameCompleted = sessionDto.EndTime != null
                        ? sessionDto.EndTime : null,
            LastGameCompleted = sessionDto.EndTime,
            LastGameStarted = sessionDto.StartTime,
            TotalGamesDuration = sessionDto.EndTime.HasValue
                        ? sessionDto.EndTime.Value - sessionDto.StartTime : TimeSpan.Zero,
            Name = sessionDto.PlayerNumsAndNames.ContainsValue(playerName) ? playerName : throw new InvalidDataException($"PSE creation attempted with {playerName}, which was not found in player nums and names of Game {sessionDto.Id} "),
            GamesStarted = 1,
            GamesCompleted = sessionDto.EndTime == null ? 0 : 1,
            GamesWon = sessionDto.Winner == playerNumber ? 1 : 0,
            AttacksWon = sessionDto.Attacks.Count(attack => attack.Player == playerNumber && attack.AttackerLoss < attack.DefenderLoss),
            AttacksLost = sessionDto.Attacks.Count(attack => attack.Player == playerNumber && attack.AttackerLoss > attack.DefenderLoss),
            AttacksTied = sessionDto.Attacks.Count(attack => attack.Player == playerNumber && attack.AttackerLoss == attack.DefenderLoss),
            Conquests = sessionDto.Attacks.Count(attack => attack.Player == playerNumber && attack.Conquered),
            Retreats = sessionDto.Attacks.Count(attack => attack.Player == playerNumber && attack.Retreated),
            ForcedRetreats = sessionDto.Attacks.Count(attack => attack.Defender == playerNumber && attack.Retreated),
            Moves = sessionDto.Moves.Count(move => move.Player == playerNumber),
            MaxAdvances = sessionDto.Moves.Count(move => move.Player == playerNumber && move.MaxAdvanced),
            TradeIns = sessionDto.Trades.Count(trade => trade.Player == playerNumber),
            TotalOccupationBonus = sessionDto.Trades.Where(trade => trade.Player == playerNumber).Sum(t => t.OccupiedBonus)
        };
    }

    private bool UpdatePlayerStats(PlayerStatsEntity playerStats, GameSessionDto sessionDto, Dictionary<int, string> playerNumToNameMap, List<string> errors)
    {
        if (playerStats.Name != statsDto.Name)
        {
            _logger.LogError("Player Stats Update failed. Provided Player Name ID {dtoName} did not match existing Player Stats name {pseName}.",
                statsDto.Name, playerStats.Name);
            errors.Add($"Fetch Error for PSE. PSE with name {playerStats.Name} and install ID '{playerStats.InstallId}' was not found.");
            return false;
        }

        if (playerStats.IsDemo)
        {
            _logger.LogWarning("Player Stats Update was called on a demo Entity: {name} on install {install}", playerStats.Name, playerStats.InstallId);
            errors.Add($"PSE Updated a Demo Entity with name {playerStats.Name} and install ID '{playerStats.InstallId}'.");
            return false;
        }

        try
        {
            if (playerStats.LastGameStarted < sessionDto.StartTime)
            {
                playerStats.LastGameStarted = sessionDto.StartTime;
            }

            // If synced session was a completed game, check if it's already been received, then if not update game completion tracking data
            if (sessionDto.EndTime.HasValue)
            {
                if (playerStats.LastGameCompleted == null || sessionDto.EndTime > playerStats.LastGameCompleted)
                    playerStats.LastGameCompleted = sessionDto.EndTime;

                if (playerStats.FirstGameCompleted == null || sessionDto.EndTime < playerStats.FirstGameCompleted)
                    playerStats.FirstGameCompleted = sessionDto.EndTime;

                if (playerStats.FirstGameStarted < sessionDto.StartTime)
                    playerStats.FirstGameStarted = sessionDto.StartTime;

                playerStats.TotalGamesDuration += sessionDto.EndTime.Value - sessionDto.StartTime;
                playerStats.GamesCompleted++;
                if (sessionDto.Winner is int winnerNum)
                {
                    if (playerNumToNameMap.TryGetValue(winnerNum, out string? winnerName) && !string.IsNullOrEmpty(winnerName))
                    {
                        if (winnerName == playerStats.Name)
                            playerStats.GamesWon++;
                    }
                }
            }
            // Aggregate stat deltas from last update (not absolute values!)
            else
            {
                playerStats.AttacksWon += statsDto.AttacksWon - playerStats.AttacksWon >= 0
                    ? statsDto.AttacksWon - playerStats.AttacksWon
                    : throw new InvalidDataException($"Player {playerStats.Name} attempted to decrease AttacksWon property.");
                playerStats.AttacksLost += statsDto.AttacksLost - playerStats.AttacksLost >= 0
                    ? statsDto.AttacksLost - playerStats.AttacksLost
                    : throw new InvalidDataException($"Player {playerStats.Name} attempted to decrease AttacksLost property.");
                playerStats.Retreats += statsDto.Retreats - playerStats.Retreats >= 0
                    ? statsDto.Retreats - playerStats.Retreats
                    : throw new InvalidDataException($"Player {playerStats.Name} attempted to decrease Retreats property.");
                playerStats.ForcedRetreats += statsDto.ForcedRetreats - playerStats.ForcedRetreats >= 0
                    ? statsDto.ForcedRetreats - playerStats.ForcedRetreats
                    : throw new InvalidDataException($"Player {playerStats.Name} attempted to decrease ForcedRetreats property.");
                playerStats.Conquests += statsDto.Conquests - playerStats.Conquests >= 0
                    ? statsDto.Conquests - playerStats.Conquests
                    : throw new InvalidDataException($"Player {playerStats.Name} attempted to decrease Conquests property.");
                playerStats.TotalOccupationBonus += statsDto.TotalOccupationBonus - playerStats.TotalOccupationBonus >= 0
                    ? statsDto.TotalOccupationBonus - playerStats.TotalOccupationBonus
                    : throw new InvalidDataException($"Player {playerStats.Name} attempted to decrease TotalOccupationBonus property.");
                playerStats.TradeIns += statsDto.TradeIns - playerStats.TradeIns >= 0
                    ? statsDto.TradeIns - playerStats.TradeIns
                    : throw new InvalidDataException($"Player {playerStats.Name} attempted to decrease TradeIns property.");
                playerStats.Moves += statsDto.Moves - playerStats.Moves >= 0
                    ? statsDto.Moves - playerStats.Moves
                    : throw new InvalidDataException($"Player {playerStats.Name} attempted to decrease Moves property.");
                playerStats.MaxAdvances += statsDto.MaxAdvances - playerStats.MaxAdvances >= 0
                    ? statsDto.MaxAdvances - playerStats.MaxAdvances
                    : throw new InvalidDataException($"Player {playerStats.Name} attempted to decrease MaxAdvances property.");
            }
                
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("An unexpected error occurred while attempting to update Player Stats Entity with install ID '{install}' player name '{name}': {Message}", playerStats.InstallId, playerStats.Name, ex.Message);
            errors.Add($"Update Error on PSE with name {playerStats.Name} and install {playerStats.InstallId}: " + ex.Message);
            return false;
        }
    }
}
