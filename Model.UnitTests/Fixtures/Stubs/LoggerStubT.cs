using Microsoft.Extensions.Logging;

namespace Model.Tests.Fixtures.Stubs;

public class LoggerStubT<T> : ILogger<T> where T : class
{
    public LogLevel LastLoggedLevel { get; private set; }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        LastLoggedLevel = logLevel;
    }
}
