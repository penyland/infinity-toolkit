namespace Infinity.Toolkit.Messaging.InMemory;

public static class MessageBusBuilderExtensions
{
    private const string DefaultConfigSectionName = "Infinity:Messaging:InMemoryBus";

    public static MessageBusBuilder AddInMemoryBus(this MessageBusBuilder messageBusBuilder, Action<InMemoryBusBuilder>? builder)
        => messageBusBuilder.ConfigureInMemoryBus(builder);

    public static MessageBusBuilder ConfigureInMemoryBus(this MessageBusBuilder messageBusBuilder)
        => messageBusBuilder.ConfigureInMemoryBus((builder) => { });

    public static MessageBusBuilder ConfigureInMemoryBus(this MessageBusBuilder messageBusBuilder, Action<InMemoryBusBuilder>? builder)
    {
        var brokerBuilder = new InMemoryBusBuilder(messageBusBuilder);
        brokerBuilder.ConfigureInMemoryBusDefaults((o) => { });
        builder?.Invoke(brokerBuilder);
        return messageBusBuilder;
    }

    public static MessageBusBuilder ConfigureInMemoryBus(this MessageBusBuilder messageBusBuilder, Action<InMemoryBusBuilder> builder, InMemoryBusOptions options, string configSectionPath = DefaultConfigSectionName)
    {
        var brokerBuilder = new InMemoryBusBuilder(messageBusBuilder);
        brokerBuilder.ConfigureInMemoryBusDefaults((o) => { o = options; }, configSectionPath);
        builder?.Invoke(brokerBuilder);
        return messageBusBuilder;
    }

    public static MessageBusBuilder ConfigureInMemoryBus(this MessageBusBuilder messageBusBuilder, Action<InMemoryBusBuilder> builder, Action<InMemoryBusOptions> options, string configSectionPath = DefaultConfigSectionName)
    {
        var brokerBuilder = new InMemoryBusBuilder(messageBusBuilder);
        brokerBuilder.ConfigureInMemoryBusDefaults(options, configSectionPath);
        builder?.Invoke(brokerBuilder);
        return messageBusBuilder;
    }

    /// <summary>
    /// Adds an InMemory broker to the message bus.
    /// </summary>
    /// <param name="builder">The message bus builder.</param>
    /// <param name="options">An optional delegate to configure the broker options.</param>
    /// <param name="configSectionPath">The configuration section path to bind to the broker options.</param>
    /// <returns>An InMemoryBrokerBuilder that can be used to further configure the broker.</returns>
    public static InMemoryBusBuilder AddInMemoryBus(this MessageBusBuilder builder, Action<InMemoryBusOptions> options, string configSectionPath = DefaultConfigSectionName)
    {
        var brokerBuilder = new InMemoryBusBuilder(builder);
        return brokerBuilder.ConfigureInMemoryBusDefaults(options, configSectionPath);
    }

    private static InMemoryBusBuilder ConfigureInMemoryBusDefaults(this InMemoryBusBuilder builder, Action<InMemoryBusOptions> options, string configSectionPath = DefaultConfigSectionName)
    {
        builder.Services.AddOptions<InMemoryBusOptions>()
                      .BindConfiguration(configSectionPath)
                      .Configure(options)
                      .ValidateDataAnnotations()
                      .ValidateOnStart();

        builder.Services.TryAddSingleton<SequenceNumberGenerator>();
        builder.Services.TryAddSingleton<InMemoryChannelClientFactory>();
        builder.Services.ConfigureOptions<ConfigureInMemoryBusOptions>();
        builder.AddBroker<InMemoryBus, InMemoryBusOptions>(options =>
        {
            options.DisplayName = InMemoryBusDefaults.Name;
        });

        // Add a default generic channel producer.
        //builder.AddChannelProducer("inmemorydefaultchannel", options => { options.ChannelName = "inmemorydefaultchannel"; });
        //builder.AddChannelConsumer("inmemorydefaultchannel", options => { options.ChannelName = "inmemorydefaultchannel"; });
        builder.AddDefaultChannelProducer();
        builder.AddDefaultChannelConsumer();

        return builder;
    }
}
