namespace HazardBackend.DbQueries.Validation;

public record ParseResult<T>(bool Success, T? Value, string[] Errors) where T: class;

