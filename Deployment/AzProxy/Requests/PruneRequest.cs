namespace HazardBackend.Requests;

public record PruneRequest(bool PruneDemos, bool ForcePrune, int? DaysOffset);
