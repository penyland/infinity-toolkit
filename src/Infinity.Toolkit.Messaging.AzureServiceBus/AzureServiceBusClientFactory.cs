using System.Collections.Concurrent;

namespace Infinity.Toolkit.Messaging.AzureServiceBus;

/// <summary>
/// Represents a factory for creating <see cref="ServiceBusClient"/> instances.
/// </summary>
public sealed class AzureServiceBusClientFactory : IAsyncDisposable
{
    private readonly ServiceBusClient serviceBusClient;
    private readonly ConcurrentDictionary<string, ServiceBusSender> senderCache = new();

    public AzureServiceBusClientFactory(IServiceProvider serviceProvider, IOptions<AzureServiceBusOptions> options)
    {
        // Check if ServiceBusClient is registered in the DI container for example when using .NET Aspire Azure Service Bus component
        if (serviceProvider.GetService<ServiceBusClient>() is not null)
        {
            serviceBusClient = serviceProvider.GetRequiredService<ServiceBusClient>();
            return;
        }

        var azureServiceBusOptions = options.Value;
        serviceBusClient = string.IsNullOrEmpty(azureServiceBusOptions.ConnectionString)
            ? new ServiceBusClient(azureServiceBusOptions.FullyQualifiedNamespace, azureServiceBusOptions.TokenCredential, azureServiceBusOptions.ServiceBusClientOptions)
            : new ServiceBusClient(azureServiceBusOptions.ConnectionString, azureServiceBusOptions.ServiceBusClientOptions);
    }

    /// <summary>
    /// Creates a <see cref="ServiceBusSender"/> for the specified channel name.
    /// </summary>
    /// <param name="channelName">The name of the channel to create the sender for.</param>
    /// <param name="serviceBusSenderOptions">The options to use when creating the sender.</param>
    /// <returns>A <see cref="ServiceBusSender"/> instance.</returns>
    public ServiceBusSender CreateSender(string channelName, ServiceBusSenderOptions? serviceBusSenderOptions = default)
    {
        if (!senderCache.TryGetValue(channelName, out var sender))
        {
            sender = serviceBusClient.CreateSender(channelName, serviceBusSenderOptions);
            senderCache.TryAdd(channelName, sender);
        }

        return sender;
    }

    /// <summary>
    /// Creates a <see cref="ServiceBusProcessor"/> for the specified channel name.
    /// </summary>
    /// <param name="channelName">The name of the channel to create the processor for.</param>
    /// <param name="options">The options to use when creating the processor.</param>
    /// <returns>A <see cref="ServiceBusProcessor"/> instance.</returns>
    public ServiceBusProcessor CreateProcessor(string channelName, ServiceBusProcessorOptions options) => serviceBusClient.CreateProcessor(channelName, options);

    /// <summary>
    /// Creates a <see cref="ServiceBusProcessor"/> for the specified channel name and subscription.
    /// </summary>
    /// <param name="channelName">The name of the channel to create the processor for.</param>
    /// <param name="subscription">The name of the subscription to create the processor for.</param>
    /// <param name="options">The options to use when creating the processor.</param>
    /// <returns>A <see cref="ServiceBusProcessor"/> instance.</returns>
    public ServiceBusProcessor CreateProcessor(string channelName, string subscription, ServiceBusProcessorOptions options) => serviceBusClient.CreateProcessor(channelName, subscription, options);

    /// <summary>
    /// Creates a <see cref="ServiceBusReceiver"/> for the specified channel name.
    /// </summary>
    /// <param name="channelName">The name of the channel to create the receiver for.</param>
    /// <param name="options">The options to use when creating the receiver.</param>
    /// <returns>A <see cref="ServiceBusReceiver"/> instance.</returns>
    public ServiceBusReceiver CreateReceiver(string channelName, ServiceBusReceiverOptions options) => serviceBusClient.CreateReceiver(channelName, options);

    /// <summary>
    /// Creates a <see cref="ServiceBusReceiver"/> for the specified channel name and subscription.
    /// </summary>
    /// <param name="channelName">The name of the channel to create the receiver for.</param>
    /// <param name="subscriptionName">The name of the subscription to create the receiver for.</param>
    /// <param name="options">The options to use when creating the receiver.</param>
    /// <returns>A <see cref="ServiceBusReceiver"/> instance.</returns>
    public ServiceBusReceiver CreateReceiver(string channelName, string subscriptionName, ServiceBusReceiverOptions options) => serviceBusClient.CreateReceiver(channelName, subscriptionName, options);

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return ((IAsyncDisposable)serviceBusClient).DisposeAsync();
    }
}
