namespace HazardBackend.Storage.AzureDB.Services.Pruner;

public record PruneRequest(bool PruneDemos, bool ForcePrune, int? DaysOffset);
