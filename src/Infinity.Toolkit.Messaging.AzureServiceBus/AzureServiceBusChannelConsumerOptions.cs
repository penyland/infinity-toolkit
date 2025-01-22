namespace Infinity.Toolkit.Messaging.AzureServiceBus;

/// <summary>
/// Represents the options for a Azure Service Bus channel consumer.
/// </summary>
public sealed class AzureServiceBusChannelConsumerOptions : ChannelConsumerOptions
{
    /// <summary>
    /// Gets or sets the <see cref="ServiceBusProcessorOptions"/> to use when creating the <see cref="ServiceBusProcessor"/>.
    /// </summary>
    public ServiceBusProcessorOptions ServiceBusProcessorOptions { get; set; } = new();
}

internal class ConfigureAzureServiceBusChannelOptions(IOptions<MessageBusOptions> options) : IPostConfigureOptions<AzureServiceBusChannelConsumerOptions>
{
    private readonly MessageBusOptions messageBusOptions = options.Value;

    public void PostConfigure(string? name, AzureServiceBusChannelConsumerOptions options)
    {
        options.EventTypeName ??= options.EventType?.Name.ToLowerInvariant() ?? string.Empty;
        options.JsonSerializerOptions ??= messageBusOptions?.JsonSerializerOptions;
    }
}
