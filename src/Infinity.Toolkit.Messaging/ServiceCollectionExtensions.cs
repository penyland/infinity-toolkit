using Infinity.Toolkit.Messaging.Abstractions;
using Infinity.Toolkit.Messaging.Diagnostics;
using Infinity.Toolkit.Messaging.InMemory;
using Microsoft.Extensions.Hosting;

namespace Infinity.Toolkit.Messaging;

public static class ServiceCollectionExtensions
{
    private const string ConfigSectionPath = "Infinity:Messaging";

    /// <summary>
    /// Adds the messaging bus to the IServiceCollection with default settings.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static MessageBusBuilder AddInfinityMessaging(this IServiceCollection services) => services.AddInfinityMessaging(messageBusBuilder => { }, options => { });

    /// <summary>
    /// Adds the messaging bus to the host with default settings.
    /// </summary>
    /// <param name="builder">The host builder to add the message bus.</param>
    /// <returns>A MessageBusBuilder instance to further configure and build the message bus.</returns>
    public static MessageBusBuilder AddInfinityMessaging(this IHostApplicationBuilder builder) => builder.Services.AddInfinityMessaging(messageBusBuilder => { }, options => { });

    /// <summary>
    /// Adds the messaging bus to the IServiceCollection, and applies configurations specified in a delegate.
    /// </summary>
    /// <param name="builder">The host builder to add the message bus.</param>
    /// <param name="configureSettings">A delegate to configure the message bus options.</param>
    /// <param name="configSectionPath">Optional: The configuration section path to bind to the message bus options.</param>
    /// <returns>An MessageBusBuilder instance to build a message bus.</returns>
    public static MessageBusBuilder AddInfinityMessaging(this IHostApplicationBuilder builder, Action<MessageBusOptions> configureSettings, string configSectionPath = ConfigSectionPath) => builder.Services.AddInfinityMessaging(configureBus => { }, configureSettings, configSectionPath);

    /// <summary>
    /// Adds the messaging bus to the IServiceCollection, and applies configurations specified in a delegate.
    /// </summary>
    /// <param name="services">The service collection to register the Infinity Messaging bus with.</param>
    /// <param name="configureBusBuilder">A delegate to configure the message bus builder.</param>
    /// <returns></returns>
    public static MessageBusBuilder AddInfinityMessaging(this IServiceCollection services, Action<MessageBusBuilder> configureBusBuilder) => services.AddInfinityMessaging(configureBusBuilder, options => { });

    /// <summary>
    /// Adds the messaging bus to the IServiceCollection, and applies configurations specified in a delegate.
    /// </summary>
    /// <param name="services">The service collection to register the Infinity Messaging bus with.</param>
    /// <param name="configureSettings">A delegate to configure the message bus options.</param>
    /// <param name="configSectionPath">Optional: The configuration section path to bind to the message bus options.</param>
    /// <param name="configureBusBuilder">A delegate to configure the message bus builder.</param>
    /// <returns>An MessageBusBuilder instance to build a message bus.</returns>
    public static MessageBusBuilder AddInfinityMessaging(this IServiceCollection services, Action<MessageBusBuilder> configureBusBuilder, Action<MessageBusOptions> configureSettings, string configSectionPath = ConfigSectionPath)
    {
        services.ConfigureDefaults();

        services.AddOptions<MessageBusOptions>()
            .BindConfiguration(configSectionPath)
            .Configure(configureSettings)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var messageBusBuilder = new MessageBusBuilder(services);

        // Include InMemoryBus by default
        messageBusBuilder.ConfigureInMemoryBus();

        configureBusBuilder?.Invoke(messageBusBuilder);

        return messageBusBuilder;
    }

    /// <summary>
    /// Registers the message bus with the IServiceCollection.
    /// </summary>
    /// <param name="services">The service collection to register the message bus with.</param>
    /// <returns>The message bus builder.</returns>
    private static IServiceCollection ConfigureDefaults(this IServiceCollection services)
    {
        services.AddLogging();
        services.AddOptions();

        services.ConfigureOptions<ConfigureMessageBusOptions>();
        services.ConfigureOptions<ConfigureMessageBusBrokerOptions>();

        services.TryAddSingleton<IMessageBus, MessageBus>();
        services.TryAddSingleton<Metrics>();
        services.TryAddSingleton<MessagingExceptionHandler>();

        services.AddHostedService<MessageBusBackgroundService>();

        services.ConfigureOptions<ConfigureMessageBusOptions>();

        return services;
    }
}
