namespace HazardBackend.Storage.AzureDB.Services.Queries.DbQueries.Result;

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
