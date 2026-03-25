namespace HazardBackend.Storage.AzureDB.Services.Queries.Validation;

public record ParseResult<T>(bool Success, T? Value, string? Error) where T: class;

