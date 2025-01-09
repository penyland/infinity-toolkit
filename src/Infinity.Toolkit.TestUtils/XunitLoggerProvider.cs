using Microsoft.Extensions.Logging;
using Xunit;

namespace Infinity.Toolkit.TestUtils;

public sealed class XUnitLoggerProvider(ITestOutputHelper testOutputHelper) : ILoggerProvider
{
    private readonly ITestOutputHelper testOutputHelper = testOutputHelper;
    private readonly LoggerExternalScopeProvider scopeProvider = new();

    public ILogger CreateLogger(string categoryName)
    {
        return new XunitLogger(testOutputHelper, scopeProvider, categoryName);
    }

    public void Dispose()
    {
    }
}
