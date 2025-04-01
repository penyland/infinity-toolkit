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
        return messageBusBuilder.ConfigureInMemoryBus(builder, (o) => { });
    }

    public static MessageBusBuilder ConfigureInMemoryBus(this MessageBusBuilder messageBusBuilder, Action<InMemoryBusBuilder> builder, InMemoryBusOptions options, string configSectionPath = DefaultConfigSectionName)
    {
        return messageBusBuilder.ConfigureInMemoryBus(builder, (o) => { o = options; }, configSectionPath);
    }

    public static MessageBusBuilder ConfigureInMemoryBus(this MessageBusBuilder messageBusBuilder, Action<InMemoryBusBuilder>? builder, Action<InMemoryBusOptions> options, string configSectionPath = DefaultConfigSectionName)
    {
        var brokerBuilder = new InMemoryBusBuilder(messageBusBuilder);
        brokerBuilder.ConfigureInMemoryBusDefaults(options, configSectionPath);
        builder?.Invoke(brokerBuilder);
        return messageBusBuilder;
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
        //builder.AddDefaultChannelProducer();
        //builder.AddDefaultChannelConsumer();

        return builder;
    }
}
