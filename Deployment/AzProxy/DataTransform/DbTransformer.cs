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

    public async Task TransformFromSessionDto(GameSessionDto sessionData)
    {
        // Errors collection, allowing logs and responses to accumulate and report all back when errors aren't fatal
        List<string> errorList = [];
        Dictionary<int, string> playerNumToNameMap;
        bool newGame = false; // Used to determine if PlayerStats should increment games started
        int prevLastAction = 0; // Used to determine the last action recorded for a player, to avoid double-counting
        Guid installId = sessionData.InstallId;
        int actionCount = sessionData.Attacks.Count + sessionData.Moves.Count + sessionData.Trades.Count;

        // Validate count integrity
        if (sessionData.NumActions != actionCount)
        {
            _logger.LogError("Action count mismatch for game {gameId}: expected {expected}, found {actual}",
                sessionData.Id, sessionData.NumActions, actionCount);
            throw new InvalidDataException($"Action count mismatch: expected {sessionData.NumActions}, found {actionCount}");
        }

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
            .Where(gs => gs.GameId == sessionData.Id && gs.InstallId == installId)
            .FirstOrDefaultAsync()
            is not GameSessionEntity previousSession)
        {
            newGame = true;
            var newSession = CreateNewGameSession(installId, sessionData);

            // Create AttackActions
            List<AttackActionEntity> newAttackActions = [];
            foreach (var attackAction in sessionData.Attacks)
            {
                var newAttackAction = CreateAttackAction(sessionData.Id, installId, attackAction, playerNumToNameMap);
                newAttackAction.GameSession = newSession;
                newAttackActions.Add(newAttackAction);
            }

            // Create MoveActions
            List<MoveActionEntity> newMoveActions = [];
            foreach (var moveAction in sessionData.Moves)
            {
                var newMoveAction = CreateMoveAction(sessionData.Id, installId, moveAction, playerNumToNameMap);
                newMoveAction.GameSession = newSession;
                newMoveActions.Add(newMoveAction);
            }

            // Create TradeActions
            List<TradeActionEntity> newTradeActions = [];
            foreach (var tradeAction in sessionData.Trades)
            {
                var newTradeAction = CreateTradeAction(sessionData.Id, installId, tradeAction, playerNumToNameMap);
                newTradeAction.GameSession = newSession;
                newTradeActions.Add(newTradeAction);
            }

            _context.GameSessions.Add(newSession);
            _context.AttackActions.AddRange(newAttackActions);
            _context.MoveActions.AddRange(newMoveActions);
            _context.TradeActions.AddRange(newTradeActions);
        }
        else // previous session found; if sync data is more up-to-date, update session
        {
            int previousSessionActions = previousSession.AttackActions.Count + previousSession.MoveActions.Count + previousSession.TradeActions.Count;
            if (previousSessionActions >= actionCount)
            {
                _logger.LogInformation("Game Session {gameID} on install {installID} already has {prevActions}, while sync has {syncActions} actions. Skipping.",
                    sessionData.Id, installId, previousSessionActions, actionCount);
                return;
            }

            if (!UpdateGameSession(previousSession, sessionData, playerNumToNameMap, errorList))
            {
                throw new InvalidOperationException($"Game Session {previousSession.GameId} with install {previousSession.InstallId} failed to update! Aborting...");
            }

            _logger.LogInformation("GSE for game {gameID} on install {installID} successfully updated.", previousSession.GameId, previousSession.InstallId);

            // More memory-efficient: avoids allocating a combined sequence via SelectMany.
            // Slightly more CPU work (3 Max calls), but better for large datasets or tight memory constraints.
            var lastActionIds = new int[3]
            {
                sessionData.Attacks.Select(t => t.ActionId).DefaultIfEmpty().Max(),
                sessionData.Trades.Select(t => t.ActionId).DefaultIfEmpty().Max(),
                sessionData.Moves.Select(t => t.ActionId).DefaultIfEmpty().Max()
            };

            prevLastAction = lastActionIds.Max();
        }

        // Create or Update PlayerStats for each Player in this new or updated Session
        foreach(var playerData in playerNumToNameMap)
        {
            if (await GetPlayerStats(installId, playerData.Value, errorList) is PlayerStatsEntity prevPlayerStats)
            {
                if (!UpdatePlayerStats(prevPlayerStats, sessionData, playerData.Key, playerData.Value, newGame, prevLastAction, errorList))
                {
                    errorList.Add($"Failed to Update stats for {prevPlayerStats.Name} with install ID {prevPlayerStats.InstallId}.");
                    _logger.LogWarning("Update failed for Player {plyrName} on install {installID}", prevPlayerStats.Name, prevPlayerStats.InstallId);
                    continue;
                }

                _logger.LogInformation("PSE for player {name} succesfully updated.", prevPlayerStats.Name);
            }
            else
                await _context.PlayerStats.AddAsync(CreateNewPlayerStats(installId, sessionData, playerData.Key, playerData.Value));
        }

        if (errorList.Count != 0)
            throw new PartialFailureException(
                $"Sync partially completed, with {errorList.Count} errors: {string.Join("; ", errorList)}", errorList);
        
        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully synced game session {gameId} for install {installId}",
                sessionData.Id, installId);
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


        Guid installId = sessionDto.InstallId;

        if (oldSession.InstallId != installId)
        {
            _logger.LogError("Game Session Update failed. Provided Game ID {dtoID} did not match existing Entity Game ID {entityID}.",
                oldSession.InstallId, sessionDto.InstallId);
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
                var newAttackAction = CreateAttackAction(sessionDto.Id, installId, attackAction, playerNumToNameMap);
                newAttackAction.GameSession = oldSession;
                _context.AttackActions.Add(newAttackAction);
            }

            // Create MoveActions
            foreach (var moveAction in sessionDto.Moves)
            {
                var newMoveAction = CreateMoveAction(sessionDto.Id, installId, moveAction, playerNumToNameMap);
                newMoveAction.GameSession = oldSession;
                _context.MoveActions.Add(newMoveAction);
            }

            // Create TradeActions
            foreach (var tradeAction in sessionDto.Trades)
            {
                var newTradeAction = CreateTradeAction(sessionDto.Id, installId, tradeAction, playerNumToNameMap);
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
    }

    private static AttackActionEntity CreateAttackAction(Guid gameID, Guid installID, AttackActionDto dto, Dictionary<int, string> playerNumToNameMap)
    {
        return new AttackActionEntity()
        {
            GameId = gameID,
            InstallID = installID,
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
    private static MoveActionEntity CreateMoveAction(Guid gameID, Guid installID, MoveActionDto dto, Dictionary<int, string> playerNumToNameMap)
    {
        return new MoveActionEntity()
        {
            GameId = gameID,
            InstallID = installID,
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
    private static TradeActionEntity CreateTradeAction(Guid gameID, Guid installID, TradeActionDto dto, Dictionary<int, string> playerNumToNameMap)
    {
        return new TradeActionEntity()
        {
            GameId = gameID,
            InstallID = installID,
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
    private async Task<PlayerStatsEntity?> GetPlayerStats(Guid installId, string name, List<string> errors)
    {
        try
        {
            return await _context.PlayerStats.Where(p => p.InstallId == installId && p.Name == name).FirstOrDefaultAsync();
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
            AttackDiceRolled = sessionDto.Attacks.Where(attack => attack.Player == playerNumber).Sum(a => a.AttackerDice),
            DefenseDiceRolled = sessionDto.Attacks.Where(attack => attack.Defender == playerNumber).Sum(a => a.DefenderDice),
            Moves = sessionDto.Moves.Count(move => move.Player == playerNumber),
            MaxAdvances = sessionDto.Moves.Count(move => move.Player == playerNumber && move.MaxAdvanced),
            TradeIns = sessionDto.Trades.Count(trade => trade.Player == playerNumber),
            TotalOccupationBonus = sessionDto.Trades.Where(trade => trade.Player == playerNumber).Sum(t => t.OccupiedBonus)
        };
    }
    private bool UpdatePlayerStats(PlayerStatsEntity playerStats, GameSessionDto sessionDto, int playerNumber, string playerName, bool newGame, int prevLastAction, List<string> errors)
    {
        if (playerStats.Name != playerName)
        {
            _logger.LogError("Player Stats Update failed. Provided Player Name ID {name} did not match existing Player Stats name {pseName}.",
                playerName, playerStats.Name);
            errors.Add($"Fetch Error for PSE. PSE with name {playerStats.Name} and install ID '{playerStats.InstallId}' was not found.");
            return false;
        }

        if (playerStats.IsDemo)
        {
            _logger.LogWarning("Player Stats Update was called on a demo Entity: {name} on install {install}", playerStats.Name, playerStats.InstallId);
            errors.Add($"PSE Updated a Demo Entity with name {playerStats.Name} and install ID '{playerStats.InstallId}'.");
        }

        try
        {
            if (newGame)
                playerStats.GamesStarted++;

            if (playerStats.LastGameStarted < sessionDto.StartTime)
            {
                playerStats.LastGameStarted = sessionDto.StartTime;
            }

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
                if (sessionDto.Winner == playerNumber)
                    playerStats.GamesWon++;
            }

            // Stat increases must be calculated using unique Action IDs, since we allow partial updates
            
            // Attack Stat Deltas
            var newAttacks = sessionDto.Attacks.Where(a => a.ActionId > prevLastAction);

            foreach(var attack in newAttacks)
            {
                if (attack.Player == playerNumber)
                {
                    switch (attack.AttackerLoss - attack.DefenderLoss)
                    {
                        case 0: playerStats.AttacksTied++; break;
                        case < 0: playerStats.AttacksLost++; break;
                        case > 0: playerStats.AttacksWon++; break;
                    }

                    if (attack.Conquered)
                        playerStats.Conquests++;
                    if (attack.Retreated)
                        playerStats.Retreats++;

                    playerStats.AttackDiceRolled += attack.AttackerDice;
                    playerStats.DefenseDiceRolled += attack.DefenderDice;
                }

                if (attack.Defender == playerNumber && attack.Retreated)
                {
                    playerStats.ForcedRetreats++;
                }
            }

            // Trade Stat Deltas
            var newTrades = sessionDto.Trades.Where(t => t.ActionId > prevLastAction && t.Player == playerNumber);

            foreach(var trade in newTrades)
            {
                playerStats.TradeIns++;
                playerStats.TotalOccupationBonus += trade.OccupiedBonus;
            }

            // Move Stat Deltas
            var newMoves = sessionDto.Moves.Where(m => m.ActionId > prevLastAction && m.Player == playerNumber);

            foreach(var move in newMoves)
            {
                playerStats.Moves++;
                if (move.MaxAdvanced)
                    playerStats.MaxAdvances++;
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
