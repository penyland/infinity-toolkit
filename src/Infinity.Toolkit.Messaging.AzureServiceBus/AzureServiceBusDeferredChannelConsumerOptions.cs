namespace Infinity.Toolkit.Messaging.AzureServiceBus;

public sealed class AzureServiceBusDeferredChannelConsumerOptions : ChannelConsumerOptions
{
    /// <summary>
    /// Gets or sets the <see cref="ServiceBusReceiverOptions"/> to use when creating the receiver.
    /// </summary>
    public ServiceBusReceiverOptions ServiceBusReceiverOptions { get; set; }
}

internal class ConfigureAzureServiceBusBrokerDeferredChannelConsumerOptions : IPostConfigureOptions<AzureServiceBusDeferredChannelConsumerOptions>
{
    public void PostConfigure(string? name, AzureServiceBusDeferredChannelConsumerOptions options)
    {
        options.ServiceBusReceiverOptions ??= new();
    }
}
