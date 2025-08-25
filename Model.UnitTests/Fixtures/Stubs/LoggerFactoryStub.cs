using Microsoft.Extensions.Logging;

namespace Model.Tests.Fixtures.Stubs;

public class LoggerFactoryStub : ILoggerFactory
{
    public void AddProvider(ILoggerProvider provider)
    {
        throw new NotImplementedException();
    }

    public ILogger CreateLogger(string categoryName)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
    }

    public static ILogger<T> CreateLogger<T>() where T : class
    {
        return new LoggerStubT<T>();
    }
}
