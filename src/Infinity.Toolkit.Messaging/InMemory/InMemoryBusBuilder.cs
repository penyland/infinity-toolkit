using Infinity.Toolkit.Messaging.Abstractions;

namespace Infinity.Toolkit.Messaging.InMemory;

public sealed class InMemoryBusBuilder(MessageBusBuilder messageBusBuilder)
{
    public IServiceCollection Services { get; } = messageBusBuilder.Services;

    public MessageBusBuilder MessageBusBuilder { get; } = messageBusBuilder;

    public string BrokerName => InMemoryBusDefaults.Name;
}

public static class InMemoryBusBuilderExtensions
{
    public static InMemoryBusBuilder AddChannelConsumer<TMessage>(this InMemoryBusBuilder builder) => builder.AddChannelConsumer<TMessage>(options => { });

    /// <summary>
    /// Adds a channel consumer to the broker.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="builder">The <see cref="InMemoryBusBuilder"/>.</param>
    /// <param name="configureChannelOptions">A delegate that can be used to configure the channel options.</param>
    /// <returns>An <see cref="InMemoryBusBuilder"/> that can be used to further configure the InMemoryBroker.</returns>
    public static InMemoryBusBuilder AddChannelConsumer<TMessage>(this InMemoryBusBuilder builder, Action<InMemoryChannelConsumerOptions> configureChannelOptions)
    {
        return builder.ConfigureChannelConsumer(typeof(TMessage).AssemblyQualifiedName ?? typeof(TMessage).Name, typeof(TMessage), configureChannelOptions);
    }

    /// <summary>
    /// Adds a keyed channel consumer to the InMemoryBroker.
    /// Only one producer per key is allowed.
    /// </summary>
    /// <param name="builder">The <see cref="InMemoryBusBuilder"/>.</param>
    /// <param name="serviceKey">The key to identify the channel consumer with.</param>
    /// <param name="configureChannelOptions">A delegate that can be used to configure the channel options.</param>
    /// <returns>An <see cref="InMemoryBusBuilder"/> that can be used to further configure the InMemoryBroker.</returns>
    public static InMemoryBusBuilder AddChannelConsumer(this InMemoryBusBuilder builder, string serviceKey, Action<InMemoryChannelConsumerOptions>? configureChannelOptions)
    {
        return builder.ConfigureChannelConsumer(serviceKey, typeof(object), configureChannelOptions);
    }

    private static InMemoryBusBuilder ConfigureChannelConsumer(this InMemoryBusBuilder builder, string serviceKey, Type type, Action<InMemoryChannelConsumerOptions>? configureChannelOptions)
    {
        builder.Services.AddOptions<InMemoryChannelConsumerOptions>(serviceKey)
            .Configure(options =>
            {
                options.ChannelName = serviceKey;
                options.ChannelType = ChannelType.Topic;
                options.EventType = type;
                options.SubscriptionName = serviceKey;
                configureChannelOptions?.Invoke(options);
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<InMemoryBusOptions>()
            .Configure(options =>
            {
                Console.WriteLine($"Adding channel consumer for {serviceKey} with EventType {type} on {builder.BrokerName}");
                options.ChannelConsumerRegistry.TryAdd(serviceKey, new ChannelConsumerRegistration
                {
                    BrokerName = builder.BrokerName,
                    EventType = type,
                    Key = serviceKey
                });
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.ConfigureOptions<ConfigureInMemoryBusChannelOptions>();

        return builder;
    }

    /// <summary>
    /// Adds a keyed channel producer to the InMemoryBroker.
    /// </summary>
    /// <param name="builder">The <see cref="InMemoryBusBuilder"/>.</param>
    /// <param name="serviceKey">The key to identify the channel producer.</param>
    /// <param name="configureChannelOptions">A delegate that can be used to configure the channel options.</param>
    /// <returns>An <see cref="InMemoryBusBuilder"/> that can be used to further configure the InMemoryBroker.</returns>
    public static InMemoryBusBuilder AddChannelProducer(this InMemoryBusBuilder builder, string serviceKey, Action<InMemoryChannelProducerOptions> configureChannelOptions)
    {
        ArgumentNullException.ThrowIfNull(serviceKey, nameof(serviceKey));
        builder.ConfigureChannelProducerOptions(serviceKey, configureChannelOptions);
        builder.Services.AddKeyedTransient<IChannelProducer, InMemoryChannelProducer>(serviceKey);
        return builder;
    }

    /// <summary>
    /// Adds a transient default channel producer that can produce messages of the type <typeparamref name="TEventType"/> to the InMemoryBroker.
    /// The channel producer is configured to send messages to a topic channel with the same name as the type of the message.
    /// </summary>
    /// <typeparam name="TEventType">The type of the message.</typeparam>
    /// <param name="builder">The <see cref="InMemoryBusBuilder"/>.</param>
    /// <returns>An <see cref="InMemoryBusBuilder"/> that can be used to further configure the InMemoryBroker.</returns>
    public static InMemoryBusBuilder AddChannelProducer<TEventType>(this InMemoryBusBuilder builder)
        where TEventType : class
    {
        builder.ConfigureChannelProducerOptions(typeof(TEventType), options =>
        {
            options.ChannelName = typeof(TEventType).Name;
        });

        builder.Services.AddTransient<IChannelProducer<TEventType>, InMemoryChannelProducer<TEventType>>();
        return builder;
    }

    /// <summary>
    /// Adds a transient default channel producer that can produce messages of the type <typeparamref name="TEventType"/> to the InMemoryBroker.
    /// </summary>
    /// <typeparam name="TEventType">The type of the message.</typeparam>
    /// <param name="builder">The <see cref="InMemoryBusBuilder"/>.</param>
    /// <param name="configureChannelOptions">A delegate that can be used to configure the channel options.</param>
    /// <returns>An <see cref="InMemoryBusBuilder"/> that can be used to further configure the InMemoryBroker.</returns>
    public static InMemoryBusBuilder AddChannelProducer<TEventType>(this InMemoryBusBuilder builder, Action<InMemoryChannelProducerOptions> configureChannelOptions)
        where TEventType : class
    {
        builder.ConfigureChannelProducerOptions(typeof(TEventType), configureChannelOptions);
        builder.Services.AddTransient<IChannelProducer<TEventType>, InMemoryChannelProducer<TEventType>>();
        return builder;
    }

    /// <summary>
    /// Adds a transient channel producer of the type <typeparamref name="TImplementation"/> that can produce messages of the type <typeparamref name="TEventType"/> to the InMemoryBroker.
    /// </summary>
    /// <typeparam name="TEventType">The type of the event.</typeparam>
    /// <typeparam name="TImplementation">A type that implements <see cref="IChannelProducer{TEventType}"/>.</typeparam>
    /// <param name="builder">The <see cref="InMemoryBusBuilder"/>.</param>
    /// <param name="configureChannelOptions">A delegate that can be used to configure the channel options.</param>
    /// <returns>An <see cref="InMemoryBusBuilder"/> that can be used to further configure the InMemoryBroker.</returns>
    public static InMemoryBusBuilder AddChannelProducer<TEventType, TImplementation>(this InMemoryBusBuilder builder, Action<InMemoryChannelProducerOptions> configureChannelOptions)
        where TEventType : class
        where TImplementation : class
    {
        builder.Services.AddTransient<TImplementation>();
        return builder.AddChannelProducer<TEventType>(configureChannelOptions);
    }

    /// <summary>
    /// Adds a transient channel producer of the type <typeparamref name="TService"/> with an implementation type <typeparamref name="TImplementation"/> that can produce messages of the type <typeparamref name="TEventType"/> to the InMemoryBroker.
    /// </summary>
    /// <typeparam name="TEventType">The type of the event.</typeparam>
    /// <typeparam name="TService">The type of the service to add.</typeparam>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <param name="builder">The <see cref="InMemoryBusBuilder"/>.</param>
    /// <param name="configureChannelOptions">A delegate that can be used to configure the channel options.</param>
    /// <returns>An <see cref="InMemoryBusBuilder"/> that can be used to further configure the InMemoryBroker.</returns>
    public static InMemoryBusBuilder AddChannelProducer<TEventType, TService, TImplementation>(this InMemoryBusBuilder builder, Action<InMemoryChannelProducerOptions> configureChannelOptions)
        where TEventType : class
        where TService : class
        where TImplementation : class, TService
    {
        builder.Services.AddTransient<TService, TImplementation>();
        return builder.AddChannelProducer<TEventType>(configureChannelOptions);
    }

    /// <summary>
    /// Adds a transient deferred channel consumer that can consume deferred messages of the type <typeparamref name="TEventType"/> from the InMemoryBroker.
    /// </summary>
    /// <typeparam name="TEventType">The type of the message.</typeparam>
    /// <param name="configureChannelOptions">A delegate that can be used to configure the channel options.</param>
    /// <returns>An <see cref="InMemoryBusBuilder"/> that can be used to further configure the InMemoryBroker.</returns>
    public static InMemoryBusBuilder AddDeferredChannelConsumer<TEventType>(this InMemoryBusBuilder builder, Action<InMemoryDeferredChannelConsumerOptions> configureChannelOptions)
        where TEventType : class
    {
        builder.Services.AddOptions<InMemoryDeferredChannelConsumerOptions>(typeof(TEventType).AssemblyQualifiedName)
            .Configure(options =>
            {
                options.EventType = typeof(TEventType);
                configureChannelOptions(options);
            })
            .ValidateOnStart();

        builder.Services.AddTransient<IDeferredChannelConsumer<TEventType>, InMemoryDeferredChannelConsumer<TEventType>>();
        return builder;
    }

    /// <summary>
    /// Adds a broker of type <typeparamref name="TBroker"/> with options of type <typeparamref name="TBrokerOptions"/> to the InMemoryBrokerBuilder.
    /// </summary>
    /// <typeparam name="TBroker">The type of the broker.</typeparam>
    /// <typeparam name="TBrokerOptions">The type of the broker options.</typeparam>
    /// <param name="builder">The <see cref="InMemoryBusBuilder"/>.</param>
    /// <param name="configureOptions">A delegate that can be used to configure the broker options.</param>
    /// <returns>An <see cref="InMemoryBusBuilder"/> that can be used to further configure the AzureServiceBusBroker.</returns>
    internal static InMemoryBusBuilder AddBroker<TBroker, TBrokerOptions>(this InMemoryBusBuilder builder, Action<TBrokerOptions> configureOptions)
        where TBroker : class, IBroker
        where TBrokerOptions : MessageBusBrokerOptions
    {
        builder.MessageBusBuilder.AddBroker<TBroker, TBrokerOptions>(InMemoryBusDefaults.Name, configureOptions);
        return builder;
    }

    private static InMemoryBusBuilder ConfigureChannelProducerOptions(this InMemoryBusBuilder builder, Type eventType, Action<InMemoryChannelProducerOptions> configureChannelProducerOptions)
    {
        builder.Services.ConfigureOptions<ConfigureInMemoryChannelProducerOptions>();

        builder.Services.AddOptions<InMemoryChannelProducerOptions>(eventType.AssemblyQualifiedName ?? eventType.Name)
            .Configure(options =>
            {
                options.EventType = eventType;
                options.Key = eventType.AssemblyQualifiedName ?? eventType.Name;
                options.Name = $"{eventType.Name}ChannelProducer";
                configureChannelProducerOptions(options);
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return builder;
    }

    private static InMemoryBusBuilder ConfigureChannelProducerOptions(this InMemoryBusBuilder builder, string serviceKey, Action<InMemoryChannelProducerOptions> configureChannelProducerOptions)
    {
        builder.Services.ConfigureOptions<ConfigureInMemoryChannelProducerOptions>();

        builder.Services.AddOptions<InMemoryChannelProducerOptions>(serviceKey)
            .Configure(options =>
            {
                options.Key = serviceKey;
                options.Name = $"{serviceKey}ChannelProducer";
                configureChannelProducerOptions(options);
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return builder;
    }



    public static InMemoryBusBuilder AddDefaultChannelProducer(this InMemoryBusBuilder builder, string serviceKey = "default", Action<InMemoryChannelProducerOptions>? configureChannelOptions = null)
    {
        ArgumentNullException.ThrowIfNull(serviceKey, nameof(serviceKey));
        builder.ConfigureDefaultChannelProducerOptions(serviceKey, configureChannelOptions);
        builder.Services.AddTransient<IChannelProducer2, InMemoryChannelProducer2>();
        return builder;
    }

    private static InMemoryBusBuilder ConfigureDefaultChannelProducerOptions(this InMemoryBusBuilder builder, string serviceKey, Action<InMemoryChannelProducerOptions>? configureChannelProducerOptions)
    {
        builder.Services.ConfigureOptions<ConfigureInMemoryChannelProducerOptions>();

        builder.Services.AddOptions<InMemoryChannelProducerOptions>(serviceKey)
            .Configure(options =>
            {
                options.Key = serviceKey;
                options.Name = $"{serviceKey}ChannelProducer";
                options.ChannelType = ChannelType.Topic;
                options.ChannelName = serviceKey;
                configureChannelProducerOptions?.Invoke(options);
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return builder;
    }

    public static InMemoryBusBuilder AddDefaultChannelConsumer(this InMemoryBusBuilder builder, string serviceKey = "default", Action<InMemoryChannelConsumerOptions>? configureChannelOptions = default)
    {
        return builder.ConfigureDefaultChannelConsumer(serviceKey, typeof(object), configureChannelOptions);
    }

    private static InMemoryBusBuilder ConfigureDefaultChannelConsumer(this InMemoryBusBuilder builder, string serviceKey, Type type, Action<InMemoryChannelConsumerOptions>? configureChannelOptions)
    {
        builder.Services.AddOptions<InMemoryChannelConsumerOptions>(serviceKey)
            .Configure(options =>
            {
                options.ChannelName = serviceKey;
                options.ChannelType = ChannelType.Topic;
                options.EventType = type;
                options.SubscriptionName = serviceKey;
                options.RequireCloudEventsTypeProperty = false;
                configureChannelOptions?.Invoke(options);
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        Console.WriteLine($"Adding channel consumer for {serviceKey} with EventType {type} on {builder.BrokerName}");
        builder.Services.AddOptions<InMemoryBusOptions>()
            .Configure(options =>
            {
                options.ChannelConsumerRegistry.TryAdd(serviceKey, new ChannelConsumerRegistration
                {
                    BrokerName = builder.BrokerName,
                    EventType = type,
                    Key = serviceKey
                });
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.ConfigureOptions<ConfigureInMemoryBusChannelOptions>();

        return builder;
    }
}
