namespace HazardBackend.Storage.AzureDB.Services.Queries.Result;

public record DbQueryResult(string? Error, DbQueryData? Data);
