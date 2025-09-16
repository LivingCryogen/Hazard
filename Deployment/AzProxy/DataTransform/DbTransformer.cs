using AzProxy.Context;
using AzProxy.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

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

        /* Before updating, check if there is already a GameSessionEntity for this session. If the incoming number of actions 
         * (real SessionDto actions) is greater than the current Sessions action count, Update. Otherwise, don't. */

        // Player Name lookup
        Dictionary<int, string> playerNumToNameMap = sessionData.PlayerStats.ToDictionary(p => p.Number, p => p.Name);
        HashSet<string> sessionPlayerNames = [.. sessionData.PlayerStats.Select(p => p.Name)];

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

            //// 1. Create all entities with proper relationships
            //// 2. Then add everything to context at once
            //_context.GameSessions.Add(newSession);
            //_context.AttackActions.AddRange(newAttackActions);
            //_context.MoveActions.AddRange(newMoveActions);
            //_context.TradeActions.AddRange(newTradeActions);
            //_context.GamePlayers.AddRange(newGamePlayers);

            //// 3. Save everything
            //await _context.SaveChangesAsync();

            await _context.GameSessions.AddAsync();
        }
        else
        {
            int previousSessionActions = previousSession.AttackActions.Count + previousSession.MoveActions.Count + previousSession.TradeActions.Count;
            if (previousSessionActions >= trackedActions)
            {
                _logger.LogInformation("Game Session {gameID} on install {installID} already has {prevActions}, while sync has {syncActions} actions. Skipping.",
                    sessionData.Id, installGuid, previousSessionActions, trackedActions);
                return;
            }

            if (!UpdateGameSession(previousSession, sessionData))
            {

            }
        }



        var installPlayerList = await GetPlayerStats(installGuid);

        if (installPlayerList == null || installPlayerList.Count == 0) // no PlayerStats for this Install
        {
            string winnerName = string.Empty;
            if (sessionData.Winner is int winner && _playerNumToNameMap.TryGetValue(winner, out var mappedName))
                winnerName = mappedName;
            
            foreach (var plyrStatsDto in sessionData.PlayerStats)
                await _context.PlayerStats.AddAsync(CreateNewPlayerStats(installGuid, sessionData, plyrStatsDto));
        }
        else 
        {
            HashSet<string> installPlayerNames = [.. installPlayerList.Select(p => p.Name)];
            // new players
            var newPlayerNames = _sessionPlayerNames.ExceptBy(installPlayerNames, name => name);
            var previousPlayerNames = _sessionPlayerNames.IntersectBy(installPlayerNames, name => name);

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
                    _logger.LogWarning("Update failed for Player {plyrName} on install {installID}", playerStats.Name, playerStats.InstallId);
                    continue;
                }
            }
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

    private bool UpdateGameSession(GameSessionEntity oldSession, GameSessionDto sessionDto)
    {
        if (oldSession.GameId != sessionDto.Id)
        {
            _logger.LogError("Game Session Update failed. Provided Game ID {dtoID} did not match existing Entity Game ID {entityID}.",
                oldSession.GameId, sessionDto.Id);
            return false;
        }

        try
        {
            oldSession.Players.Clear();
            

            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("An unexpected error occurred while attempting to update Game Session Entity with Game ID '{gameID}': {Message}", oldSession.GameId, ex.Message);
            return false;
        }
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
                if (playerStats.)
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
