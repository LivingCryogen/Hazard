namespace HazardBackend.Storage.AzureDB.Services.Queries.Result;

public enum DBQueryErrorType
{
    None,
    NotFound,
    ConnectionError,
    Timeout,
    Unauthorized,
    QueryError,
    UnknownError
}
