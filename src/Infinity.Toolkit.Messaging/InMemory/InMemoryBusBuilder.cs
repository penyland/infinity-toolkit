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
    #region ChannelConsumers

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
    public static InMemoryBusBuilder AddKeyedChannelConsumer(this InMemoryBusBuilder builder, string serviceKey, Action<InMemoryChannelConsumerOptions>? configureChannelOptions)
    {
        return builder.ConfigureChannelConsumer(serviceKey, configureChannelOptions: configureChannelOptions);
    }

    public static InMemoryBusBuilder AddKeyedChannelConsumer<TMessage>(this InMemoryBusBuilder builder, string serviceKey, Action<InMemoryChannelConsumerOptions>? configureChannelOptions = default)
    {
        return builder.ConfigureChannelConsumer(serviceKey, typeof(TMessage), configureChannelOptions);
    }

    private static InMemoryBusBuilder ConfigureChannelConsumer(this InMemoryBusBuilder builder, string serviceKey, Type? type = default, Action<InMemoryChannelConsumerOptions>? configureChannelOptions = default)
    {
        builder.Services.AddOptions<InMemoryChannelConsumerOptions>(serviceKey)
            .Configure(options =>
            {
                options.ChannelName = serviceKey;
                options.ChannelType = ChannelType.Topic;
                options.SubscriptionName = serviceKey;

                if (type is not null)
                {
                    options.EventType = type;
                }

                configureChannelOptions?.Invoke(options);
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

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
    #endregion

    /// <summary>
    /// Adds a default channel producer to the in-memory bus builder.
    /// </summary>
    /// <param name="builder">Used to configure the in-memory bus with a new channel producer.</param>
    /// <param name="configureChannelOptions">Allows customization of options for the channel producer being added.</param>
    /// <returns>Returns the updated in-memory bus builder with the new channel producer.</returns>
    public static InMemoryBusBuilder AddDefaultChannelProducer(this InMemoryBusBuilder builder, Action<InMemoryChannelProducerOptions>? configureChannelOptions = null)
    {
        return builder.AddKeyedChannelProducer("default", configureChannelOptions);
    }

    /// <summary>
    /// Adds a transient default channel producer that can produce messages of the type <typeparamref name="TMessage"/> to the InMemoryBroker.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <param name="builder">The <see cref="InMemoryBusBuilder"/>.</param>
    /// <param name="configureChannelOptions">A delegate that can be used to configure the channel options.</param>
    /// <returns>An <see cref="InMemoryBusBuilder"/> that can be used to further configure the InMemoryBroker.</returns>
    public static InMemoryBusBuilder AddChannelProducer<TMessage>(this InMemoryBusBuilder builder, Action<InMemoryChannelProducerOptions>? configureChannelOptions = null)
        where TMessage : class
    {
        builder.ConfigureChannelProducerOptions<TMessage>(typeof(TMessage).Name, options =>
        {
            options.ChannelName = typeof(TMessage).Name;
            configureChannelOptions?.Invoke(options);
        });

        builder.Services.AddTransient<IChannelProducer<TMessage>, InMemoryChannelProducer<TMessage>>();
        return builder;
    }

    /// <summary>
    /// Adds a keyed channel producer to the InMemoryBroker.
    /// </summary>
    /// <param name="builder">The <see cref="InMemoryBusBuilder"/>.</param>
    /// <param name="serviceKey">The key to identify the channel producer.</param>
    /// <param name="configureChannelOptions">A delegate that can be used to configure the channel options.</param>
    /// <returns>An <see cref="InMemoryBusBuilder"/> that can be used to further configure the InMemoryBroker.</returns>
    public static InMemoryBusBuilder AddKeyedChannelProducer(this InMemoryBusBuilder builder, string serviceKey, Action<InMemoryChannelProducerOptions>? configureChannelOptions = default)
    {
        ArgumentNullException.ThrowIfNull(serviceKey, nameof(serviceKey));
        builder.ConfigureKeyedChannelProducerOptions(serviceKey, configureChannelOptions);
        builder.Services.AddKeyedTransient<IChannelProducer, InMemoryChannelProducer>(serviceKey);
        return builder;
    }

    public static InMemoryBusBuilder AddKeyedChannelProducer<TMessage>(this InMemoryBusBuilder builder, string serviceKey, Action<InMemoryChannelProducerOptions>? configureChannelOptions = default)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(serviceKey, nameof(serviceKey));
        builder.ConfigureChannelProducerOptions<TMessage>(serviceKey, configureChannelOptions);
        builder.Services.AddKeyedTransient<IChannelProducer<TMessage>, InMemoryChannelProducer<TMessage>>(serviceKey);
        return builder;
    }

    private static InMemoryBusBuilder ConfigureChannelProducerOptions<TMessage>(this InMemoryBusBuilder builder, string serviceKey, Action<InMemoryChannelProducerOptions>? configureChannelProducerOptions)
        where TMessage : class
    {
        builder.Services.ConfigureOptions<ConfigureInMemoryChannelProducerOptions>();

        var eventType = typeof(TMessage);

        builder.Services.AddOptions<InMemoryBusOptions>()
            .Configure(options =>
            {
                options.ChannelProducerRegistry.TryAdd(eventType.AssemblyQualifiedName ?? eventType.Name, new ChannelProducerRegistration
                {
                    BrokerName = builder.BrokerName,
                    EventType = eventType,
                    Key = serviceKey
                });
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<InMemoryChannelProducerOptions>(eventType.AssemblyQualifiedName ?? eventType.Name)
            .Configure(options =>
        {
                options.EventType = eventType;
                options.ServiceKey = serviceKey ?? eventType.AssemblyQualifiedName ?? eventType.Name;
                options.Name = $"{eventType.Name}ChannelProducer";
                configureChannelProducerOptions?.Invoke(options);
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return builder;
    }

    private static InMemoryBusBuilder ConfigureKeyedChannelProducerOptions(this InMemoryBusBuilder builder, string serviceKey, Action<InMemoryChannelProducerOptions>? configureChannelProducerOptions)
    {
        builder.Services.ConfigureOptions<ConfigureInMemoryChannelProducerOptions>();

        builder.Services.AddOptions<InMemoryBusOptions>()
            .Configure(options =>
            {
                options.ChannelProducerRegistry.TryAdd(serviceKey, new ChannelProducerRegistration
                {
                    BrokerName = builder.BrokerName,
                    Key = serviceKey
                });
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<InMemoryChannelProducerOptions>(serviceKey)
            .Configure(options =>
        {
            options.ChannelName = serviceKey;
                options.ChannelType = ChannelType.Topic;
                options.ServiceKey = serviceKey;
                options.Name = $"{serviceKey}ChannelProducer";
                configureChannelProducerOptions?.Invoke(options);
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();

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
}
