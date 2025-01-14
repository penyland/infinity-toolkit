using Microsoft.Extensions.Logging;

namespace Infinity.Toolkit.TestUtils;

public sealed class XunitLoggerFactory(ITestOutputHelper testOutputHelper) : ILoggerFactory
{
    private readonly LoggerExternalScopeProvider scopeProvider = new();

    public void AddProvider(ILoggerProvider provider)
    {
        throw new NotImplementedException();
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new XunitLogger(testOutputHelper, scopeProvider, categoryName);
    }

    public void Dispose()
    {
    }

    public static ILogger<T> CreateLogger<T>(ITestOutputHelper testOutputHelper)
    {
        var loggerFactory = new LoggerFactory();
        loggerFactory.AddProvider(new XUnitLoggerProvider(testOutputHelper));
        return loggerFactory.CreateLogger<T>();
    }
}
