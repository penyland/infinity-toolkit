using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Infinity.Toolkit.Tests.TestUtils;

public static class XunitLoggerProviderExtensions
{
    public static ILoggingBuilder AddXunit(this ILoggingBuilder builder, ITestOutputHelper testOutputHelper)
    {
        builder.Services.TryAddSingleton(testOutputHelper);

        builder.Services.AddSingleton<ILoggerProvider>(new XUnitLoggerProvider(testOutputHelper));
        return builder;
    }
}
