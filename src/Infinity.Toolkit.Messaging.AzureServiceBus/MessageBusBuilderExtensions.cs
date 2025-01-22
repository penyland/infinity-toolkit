namespace Infinity.Toolkit.Messaging.AzureServiceBus;

public static class MessageBusBuilderExtensions
{
    private const string DefaultConfigSectionName = "Infinity:Messaging:AzureServiceBus";

    public static MessageBusBuilder ConfigureAzureServiceBus(this MessageBusBuilder messageBusBuilder, Action<AzureServiceBusBuilder> builder, string configSectionName = DefaultConfigSectionName)
    {
        var busBuilder = new AzureServiceBusBuilder(messageBusBuilder);
        busBuilder.ConfigureAzureServiceBusDefaults((o) => { }, configSectionName);
        builder?.Invoke(busBuilder);
        return messageBusBuilder;
    }

    public static MessageBusBuilder ConfigureAzureServiceBus(this MessageBusBuilder messageBusBuilder, Action<AzureServiceBusBuilder> builder, AzureServiceBusOptions options, string configSectionName = DefaultConfigSectionName)
    {
        var busBuilder = new AzureServiceBusBuilder(messageBusBuilder);
        busBuilder.ConfigureAzureServiceBusDefaults((o) => { o = options; }, configSectionName);
        builder?.Invoke(busBuilder);
        return messageBusBuilder;
    }

    public static MessageBusBuilder ConfigureAzureServiceBus(this MessageBusBuilder messageBusBuilder, Action<AzureServiceBusBuilder> builder, Action<AzureServiceBusOptions> options, string configSectionName = DefaultConfigSectionName)
    {
        var busBuilder = new AzureServiceBusBuilder(messageBusBuilder);
        busBuilder.ConfigureAzureServiceBusDefaults(options, configSectionName);
        builder?.Invoke(busBuilder);
        return messageBusBuilder;
    }

    /// <summary>
    /// Adds an Azure Service Bus to the message bus.
    /// </summary>
    /// <param name="messageBusBuilder">The message bus builder.</param>
    /// <param name="configureSettings">An optional delegate to configure the options.</param>
    /// <param name="configSectionName">The configuration section path to bind to the options.</param>
    /// <returns>An AzureServiceBusBuilder that can be used to further configure.</returns>
    public static AzureServiceBusBuilder AddAzureServiceBusBroker(this MessageBusBuilder messageBusBuilder, Action<AzureServiceBusOptions> configureSettings, string configSectionName = DefaultConfigSectionName)
    {
        var busBuilder = new AzureServiceBusBuilder(messageBusBuilder);
        return busBuilder.ConfigureAzureServiceBusDefaults(configureSettings, configSectionName);
    }

    /// <summary>
    /// Adds an Azure Service Bus to the message bus.
    /// </summary>
    /// <param name="messageBusBuilder">The message bus builder.</param>
    /// <param name="configSectionName">The configuration section path to bind to the options.</param>
    /// <returns>An AzureServiceBusBuilder that can be used to further configure.</returns>
    public static AzureServiceBusBuilder AddAzureServiceBusBroker(this MessageBusBuilder messageBusBuilder, string configSectionName = DefaultConfigSectionName)
    {
        var busBuilder = new AzureServiceBusBuilder(messageBusBuilder);
        return busBuilder.ConfigureAzureServiceBusDefaults((_) => { });
    }

    private static AzureServiceBusBuilder ConfigureAzureServiceBusDefaults(this AzureServiceBusBuilder builder, Action<AzureServiceBusOptions> configureSettings, string configSectionPath = DefaultConfigSectionName)
    {
        builder.Services.AddOptions<AzureServiceBusOptions>()
                      .BindConfiguration(configSectionPath)
                      .Configure(configureSettings)
                      .ValidateDataAnnotations()
                      .ValidateOnStart();

        builder.Services.TryAddSingleton<AzureServiceBusClientFactory>();
        builder.Services.ConfigureOptions<ConfigureAzureServiceBusOptions>();
        builder.AddBroker<AzureServiceBusBroker, AzureServiceBusOptions>(options =>
        {
            options.DisplayName = AzureServiceBusDefaults.Name;
        });

        return builder;
    }
}
