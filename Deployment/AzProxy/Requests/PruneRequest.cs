namespace AzProxy.Requests;

public record PruneRequest(bool PruneDemos, bool ForcePrune, int? DaysOffset);
