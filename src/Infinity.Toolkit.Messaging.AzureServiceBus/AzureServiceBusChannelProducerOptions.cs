namespace Infinity.Toolkit.Messaging.AzureServiceBus;

public sealed class AzureServiceBusChannelProducerOptions : ChannelProducerOptions
{
    /// <summary>
    /// A property used to set the <see cref="ServiceBusSender"/> ID to identify the client. This can be used to correlate logs
    /// and exceptions. If <c>null</c> or empty, a random unique value will be used.
    /// </summary>
    public string Identifier { get; set; }

    /// <summary>
    /// Get or sets options that can be specified when creating a <see cref="ServiceBusSender"/>
    /// to configure its behavior.
    /// </summary>
    public ServiceBusSenderOptions ServiceBusSenderOptions { get; set; }
}

internal class ConfigureAzureServiceBusBrokerChannelProducerOptions(IOptions<MessageBusOptions> options) : IPostConfigureOptions<AzureServiceBusChannelProducerOptions>
{
    private readonly MessageBusOptions messageBusOptions = options.Value;

    public void PostConfigure(string? name, AzureServiceBusChannelProducerOptions options)
    {
        options.EventTypeName ??= options.EventType?.Name.ToLowerInvariant() ?? string.Empty;
        options.JsonSerializerOptions ??= messageBusOptions?.JsonSerializerOptions;
        options.Source ??= new Uri($"asb://{messageBusOptions?.ApplicationName}/{messageBusOptions?.Environment}/{options.Name}".ToLowerInvariant());
        options.ServiceBusSenderOptions ??= !string.IsNullOrEmpty(options.Identifier) ? new() { Identifier = options.Identifier } : new();
    }
}
