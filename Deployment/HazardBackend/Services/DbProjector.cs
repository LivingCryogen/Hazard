using HazardBackend.Storage.AzureDB.Context;
using Microsoft.EntityFrameworkCore;

namespace HazardBackend.Services;

internal class DbProjector(IDbContextFactory<GameStatsDbContext> dbcFactory)
{
    private readonly IDbContextFactory<GameStatsDbContext> _dbcFactory = dbcFactory;

    internal async Task<IResult> TakeSnapshotsAsync(Guid updatedSessionID)
    {
        await using var db =
            await _dbcFactory.CreateDbContextAsync();

        // Session File Snapshot

        // Install Summary Snapshot

        // Global Snapshot (leaderboards)? - maybe conditional?

        // .AsNoTracking() <-- don't forget to add to DbQuery here

        //playerNumToNameMap = sessionData.PlayerNumsAndNames.ToDictionary(
        //    p => int.Parse(p.Key),
        //    p => p.Value);
    }
}
