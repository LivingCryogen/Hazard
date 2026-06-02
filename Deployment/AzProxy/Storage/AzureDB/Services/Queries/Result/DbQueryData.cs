using HazardBackend.DTOs;

namespace HazardBackend.Storage.AzureDB.Services.Queries.Result;

// A "union type" to represent the various possible results of a query.
// This allows us to return different types of results from the same method while maintaining type safety.
public abstract record DbQueryData
{
    private DbQueryData() { }

    public sealed record PlayerStats(IReadOnlyList<PlayerStatsDto> Items) : DbQueryData;
    public sealed record GameStats(IReadOnlyList<GameSessionDto> Items) : DbQueryData;
}
