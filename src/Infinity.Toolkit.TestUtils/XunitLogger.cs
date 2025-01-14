using Microsoft.Extensions.Logging;

namespace Infinity.Toolkit.TestUtils;

internal class XunitLogger(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider, string categoryName) : ILogger
{
    private readonly LoggerExternalScopeProvider scopeProvider = scopeProvider;
    private readonly string categoryName = categoryName;
    private readonly ITestOutputHelper testOutputHelper = testOutputHelper;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
        => scopeProvider.Push(state);

    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        var message = formatter(state, exception);
        try
        {
            testOutputHelper.WriteLine(message);
        }
        catch (Exception)
        {
        }
    }
}

internal sealed class XunitLogger<T>(ITestOutputHelper testOutputHelper, LoggerExternalScopeProvider scopeProvider) : XunitLogger(testOutputHelper, scopeProvider, typeof(T).Name), ILogger<T>
{
}
