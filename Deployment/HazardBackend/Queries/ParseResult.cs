namespace HazardBackend.Queries;

public record ParseResult<T>(bool Success, T? Value, List<string> Errors) where T: class;

