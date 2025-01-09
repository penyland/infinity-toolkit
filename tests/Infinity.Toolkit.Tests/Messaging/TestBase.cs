using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Infinity.Toolkit.Tests.Messaging;

public class TestBase
{
    public static ServiceProvider ConfigureServiceProvider(Action<IServiceCollection> configure, ITestOutputHelper testOutputHelper)
    {
        var services = new ServiceCollection();
        services.AddLogging(loggingBuilder => loggingBuilder.AddXunit(testOutputHelper));
        services.AddMetrics();
        services.AddSingleton(CreateConfiguration([]));

        configure(services);
        return services.BuildServiceProvider();
    }

    public static ServiceProvider ConfigureServiceProvider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMetrics();

        configure(services);
        return services.BuildServiceProvider();
    }

    public static IConfiguration CreateConfiguration(Dictionary<string, string?> configurationValues) => new ConfigurationBuilder()
            .AddInMemoryCollection(configurationValues)
            .Build();
}

public class TestOptionsMonitor<T>(T options) : IOptionsMonitor<T>
    where T : class
{
    private readonly T options = options;

    public T CurrentValue => options;

    public T Get(string? name) => options;

    public IDisposable OnChange(Action<T, string> listener) => throw new NotImplementedException();
}
