using HazardBackend.Storage.AzureDB.Services.Pruner;

namespace HazardBackend.Storage.AzureDB;

public interface IDatabaseManager
{
    public DateTime? LastPruneDate { get; }
    public bool Pruned { get; } 
    public int? PruneAfterDays { get; }

    public void InitializeLastPruneDate(DateTime lastPruneDate);
    public Task<DbQueryResult> HandleDatabaseQuery(string query);
    public bool ShouldPrune();
    public Task<bool> PruneAsync(PruneRequest pruneRequest);
}
