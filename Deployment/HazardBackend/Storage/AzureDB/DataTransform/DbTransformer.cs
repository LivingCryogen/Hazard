using HazardBackend.DTOs;
using HazardBackend.Storage.AzureDB.Context;
using HazardBackend.Storage.AzureDB.Entities;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HazardBackend.Storage.AzureDB.DataTransform;

public class DbTransformer(GameStatsDbContext context, ILogger<DbTransformer> logger)
{
    public async Task TransformFromSessionDto(GameSessionDto sessionData)
    {
        // Errors collection, allowing logs and responses to accumulate and report all back when errors aren't fatal
        List<string> errorList = [];
        Dictionary<int, string> playerNumToNameMap;
        bool newGame = false; // Used to determine if PlayerStats should increment games started
        int prevLastAction = 0; // Used to determine the last action recorded for a player, to avoid double-counting
        Guid installId = sessionData.InstallId;
        int actionCount = sessionData.Attacks.Count + sessionData.Moves.Count + sessionData.Trades.Count;
        bool updating = false; // Used to check if transform is on an update path or a create path

        // Validate count integrity
        if (sessionData.NumActions != actionCount)
        {
            logger.LogError("Action count mismatch for game {gameId}: expected {expected}, found {actual}",
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
            logger.LogError("Failed to parse player numbers from session data: {Message}", ex.Message);
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
        if (await context.GameSessions
            .Where(gs => gs.GameId == sessionData.Id && gs.InstallId == installId)
            .FirstOrDefaultAsync()
            is not GameSessionEntity previousSession)
        {
            newGame = true;
            var newSession = CreateNewGameSession(installId, sessionData, sessionData.Winner.HasValue ? playerNumToNameMap[(int)sessionData.Winner] : null);

            // Create ClaimActions
            List<ClaimActionEntity> newClaimActions = [];
            foreach (var claimAction in sessionData.Claims)
            {
                var newClaimAction = CreateClaimAction(sessionData.Id, installId, claimAction, playerNumToNameMap);
                newClaimAction.GameSession = newSession;
                newClaimActions.Add(newClaimAction);
            }

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

            // Create AcquiredContinentEvents
            List<AcquiredContinentEventEntity> newAcquiredContinentEvents = [];
            foreach (var acquiredContinentEvent in sessionData.AcquiredContinents)
            {
                bool fromClaim = sessionData.Claims.Any(c => c.ActionId == acquiredContinentEvent.FromActionId);
                var newAcquiredContinentEvent = CreateAcquiredContinentEventEntity(sessionData.Id, installId, acquiredContinentEvent, playerNumToNameMap, fromClaim);
                newAcquiredContinentEvent.GameSession = newSession;
                newAcquiredContinentEvents.Add(newAcquiredContinentEvent);
            }

            context.GameSessions.Add(newSession);
            context.ClaimActions.AddRange(newClaimActions);
            context.AttackActions.AddRange(newAttackActions);
            context.MoveActions.AddRange(newMoveActions);
            context.TradeActions.AddRange(newTradeActions);
            context.AcquiredContinents.AddRange(newAcquiredContinentEvents);
        }
        else // previous session found; if sync data is more up-to-date, update session
        {
            int previousSessionActions = previousSession.ClaimActions.Count + previousSession.AttackActions.Count + previousSession.MoveActions.Count + previousSession.TradeActions.Count;
            if (previousSessionActions >= actionCount)
            {
                logger.LogInformation("Game Session {gameID} on install {installID} already has {prevActions}, while sync has {syncActions} actions. Skipping.",
                    sessionData.Id, installId, previousSessionActions, actionCount);
                return;
            }

            if (!UpdateGameSession(previousSession, sessionData, playerNumToNameMap, errorList))
            {
                throw new InvalidOperationException($"Game Session {previousSession.GameId} with install {previousSession.InstallId} failed to update! Aborting...");
            }

            logger.LogInformation("GSE for game {gameID} on install {installID} successfully updated.", previousSession.GameId, previousSession.InstallId);
            updating = true;

            // More memory-efficient: avoids allocating a combined sequence via SelectMany.
            // Slightly more CPU work (3 Max calls), but better for large datasets or tight memory constraints.
            var lastActionIds = new int[4]
            {
                sessionData.Claims.Select(t => t.ActionId).DefaultIfEmpty().Max(),
                sessionData.Attacks.Select(t => t.ActionId).DefaultIfEmpty().Max(),
                sessionData.Trades.Select(t => t.ActionId).DefaultIfEmpty().Max(),
                sessionData.Moves.Select(t => t.ActionId).DefaultIfEmpty().Max()
            };

            prevLastAction = lastActionIds.Max();
        }

        // Create PlayerIdentities, GameSessionPlayer junction records, and PlayerStats for each player in the session
        foreach (var playerData in playerNumToNameMap) // playerData.Key = player number, playerData.Value = player name
        {
            var prevIdentity = await GetPlayerIdentity(installId, playerData.Value, errorList);
            var prevGSPE = await GetGameSessionPlayerEntity(sessionData.Id, installId, playerData.Value);
            var prevPlayerStats = await GetPlayerStats(installId, playerData.Value, errorList);

            if (updating)
            {
                if (prevIdentity == null)
                {
                    logger.LogWarning("No existing Player Identity found for player {num} on session {session} with name '{name}' on install {install} when attempting to update;" +
                        "creating new Player Identity and associated Game Session Player junction record.",
                        playerData.Key, sessionData.Id, playerData.Value, installId);
                    await context.PlayerIdentities.AddAsync(CreateNewPlayerIdentity(installId, playerData.Value));
                }

                if (prevGSPE == null)
                {
                    logger.LogWarning("No existing Game Session Player Entity junction record found for player {num} on session {session} with name '{name}' on install {install} when attempting to update;" +
                        "creating new Game Session Player junction record.",
                        playerData.Key, sessionData.Id, playerData.Value, installId);
                    await context.GameSessionPlayers.AddAsync(CreateNewGameSessionPlayerEntity(sessionData.Id, installId, playerData.Value));
                }

                if (prevPlayerStats == null)
                {
                    logger.LogWarning("No existing Player Stats Entity found for player {num} on session {session} with name '{name}' on install {install} when attempting to update;" +
                        "creating new Player Stats Entity.",
                        playerData.Key, sessionData.Id, playerData.Value, installId);
                    await context.PlayerStats.AddAsync(CreateNewPlayerStats(installId, sessionData, playerData.Key, playerData.Value));
                }
                else
                {
                    UpdatePlayerStats(prevPlayerStats, sessionData, playerData.Key, playerData.Value, newGame, prevLastAction, errorList);
                }
            }
            else
            {
                if (prevIdentity == null)
                {
                    await context.PlayerIdentities.AddAsync(CreateNewPlayerIdentity(installId, playerData.Value));
                }
                else
                {
                    logger.LogDebug("Existing Player Identity found for player {num} on session {session}, skipping new Player Identity creation.", playerData.Key, sessionData.Id);
                }
                if (prevGSPE == null)
                {
                    await context.GameSessionPlayers.AddAsync(CreateNewGameSessionPlayerEntity(sessionData.Id, installId, playerData.Value));
                }
                else
                {
                    logger.LogWarning("Existing Game Session Player Entity junction record found for player {num} on session {session} with name '{name}' on install {install}" +
                        " when attempting to create new session; skipping creation.",
                        playerData.Key, sessionData.Id, playerData.Value, installId);
                }
                if (prevPlayerStats == null)
                {
                    await context.PlayerStats.AddAsync(CreateNewPlayerStats(installId, sessionData, playerData.Key, playerData.Value));
                }
                else
                {
                    UpdatePlayerStats(prevPlayerStats, sessionData, playerData.Key, playerData.Value, newGame, prevLastAction, errorList);
                }
            }
        }

        if (errorList.Count != 0)
            throw new PartialFailureException(
                $"Sync partially completed, with {errorList.Count} errors: {string.Join("; ", errorList)}", errorList);
        
        try
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Successfully synced game session {gameId} for install {installId}",
                sessionData.Id, installId);
        }
        catch (Exception ex)
        {
            logger.LogError("Failed to save changes for game session {gameId}: {Message}",
                sessionData.Id, ex.Message);
            throw; // Re-throw so the caller knows the operation failed
        }
    }

    private static GameSessionEntity CreateNewGameSession(Guid installId, GameSessionDto sessionDto, string? winnerName)
    {
        return new GameSessionEntity()
        {
            InstallId = installId,
            GameId = sessionDto.Id,
            IsDemo = false,
            Version = sessionDto.Version,
            StartTime = sessionDto.StartTime,
            EndTime = sessionDto.EndTime,
            WinnerName = winnerName,
        };
    }
    private bool UpdateGameSession(GameSessionEntity oldSession, GameSessionDto sessionDto, Dictionary<int, string> playerNumToNameMap, List<string> errors)
    {
        if (oldSession.GameId != sessionDto.Id)
        {
            logger.LogError("Game Session Update failed. Provided Game ID {dtoID} did not match existing Entity Game ID {entityID}.",
                oldSession.GameId, sessionDto.Id);
            return false;
        }


        Guid installId = sessionDto.InstallId;

        if (oldSession.InstallId != installId)
        {
            logger.LogError("Game Session Update failed. Provided Game ID {dtoID} did not match existing Entity Game ID {entityID}.",
                oldSession.InstallId, sessionDto.InstallId);
            return false;
        }

        if (oldSession.IsDemo)
        {
            logger.LogWarning("Game Session Update was called on a demo Entity: {gameID} on install {install}", oldSession.GameId, oldSession.InstallId);
            errors.Add($"GSE Update was called on a Demo Entity with game ID {oldSession.GameId} and install ID '{oldSession.InstallId}'.");
            return false;
        }

        string? newWinnerName = sessionDto.Winner.HasValue ? playerNumToNameMap[(int)sessionDto.Winner] : null;

        try
        {
            LogDataChanges(oldSession, sessionDto, newWinnerName);

            oldSession.Version = sessionDto.Version;
            oldSession.StartTime = sessionDto.StartTime;
            oldSession.EndTime = sessionDto.EndTime;
            oldSession.WinnerName = newWinnerName;

            // Updating by clearing / repopulating is cleaner than attempting granular updates (no need to worry about colleciton order, etc)
            // And we do this on dbContext level to avoid any change tracking confusions
            context.RemoveRange(oldSession.ClaimActions);
            context.RemoveRange(oldSession.AttackActions);
            context.RemoveRange(oldSession.MoveActions);
            context.RemoveRange(oldSession.TradeActions);

            // Create ClaimActions
            foreach (var claimAction in sessionDto.Claims)
            {

            }

            // Create AttackActions
            foreach (var attackAction in sessionDto.Attacks)
            {
                var newAttackAction = CreateAttackAction(sessionDto.Id, installId, attackAction, playerNumToNameMap);
                newAttackAction.GameSession = oldSession;
                context.AttackActions.Add(newAttackAction);
            }

            // Create MoveActions
            foreach (var moveAction in sessionDto.Moves)
            {
                var newMoveAction = CreateMoveAction(sessionDto.Id, installId, moveAction, playerNumToNameMap);
                newMoveAction.GameSession = oldSession;
                context.MoveActions.Add(newMoveAction);
            }

            // Create TradeActions
            foreach (var tradeAction in sessionDto.Trades)
            {
                var newTradeAction = CreateTradeAction(sessionDto.Id, installId, tradeAction, playerNumToNameMap);
                newTradeAction.GameSession = oldSession;
                context.TradeActions.Add(newTradeAction);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError("An unexpected error occurred while attempting to update Game Session Entity with Game ID '{gameID}': {Message}", oldSession.GameId, ex.Message);
            errors.Add($"Update Error on GSE with Game ID '{oldSession.GameId}': " + ex.Message);           
            return false;
        }
    }
    private void LogDataChanges(GameSessionEntity oldSession, GameSessionDto sessionDto, string? newWinnerName)
    {
        // Log warnings for unexpected data changes; log information for typical/expected data updates
        if (sessionDto.Version != oldSession.Version)
        {
            logger.LogWarning("Game {gameId} version changed from {oldVer} to {newVer}.",
                sessionDto.Id, oldSession.Version, sessionDto.Version);
        }
        if (sessionDto.StartTime != oldSession.StartTime)
        {
            logger.LogWarning("Game {gameId} Start Time changed from {oldTime} to {newTime}.",
                sessionDto.Id, oldSession.StartTime, sessionDto.StartTime);
        }
        if (sessionDto.EndTime != oldSession.EndTime)
        {
            if (oldSession.EndTime != null)
            {
                logger.LogWarning("Game {gameId} End Time unexpectedly changed from {oldTime} to {newTime}.",
                    sessionDto.Id, oldSession.EndTime, sessionDto.EndTime);
            }
            else
                logger.LogInformation("Game {gameId} End Time updated from {oldTime} to {newTime}.",
                    sessionDto.Id, oldSession.EndTime, sessionDto.EndTime);
        }
        if (newWinnerName != oldSession.WinnerName)
        {
            if (oldSession.WinnerName != null)
            {
                logger.LogWarning("Game {gameId} Winner unexpectedly changed from {oldWinner} to {newWinner}.",
                    sessionDto.Id, oldSession.WinnerName, sessionDto.Winner);
            }
            else
                logger.LogInformation("Game {gameId} Winner updated to {newWinner}.",
                    sessionDto.Id, sessionDto.Winner);
        }
    }
    
    private static ClaimActionEntity CreateClaimAction(Guid gameID, Guid installID, ClaimActionDto dto, Dictionary<int, string> playerNumToNameMap)
    {
        return new ClaimActionEntity()
        {
            GameId = gameID,
            InstallID = installID,
            ActionId = dto.ActionId,
            IsDemo = false,
            PlayerName = playerNumToNameMap.TryGetValue(dto.Player, out string? claimer) && !string.IsNullOrEmpty(claimer)
                ? claimer
                : throw new InvalidDataException($"Player {dto.Player} not found in number to name map."),
            ClaimedTerritory = dto.ClaimedTerritory
        };
    }
    private static AttackActionEntity CreateAttackAction(Guid gameID, Guid installID, AttackActionDto dto, Dictionary<int, string> playerNumToNameMap)
    {
        return new AttackActionEntity()
        {
            GameId = gameID,
            InstallID = installID,
            ActionId = dto.ActionId,
            IsDemo = false
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

    private static AcquiredContinentEventEntity CreateAcquiredContinentEventEntity(Guid gameID, Guid installID, AcquiredContinentEventDto dto, Dictionary<int, string> playerNumToNameMap, bool fromClaim)
    {
        return new AcquiredContinentEventEntity()
        {
            GameId = gameID,
            InstallId = installID,
            FromActionId = dto.FromActionId,
            IsDemo = false,
            Continent = dto.Continent,
            FromClaim = fromClaim,
            PrevOwner = dto.PrevOwner == -1 
                ? null 
                : (playerNumToNameMap.TryGetValue(dto.PrevOwner, out string? prevOwner) && !string.IsNullOrEmpty(prevOwner)
                    ? prevOwner
                    : throw new InvalidDataException($"Player {dto.PrevOwner} not found in number to name map.")),
            NewOwner = playerNumToNameMap.TryGetValue(dto.NewOwner, out string? newOwner) && !string.IsNullOrEmpty(newOwner)
                ? newOwner
                : throw new InvalidDataException($"Player {dto.NewOwner} not found in number to name map.")
        };
    }

    private async Task<PlayerStatsEntity?> GetPlayerStats(Guid installId, string name, List<string> errors)
    {
        try
        {
            return await context.PlayerStats.Where(p => p.InstallId == installId && p.Name == name).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("There was an unexpected error while fetching PlayerStatsEntity associated with install ID {id}: {Message}", installId, ex.Message);
            errors.Add($"Fetch Error on PSE with install '{installId}': " + ex.Message);
            throw;
        }
    }
    private async Task<PlayerIdentityEntity?> GetPlayerIdentity(Guid installId, string name, List<string> errors)
    {
        try
        {
            return await context.PlayerIdentities.Where(p => p.InstallId == installId && p.Name == name).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("There was an unexpected error while fetching PlayerIdentityEntity associated with install ID {id}: {Message}", installId, ex.Message);
            errors.Add($"Fetch Error on PIE with playher name '{name}' and install '{installId}': " + ex.Message);
            throw;
        }
    }

    private static PlayerIdentityEntity CreateNewPlayerIdentity(Guid installId, string playerName)
    {
        return new PlayerIdentityEntity()
        {
            InstallId = installId,
            Name = playerName,
            IsDemo = false
        };
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
            Name = sessionDto.PlayerNumsAndNames.ContainsValue(playerName) 
                ? playerName 
                : throw new InvalidDataException($"PSE creation attempted with {playerName}, which was not found in player nums and names of Game {sessionDto.Id} "),
            GamesStarted = 1,
            GamesCompleted = sessionDto.EndTime == null ? 0 : 1,
            GamesWon = sessionDto.Winner == playerNumber ? 1 : 0,
            AttacksWon = sessionDto.Attacks.Count(
                attack => attack.Player == playerNumber && 
                attack.AttackerLoss < attack.DefenderLoss),
            AttacksLost = sessionDto.Attacks.Count(
                attack => attack.Player == playerNumber &&
                attack.AttackerLoss > attack.DefenderLoss),
            AttacksTied = sessionDto.Attacks.Count(
                attack => attack.Player == playerNumber && 
                attack.AttackerLoss == attack.DefenderLoss),
            Conquests = sessionDto.Attacks.Count(
                attack => attack.Player == playerNumber && 
                attack.Conquered),
            TerritoriesLost = sessionDto.Attacks.Count(
                attack => attack.Defender == playerNumber && 
                attack.Conquered),
            Retreats = sessionDto.Attacks.Count(
                attack => attack.Player == playerNumber && 
                attack.Retreated),
            ForcedRetreats = sessionDto.Attacks.Count(
                attack => attack.Defender == playerNumber && 
                attack.Retreated),
            ContinentsClaimed = sessionDto.AcquiredContinents.Count(
                acq => acq.NewOwner == playerNumber && 
                sessionDto.Claims.Any(c => c.ActionId == acq.FromActionId)),
            ContinentsConquered = sessionDto.AcquiredContinents.Count(
                acq => acq.NewOwner == playerNumber && 
                !sessionDto.Claims.Any(c => c.ActionId == acq.FromActionId) && 
                !ContPreviouslyOwnedByPlayer(acq, sessionDto, playerNumber)),
            ContinentsLost = sessionDto.AcquiredContinents.Count(acq => acq.PrevOwner == playerNumber),
            ContinentsReacquired = sessionDto.AcquiredContinents.Count(
                acq => acq.NewOwner == playerNumber && 
                ContPreviouslyOwnedByPlayer(acq, sessionDto, playerNumber)),
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
            logger.LogError("Player Stats Update failed. Provided Player Name ID {name} did not match existing Player Stats name {pseName}.",
                playerName, playerStats.Name);
            errors.Add($"Fetch Error for PSE. PSE with name {playerStats.Name} and install ID '{playerStats.InstallId}' was not found.");
            return false;
        }

        if (playerStats.IsDemo)
        {
            logger.LogWarning("Player Stats Update was called on a demo Entity: {name} on install {install}", playerStats.Name, playerStats.InstallId);
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

                if (attack.Defender == playerNumber)
                {
                    if (attack.Retreated)
                        playerStats.ForcedRetreats++;
                    if (attack.Conquered)
                        playerStats.TerritoriesLost++;
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

            // Acquired Continent Event Stat Deltas
            var newAcquiredContinents = sessionDto.AcquiredContinents.Where(acq => acq.FromActionId > prevLastAction);

            foreach(var acq in newAcquiredContinents)
            {
                if (acq.NewOwner == playerNumber)
                {
                    if (sessionDto.Claims.Any(c => c.ActionId == acq.FromActionId))
                        playerStats.ContinentsClaimed++;
                    else if (!ContPreviouslyOwnedByPlayer(acq, sessionDto, playerNumber))
                        playerStats.ContinentsConquered++;
                    else
                        playerStats.ContinentsReacquired++;
                }
                if (acq.PrevOwner == playerNumber)
                    playerStats.ContinentsLost++;
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError("An unexpected error occurred while attempting to update Player Stats Entity with install ID '{install}' player name '{name}': {Message}", playerStats.InstallId, playerStats.Name, ex.Message);
            errors.Add($"Update Error on PSE with name {playerStats.Name} and install {playerStats.InstallId}: " + ex.Message);
            return false;
        }
    }
    
    private async Task<GameSessionPlayerEntity?> GetGameSessionPlayerEntity(Guid gameId, Guid installId, string playerName)
    {
        try
        {
            return await context.GameSessionPlayers.Where(p => p.GameId == gameId && p.InstallId == installId && p.PlayerName == playerName).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            logger.LogError("There was an unexpected error while fetching GameSessionPlayer junction record associated with game {gameId} on install ID {id} for player {plyrName}: {Message}",
                gameId, installId, playerName, ex.Message);
            throw;
        }
    }

    private static GameSessionPlayerEntity CreateNewGameSessionPlayerEntity(Guid gameId, Guid installId, string playerName)
    {
        return new GameSessionPlayerEntity()
        {
            GameId = gameId,
            InstallId = installId,
            PlayerName = playerName,
            IsDemo = false
        };
    }

    private static bool ContPreviouslyOwnedByPlayer(AcquiredContinentEventDto acq, GameSessionDto sessionDto, int playerNumber) =>
        sessionDto.AcquiredContinents.Any(prev =>
        prev.Continent == acq.Continent &&
        prev.NewOwner == playerNumber &&
        prev.FromActionId < acq.FromActionId);
}
