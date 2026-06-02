namespace HazardBackend.Storage.AzureDB.Services.Queries.Result;

public record DbQueryResult(DBQueryErrorType Error, string? Message, DbQueryData? Data);
