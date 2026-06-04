namespace HazardBackend.DbQueries.Result;

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
