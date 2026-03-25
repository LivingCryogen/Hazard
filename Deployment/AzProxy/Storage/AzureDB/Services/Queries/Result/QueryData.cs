using HazardBackend.Storage.AzureDB.DataTransform.DTOs;

namespace HazardBackend.Storage.AzureDB.Services.Queries.Result;

// A "union type" to represent the various possible results of a query.
// This allows us to return different types of results from the same method while maintaining type safety.
public abstract record QueryData
{
    private QueryData() { }

    public sealed record PlayerStats(IReadOnlyList<PlayerStatsDto> Items) : QueryData;
    public sealed record GameStats(IReadOnlyList<GameSessionDto> Items) : QueryData;
}
