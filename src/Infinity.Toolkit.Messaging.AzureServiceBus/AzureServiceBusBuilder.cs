using Infinity.Toolkit.Messaging.Abstractions;

namespace Infinity.Toolkit.Messaging.AzureServiceBus;

public sealed class AzureServiceBusBuilder(MessageBusBuilder messageBusBuilder)
{
    public IServiceCollection Services { get; } = messageBusBuilder.Services;

    public MessageBusBuilder MessageBusBuilder { get; } = messageBusBuilder;

    public string BrokerName => AzureServiceBusDefaults.Name;
}

public static class AzureServiceBusBuilderExtensions
{
    /// <summary>
    /// Adds a channel consumer to the message bus.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="builder">The <see cref="AzureServiceBusBuilder"/>.</param>
    /// <param name="configureChannelOptions">A delegate that can be used to configure the channel options.</param>
    /// <returns>An <see cref="AzureServiceBusBuilder"/> that can be used to further configure the AzureServiceBusBuilder.</returns>
    public static AzureServiceBusBuilder AddChannelConsumer<TMessage>(this AzureServiceBusBuilder builder, Action<AzureServiceBusChannelConsumerOptions> configureChannelOptions)
    {
        builder.Services.AddOptions<AzureServiceBusChannelConsumerOptions>(typeof(TMessage).AssemblyQualifiedName)
            .Configure(options =>
            {
                options.EventType = typeof(TMessage);
                configureChannelOptions(options);
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<AzureServiceBusOptions>()
            .Configure(options =>
            {
                options.ChannelConsumerRegistry.Add(typeof(TMessage).AssemblyQualifiedName ?? typeof(TMessage).Name, new ChannelConsumerRegistration
                {
                    BrokerName = builder.BrokerName,
                    EventType = typeof(TMessage),
                    Key = typeof(TMessage).AssemblyQualifiedName ?? typeof(TMessage).Name
                });
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.ConfigureOptions<ConfigureAzureServiceBusChannelOptions>();

        return builder;
    }

    /// <summary>
    /// Adds a keyed channel consumer to the message bus.
    /// Only one producer per key is allowed.
    /// </summary>
    /// <param name="builder">The <see cref="AzureServiceBusBuilder"/>.</param>
    /// <param name="serviceKey">The key to identify the channel consumer with.</param>
    /// <param name="configureChannelOptions">A delegate that can be used to configure the channel options.</param>
    /// <returns>An <see cref="AzureServiceBusBuilder"/> that can be used to further configure the AzureServiceBusBuilder.</returns>
    public static AzureServiceBusBuilder AddChannelConsumer(this AzureServiceBusBuilder builder, string serviceKey, Action<AzureServiceBusChannelConsumerOptions> configureChannelOptions)
    {
        builder.Services.AddOptions<AzureServiceBusChannelConsumerOptions>(serviceKey)
            .Configure(options =>
            {
                options.RequireCloudEventsTypeProperty = false;
                configureChannelOptions(options);
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<AzureServiceBusOptions>()
            .Configure(options =>
            {
                options.ChannelConsumerRegistry.Add(serviceKey, new ChannelConsumerRegistration
                {
                    BrokerName = builder.BrokerName,
                    Key = serviceKey
                });
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.ConfigureOptions<ConfigureAzureServiceBusChannelOptions>();

        return builder;
    }

    /// <summary>
    /// Adds a bus of type <typeparamref name="TBus"/> with options of type <typeparamref name="TOptions"/> to the AzureServiceBusBuilder.
    /// </summary>
    /// <typeparam name="TBus">The type of the bus.</typeparam>
    /// <typeparam name="TOptions">The type of the bus options.</typeparam>
    /// <param name="builder">The <see cref="AzureServiceBusBuilder"/>.</param>
    /// <param name="configureOptions">A delegate that can be used to configure the options.</param>
    /// <returns>An <see cref="AzureServiceBusBuilder"/> that can be used to further configure the AzureServiceBusBuilder.</returns>
    public static AzureServiceBusBuilder AddBroker<TBus, TOptions>(this AzureServiceBusBuilder builder, Action<TOptions> configureOptions)
        where TBus : class, IBroker
        where TOptions : MessageBusBrokerOptions
    {
        builder.MessageBusBuilder.AddBroker<TBus, TOptions>(AzureServiceBusDefaults.Name, configureOptions);
        return builder;
    }

    /// <summary>
    /// Adds a keyed channel producer to the AzureServiceBusBuilder.
    /// Only one producer per key is allowed.
    /// </summary>
    /// <param name="builder">The <see cref="AzureServiceBusBuilder"/>.</param>
    /// <param name="serviceKey">The key to identify the channel producer options.</param>
    /// <param name="configureChannelOptions">A delegate that can be used to configure the channel options.</param>
    /// <returns>An <see cref="AzureServiceBusBuilder"/> that can be used to further configure the AzureServiceBusBuilder.</returns>
    public static AzureServiceBusBuilder AddChannelProducer(this AzureServiceBusBuilder builder, string serviceKey, Action<AzureServiceBusChannelProducerOptions> configureChannelOptions)
    {
        ArgumentNullException.ThrowIfNull(serviceKey, nameof(serviceKey));
        builder.ConfigureChannelProducerOptions(serviceKey, configureChannelOptions);
        builder.Services.AddKeyedTransient<IChannelProducer, AzureServiceBusChannelProducer>(serviceKey);
        return builder;
    }

    /// <summary>
    /// Adds a transient default channel producer that can produce messages of the type <typeparamref name="TEventType"/> on Azure Service Bus.
    /// </summary>
    /// <typeparam name="TEventType">The type of the message.</typeparam>
    /// <param name="builder">The <see cref="AzureServiceBusBuilder"/>.</param>
    /// <param name="configureChannelOptions">A delegate that can be used to configure the channel options.</param>
    /// <returns>An <see cref="AzureServiceBusBuilder"/> that can be used to further configure the Azure Service Bus.</returns>
    public static AzureServiceBusBuilder AddChannelProducer<TEventType>(this AzureServiceBusBuilder builder, Action<AzureServiceBusChannelProducerOptions> configureChannelOptions)
        where TEventType : class
    {
        builder.ConfigureChannelProducerOptions(typeof(TEventType), configureChannelOptions);
        builder.Services.AddTransient<IChannelProducer<TEventType>, AzureServiceBusChannelProducer<TEventType>>();
        return builder;
    }

    /// <summary>
    /// Adds a transient channel producer of the type <typeparamref name="TImplementation"/> that can produce messages of the type <typeparamref name="TEventType"/> to the Azure Service Bus.
    /// </summary>
    /// <typeparam name="TEventType">The type of the event.</typeparam>
    /// <typeparam name="TImplementation">A type that implements <see cref="IChannelProducer{TEventType}"/>.</typeparam>
    /// <param name="builder">The <see cref="AzureServiceBusBuilder"/>.</param>
    /// <param name="configureChannelOptions">A delegate that can be used to configure the channel options.</param>
    /// <returns>An <see cref="AzureServiceBusBuilder"/> that can be used to further configure the Azure Service Bus.</returns>
    public static AzureServiceBusBuilder AddChannelProducer<TEventType, TImplementation>(this AzureServiceBusBuilder builder, Action<AzureServiceBusChannelProducerOptions> configureChannelOptions)
        where TEventType : class
        where TImplementation : class
    {
        builder.Services.AddTransient<TImplementation>();
        return builder.AddChannelProducer<TEventType>(configureChannelOptions);
    }

    /// <summary>
    /// Adds a transient channel producer of the type <typeparamref name="TService"/> with an implementation type <typeparamref name="TImplementation"/> that can produce messages of the type <typeparamref name="TEventType"/> to the Azure Service Bus.
    /// </summary>
    /// <typeparam name="TEventType">The type of the event.</typeparam>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <param name="builder">The <see cref="AzureServiceBusBuilder"/>.</param>
    /// <param name="configureChannelOptions">A delegate that can be used to configure the channel options.</param>
    /// <returns>An <see cref="AzureServiceBusBuilder"/> that can be used to further configure the Azure Service Bus.</returns>
    public static AzureServiceBusBuilder AddChannelProducer<TEventType, TService, TImplementation>(this AzureServiceBusBuilder builder, Action<AzureServiceBusChannelProducerOptions> configureChannelOptions)
        where TEventType : class
        where TService : class
        where TImplementation : class, TService
    {
        builder.Services.AddTransient<TService, TImplementation>();
        return builder.AddChannelProducer<TEventType>(configureChannelOptions);
    }

    /// <summary>
    /// Adds a transient deferred channel consumer that can consume deferred messages of the type <typeparamref name="TEventType"/> from the Azure Service Bus.
    /// </summary>
    /// <typeparam name="TEventType">The type of the message.</typeparam>
    /// <param name="configureChannelOptions">A delegate that can be used to configure the channel options.</param>
    /// <returns>An <see cref="AzureServiceBusBuilder"/> that can be used to further configure the Azure Service Bus.</returns>
    public static AzureServiceBusBuilder AddDeferredChannelConsumer<TEventType>(this AzureServiceBusBuilder builder, Action<AzureServiceBusDeferredChannelConsumerOptions> configureChannelOptions)
        where TEventType : class
    {
        builder.Services.AddOptions<AzureServiceBusDeferredChannelConsumerOptions>(typeof(TEventType).AssemblyQualifiedName)
            .Configure(options =>
            {
                options.EventType = typeof(TEventType);
                configureChannelOptions(options);
            })
            .ValidateOnStart();

        builder.Services.AddTransient<IDeferredChannelConsumer<TEventType>, DeferredChannelConsumer<TEventType>>();
        return builder;
    }

    private static AzureServiceBusBuilder ConfigureChannelProducerOptions(this AzureServiceBusBuilder builder, Type eventType, Action<AzureServiceBusChannelProducerOptions> configureChannelProducerOptions)
    {
        builder.Services.ConfigureOptions<ConfigureAzureServiceBusBrokerChannelProducerOptions>();

        builder.Services.AddOptions<AzureServiceBusChannelProducerOptions>(eventType.AssemblyQualifiedName ?? eventType.Name)
            .Configure(options =>
            {
                options.EventType = eventType;
                options.ServiceKey = eventType.AssemblyQualifiedName ?? eventType.Name;
                options.Name = $"{eventType.Name}ChannelProducer";
                configureChannelProducerOptions(options);
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return builder;
    }

    private static AzureServiceBusBuilder ConfigureChannelProducerOptions(this AzureServiceBusBuilder builder, string serviceKey, Action<AzureServiceBusChannelProducerOptions> configureChannelProducerOptions)
    {
        builder.Services.ConfigureOptions<ConfigureAzureServiceBusBrokerChannelProducerOptions>();

        builder.Services.AddOptions<AzureServiceBusChannelProducerOptions>(serviceKey)
            .Configure(options =>
            {
                options.ServiceKey = serviceKey;
                options.Name = $"{serviceKey}ChannelProducer";
                configureChannelProducerOptions(options);
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return builder;
    }
}
