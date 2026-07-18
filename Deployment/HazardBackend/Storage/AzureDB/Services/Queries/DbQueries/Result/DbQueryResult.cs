using HazardBackend.DTOs;

namespace HazardBackend.Storage.AzureDB.Services.Queries.DbQueries.Result;

public sealed class DbQueryResult<T> where T : class
{
    public IReadOnlyList<T>? Data { get; init; } = null;
    public DBQueryErrorType ErrorType { get; init; } = DBQueryErrorType.None;
    public string? ErrorMessage { get; init; } = null;
    
    public bool Success => ErrorType == DBQueryErrorType.None;
}
