using HazardBackend.Storage.AzureDB.Context;
using HazardBackend.Storage.AzureDB.Entities;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using static System.Formats.Asn1.AsnWriter;

namespace HazardBackend.Storage.AzureDB.Services.Queries;

public class QueryHandler(GameStatsDbContext dbContext)
{
    private readonly HashSet<string> _leaderBoardPropertyNames = 
        [ 
            "Name",
            "GamesStarted",
            "GamesCompleted",
            "GamesWon",
            "FirstGameStarted",
            "FirstGameCompleted",
            "LastGameStarted",
            "LastGameCompleted",
            "TotalGamesDuration",
            "AttacksWon",
            "AttacksLost",
            "AttacksTied",
            "Conquests",
            "Retreats",
            "ForcedRetreats",
            "AttackDiceRolled",
            "DefenseDiceRolled",
            "Moves",
            "MaxAdvances",
            "TradeIns",
            "TotalOccupationBonus"
        ];



    public async Task<QueryResult> HandleQueryAsync(QueryType queryType, string[] queryParams)
    {
        switch (queryType)
        {
            case QueryType.Leaderboard: return await GetLeaderboard(dbContext, queryParams); 
            case QueryType.PlayerStats: return await GetPlayerStats(queryParams);

        }
    }

    private async Task<QueryResult> GetLeaderboard(string[] queryParams)
    {
        if (queryParams.Length == 0)
            return new QueryResult("Empty leaderboard query parameter; no data fetched.", null);

        if (string.IsNullOrEmpty(queryParams[0]))
            return new QueryResult("Empty leaderboard sort parameter; no data fetched.", null);

        if (!_leaderBoardPropertyNames.Contains(queryParams[0]))
            return new QueryResult($"Invalid leaderboard sort parameter '{queryParams[0]}'; no data fetched.", null);


        string sortPropertyName = queryParams[0];
        
    }
}
