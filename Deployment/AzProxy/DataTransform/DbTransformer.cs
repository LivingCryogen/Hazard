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

        // Player Name lookup
        Dictionary<int, string> playerNumToNameMap = sessionData.PlayerStats.ToDictionary(p => p.Number, p => p.Name);
        HashSet<string> sessionPlayerNames = [.. sessionData.PlayerStats.Select(p => p.Name)];

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
            if (newPlayerStats != null)
                foreach (var playerStats in newPlayerStats)
                    await _context.PlayerStats.AddAsync(CreateNewPlayerStats(installGuid, sessionData, playerStats));

            var updatingPlayerStats = installPlayerList.Where(ps => previousPlayerNames.Contains(ps.Name));
            if (updatingPlayerStats != null)
                
        }



    }

    private async Task<List<PlayerStatsEntity>> GetPlayerStats(Guid installId)
    {
        try
        {
            return await _context.PlayerStats.Where(p => p.InstallId == installId).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError("There was an unexpected error while fetching PlayerStatsEntity associated with install ID {id}: {Message}", installID, ex.Message);
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

}
