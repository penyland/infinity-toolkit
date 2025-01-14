using Infinity.Toolkit.Messaging.Abstractions;
using Infinity.Toolkit.Messaging.Diagnostics;

namespace Infinity.Toolkit.Messaging;

public static class ServiceCollectionExtensions
{
    private const string ConfigSectionPath = "Infinity:Messaging";

    public static MessageBusBuilder AddInfinityMessaging(this IServiceCollection services)
    {
        return services.AddInfinityMessaging(options => { });
    }

    public static MessageBusBuilder AddInfinityMessaging(this IServiceCollection services, Action<MessageBusOptions> configure)
    {
        return services.AddInfinityMessaging(builder => { }, configure);
    }

    /// <summary>
    /// Adds the messaging bus to the IServiceCollection, and applies configurations specified in a delegate.
    /// </summary>
    /// <param name="services">The service collection to add the message bus.</param>
    /// <param name="builder">A delegate to configure the message bus.</param>
    /// <param name="options">A delegate to configure the message bus options.</param>
    /// <param name="configSectionPath">Optional: The configuration section path to bind to the message bus options.</param>
    /// <returns>An IMessageBusBuilder instance to build a message bus.</returns>
    public static MessageBusBuilder AddInfinityMessaging(this IServiceCollection services, Action<MessageBusBuilder> builder, Action<MessageBusOptions> options, string configSectionPath = ConfigSectionPath)
    {
        services.ConfigureDefaults();

        services.AddOptions<MessageBusOptions>()
            .BindConfiguration(configSectionPath)
            .Configure(options)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var messageBusBuilder = new MessageBusBuilder(services);
        builder?.Invoke(messageBusBuilder);

        return messageBusBuilder;
    }

    /// <summary>
    /// Adds a message bus to the IServiceCollection, and applies configurations from the specified configuration section.
    /// </summary>
    /// <param name="services">The service collection to add the message bus.</param>
    /// <param name="builder">A delegate to configure the message bus.</param>
    /// <param name="configSectionPath">The configuration section path to bind to the message bus options.</param>
    /// <returns>An IMessageBusBuilder instance to build a message bus.</returns>
    public static MessageBusBuilder AddInfinityMessaging(this IServiceCollection services, Action<MessageBusBuilder>? builder, string configSectionPath = ConfigSectionPath)
    {
        services.ConfigureDefaults();

        services.AddOptions<MessageBusOptions>()
                .BindConfiguration(configSectionPath)
                .ValidateDataAnnotations()
                .ValidateOnStart();

        var messageBusBuilder = new MessageBusBuilder(services);
        builder?.Invoke(messageBusBuilder);

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
