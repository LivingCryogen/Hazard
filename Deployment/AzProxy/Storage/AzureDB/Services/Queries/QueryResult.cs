namespace HazardBackend.Storage.AzureDB.Services.Queries;

public record QueryResult(string? Error, QueryData? Data);
