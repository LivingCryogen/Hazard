namespace HazardBackend.Storage.AzureDB.Services.Queries.Result;

public record QueryResult(string? Error, QueryData? Data);
