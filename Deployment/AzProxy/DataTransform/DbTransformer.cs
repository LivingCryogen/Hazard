using AzProxy.Context;
using AzProxy.Entities;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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

        // Player Name lookup
        Dictionary<int, string> playerNumToNameMap = sessionData.PlayerStats.ToDictionary(p => p.Number, p => p.Name);
        HashSet<string> sessionPlayerNames = [.. sessionData.PlayerStats.Select(p => p.Name)];

        // Errors collection, allowing logs and responses to accumulate and report all back when errors aren't fatal
        List<string> errorList = [];

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
            foreach (var playerStat in sessionData.PlayerStats)
            {
                var newGamePlayer = CreateNewGamePlayer(newSession.GameId, playerStat.Name);
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

            if (!UpdateGameSession(previousSession, sessionData, playerNumToNameMap))
            {
                throw new InvalidOperationException($"Game Session {previousSession.GameId} with install {previousSession.InstallId} failed to update! Aborting...");
            }
        }

        var installPlayerList = await GetPlayerStats(installGuid);

        if (installPlayerList == null || installPlayerList.Count == 0) // no PlayerStats for this Install
        {
            string winnerName = string.Empty;
            if (sessionData.Winner is int winner && playerNumToNameMap.TryGetValue(winner, out var mappedName))
                winnerName = mappedName;
            
            foreach (var plyrStatsDto in sessionData.PlayerStats)
                await _context.PlayerStats.AddAsync(CreateNewPlayerStats(installGuid, sessionData, plyrStatsDto));
        }
        else 
        {
            HashSet<string> installPlayerNames = [.. installPlayerList.Select(p => p.Name)];
            // new players
            var newPlayerNames = sessionPlayerNames.ExceptBy(installPlayerNames, name => name);
            var previousPlayerNames = sessionPlayerNames.IntersectBy(installPlayerNames, name => name);

            var newPlayerStats = sessionData.PlayerStats.Where(ps => newPlayerNames.Contains(ps.Name));
            foreach (var playerStats in newPlayerStats)
                    await _context.PlayerStats.AddAsync(CreateNewPlayerStats(installGuid, sessionData, playerStats));

            var updatingPlayerStats = installPlayerList.Where(ps => previousPlayerNames.Contains(ps.Name));
            
            foreach(var playerStats in updatingPlayerStats)
            {
                var updatingPlayerDto = sessionData.PlayerStats.FirstOrDefault(ps => ps.Name == playerStats.Name);
                if (updatingPlayerDto == null)
                {
                    _logger.LogWarning("Failed to find the PlayerStatsDto expected for Player {name}. Skipping update.", playerStats.Name);
                    continue;
                }

                if (!UpdatePlayerStats(playerStats, sessionData, updatingPlayerDto))
                {
                    errorList.Add($"Failed to Update stats for {playerStats.Name} with install ID {playerStats.InstallId}.");
                    _logger.LogWarning("Update failed for Player {plyrName} on install {installID}", playerStats.Name, playerStats.InstallId);
                    continue;
                }
            }
        }

        if (errorList.Count != 0)
        {
            throw new PartialFailureException($"Sync completed, but with {errorList.Count} errors: {string.Join("; ", errorList)}", errorList);
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
            Version = sessionDto.Version,
            StartTime = sessionDto.StartTime,
            EndTime = sessionDto.EndTime,
            Winner = sessionDto.Winner,
        };
    }

    private bool UpdateGameSession(GameSessionEntity oldSession, GameSessionDto sessionDto, Dictionary<int, string> playerNumToNameMap)
    {
        if (oldSession.GameId != sessionDto.Id)
        {
            _logger.LogError("Game Session Update failed. Provided Game ID {dtoID} did not match existing Entity Game ID {entityID}.",
                oldSession.GameId, sessionDto.Id);
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
            _context.RemoveRange(oldSession.Players);
            _context.RemoveRange(oldSession.AttackActions);
            _context.RemoveRange(oldSession.MoveActions);
            _context.RemoveRange(oldSession.TradeActions);

            // Create GamePlayers
            foreach (var playerStat in sessionDto.PlayerStats)
            {
                var newGamePlayer = CreateNewGamePlayer(oldSession.GameId, playerStat.Name);
                newGamePlayer.GameSession = oldSession;
                _context.GamePlayers.Add(newGamePlayer);
            }

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

        // Action count reduction has already been checked against, but plausibly a Dto could come in with different playerstats
        var oldPlayerCount = oldSession.Players.Count;
        var newPlayerCount = sessionDto.PlayerStats.Count;
        if (oldPlayerCount != newPlayerCount)
        {
            _logger.LogWarning("Game {gameId} player count unexpectedly changed from {oldCount} to {newCount}.",
                sessionDto.Id, oldPlayerCount, newPlayerCount);
        }
    }
    private void LogDataChanges(PlayerStatsEntity oldPlayerStats, PlayerStatsDto)
    {

    }
    private static GamePlayerEntity CreateNewGamePlayer(Guid gameID, string playerName)
    {
        return new GamePlayerEntity()
        {
            GameId = gameID,
            Name = playerName
        };
    }

    private static AttackActionEntity CreateAttackAction(Guid gameID, AttackActionDto dto, Dictionary<int, string> playerNumToNameMap)
    {
        return new AttackActionEntity()
        {
            // Id is omitted, as it's auto-generated/incremented by EF Core
            GameId = gameID,
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
            // Id is omitted, as it's auto-generated/incremented by EF Core
            GameId = gameID,
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
            // Id is omitted, as it's auto-generated/incremented by EF Core
            GameId = gameID,
            PlayerName = playerNumToNameMap.TryGetValue(dto.Player, out string? trader) && !string.IsNullOrEmpty(trader)
                ? trader
                : throw new InvalidDataException($"Player {dto.Player} not found in number to name map."),
            CardTargets = string.Join(",", dto.CardTargets),
            TradeValue = dto.TradeValue,
            OccupiedBonus = dto.OccupiedBonus,
        };
    }

    private async Task<List<PlayerStatsEntity>> GetPlayerStats(Guid installId)
    {
        try
        {
            return await _context.PlayerStats.Where(p => p.InstallId == installId).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("There was an unexpected error while fetching PlayerStatsEntity associated with install ID {id}: {Message}", installId, ex.Message);
            throw;
        }
    }

    private static PlayerStatsEntity CreateNewPlayerStats(Guid installId, GameSessionDto sessionDto, PlayerStatsDto statsDto)
    {
        return new PlayerStatsEntity()
        {
            InstallId = installId,
            FirstGameStarted = sessionDto.StartTime,
            FirstGameCompleted = sessionDto.EndTime != null
                        ? sessionDto.EndTime : null,
            LastGameCompleted = sessionDto.EndTime,
            LastGameStarted = sessionDto.StartTime,
            TotalGamesDuration = sessionDto.EndTime.HasValue
                        ? sessionDto.EndTime.Value - sessionDto.StartTime : TimeSpan.Zero,
            Name = statsDto.Name,
            GamesStarted = 1,
            GamesCompleted = sessionDto.EndTime == null ? 0 : 1,
            GamesWon = sessionDto.Winner == statsDto.Number ? 1 : 0,
            AttacksWon = statsDto.AttacksWon,
            AttacksLost = statsDto.AttacksLost,
            Conquests = statsDto.Conquests,
            Retreats = statsDto.Retreats,
            ForcedRetreats = statsDto.ForcedRetreats,
            Moves = statsDto.Moves,
            MaxAdvances = statsDto.MaxAdvances,
            TradeIns = statsDto.TradeIns,
            TotalOccupationBonus = statsDto.TotalOccupationBonus
        };
    }

    private bool UpdatePlayerStats(PlayerStatsEntity playerStats, GameSessionDto sessionDto, PlayerStatsDto statsDto)
    {
        if (playerStats.Name != statsDto.Name)
        {
            _logger.LogError("Player Stats Entity Update failed. Provided Player Name ID {dtoName} did not match existing Player Stats name {pseName}.",
                statsDto.Name, playerStats.Name);
            return false;
        }

        try
        {
            playerStats.GamesStarted++;

            // If synced session was a completed game, update game completion tracking data
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
            }
            else
            {
                playerStats.
            }



                //sessionDto.e
                //sessionDto.EndTime.HasValue ? playerStats.Last

                //playerStats.FirstGameStarted == statsDto.Firs

                //FirstGameCompleted = sessionDto.EndTime != null
                //            ? sessionDto.EndTime : null,
                //LastGameCompleted = sessionDto.EndTime,
                //LastGameStarted = sessionDto.StartTime,
                //TotalGamesDuration = sessionDto.EndTime.HasValue
                //            ? sessionDto.EndTime.Value - sessionDto.StartTime : TimeSpan.Zero,
                //Name = statsDto.Name,
                //GamesStarted = 1,
                //GamesCompleted = sessionDto.EndTime == null ? 0 : 1,
                //GamesWon = sessionDto.Winner == statsDto.Number ? 1 : 0,
                //AttacksWon = statsDto.AttacksWon,
                //AttacksLost = statsDto.AttacksLost,
                //Conquests = statsDto.Conquests,
                //Retreats = statsDto.Retreats,
                //ForcedRetreats = statsDto.ForcedRetreats,
                //Moves = statsDto.Moves,
                //MaxAdvances = statsDto.MaxAdvances,
                //TradeIns = statsDto.TradeIns,
                //TotalOccupationBonus = statsDto.TotalOccupationBonus
                //return true;

                return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("An unexpected error occurred while attempting to update Player Stats Entity with install ID '{install}' player name '{name}': {Message}", playerStats.InstallId, playerStats.Name, ex.Message);
            return false;
        }
    }
}
