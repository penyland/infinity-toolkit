using Infinity.Toolkit.Messaging.Abstractions;
using Infinity.Toolkit.Messaging.AzureServiceBus;
using Infinity.Toolkit.Messaging.Diagnostics;
using static Infinity.Toolkit.Messaging.Diagnostics.Errors;

namespace Infinity.Toolkit.Messaging.AzureServiceBus;

internal sealed class DeferredChannelConsumer<T>(
    AzureServiceBusClientFactory clientFactory,
    IOptionsMonitor<AzureServiceBusDeferredChannelConsumerOptions> deferredChannelConsumerOptions,
    IOptions<MessageBusOptions> messageBusOptions,
    IOptions<AzureServiceBusOptions> brokerOptions,
    Metrics messageBusMetrics,
    ILogger<DeferredChannelConsumer<T>>? logger) : IDeferredChannelConsumer<T>
    where T : class
{
    private readonly AzureServiceBusDeferredChannelConsumerOptions options = deferredChannelConsumerOptions.Get(typeof(T).AssemblyQualifiedName) ?? throw new ArgumentException(nameof(AzureServiceBusDeferredChannelConsumerOptions));
    private readonly AzureServiceBusClientFactory clientFactory = clientFactory;
    private readonly Metrics messageBusMetrics = messageBusMetrics;
    private readonly ILogger<DeferredChannelConsumer<T>>? logger = logger;
    private readonly MessageBusOptions messageBusOptions = messageBusOptions.Value;
    private readonly AzureServiceBusOptions brokerOptions = brokerOptions.Value;

    /// <inheritdoc/>
    public async Task<IMessageHandlerContext<T>> ConsumeDeferredMessageAsync(long sequenceNumber, bool autoPurgeMessage = true)
    {
        try
        {
            await using var receiver = CreateServiceBusReceiver(options);
            var serviceBusReceivedMessage = await receiver.ReceiveDeferredMessageAsync(sequenceNumber);
            var messageContext = CreateMessageContext<T>(serviceBusReceivedMessage);

            if (autoPurgeMessage)
            {
                await receiver.CompleteMessageAsync(serviceBusReceivedMessage);
            }

            logger?.DeferredMessageConsumed(sequenceNumber, options.ChannelName);
            messageBusMetrics.RecordMessageConsumed<T>(AzureServiceBusDefaults.Name, receiver.EntityPath, operationName: DiagnosticProperty.OperationDefer);
            return messageContext;
        }
        catch
        {
            logger?.CouldNotConsumeDeferredMessage(sequenceNumber, options.ChannelName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<IMessageHandlerContext<T>>> ConsumeDeferredMessagesAsync(IReadOnlyCollection<long> sequenceNumbers, bool autoPurgeMessage = true)
    {
        try
        {
            var messageContexts = new List<IMessageHandlerContext<T>>();

            await using var receiver = CreateServiceBusReceiver(options);
            var messages = await receiver.ReceiveDeferredMessagesAsync(sequenceNumbers);
            foreach (var message in messages)
            {
                if (message != null)
                {
                    var messageContext = CreateMessageContext<T>(message);

                    if (autoPurgeMessage)
                    {
                        await receiver.CompleteMessageAsync(message);
                    }

                    messageContexts.Add(messageContext);
                }
            }

            logger?.DeferredMessagesConsumed(options.ChannelName);
            messageBusMetrics.RecordMessageConsumed<T>(AzureServiceBusDefaults.Name, receiver.EntityPath, messageContexts.Count, operationName: DiagnosticProperty.OperationDefer);
            return messageContexts;
        }
        catch
        {
            logger?.CouldNotConsumeDeferredMessages(options.ChannelName);
            throw;
        }
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<IMessageHandlerContext<T>> ConsumeDeferredMessagesAsAsyncEnumerable(IReadOnlyCollection<long> sequenceNumbers, bool autoPurgeMessage = true)
    {
        await using var receiver = CreateServiceBusReceiver(options);
        IReadOnlyList<ServiceBusReceivedMessage> messages;

        try
        {
            messages = await receiver.ReceiveDeferredMessagesAsync(sequenceNumbers);
        }
        catch
        {
            logger?.CouldNotConsumeDeferredMessages(options.ChannelName);
            throw;
        }

        foreach (var message in messages)
        {
            if (message != null)
            {
                var messageContext = CreateMessageContext<T>(message);

                if (autoPurgeMessage)
                {
                    await receiver.CompleteMessageAsync(message);
                }

                messageBusMetrics.RecordMessageConsumed<T>(AzureServiceBusDefaults.Name, receiver.EntityPath, operationName: DiagnosticProperty.OperationDefer);
                yield return messageContext;
            }
        }

        logger?.DeferredMessagesConsumed(options.ChannelName);
    }

    private IMessageHandlerContext<TMessage> CreateMessageContext<TMessage>(ServiceBusReceivedMessage message)
        where TMessage : class
    {
        var jsonSerializerOptions =
                    options.JsonSerializerOptions
                    ?? brokerOptions.JsonSerializerOptions
                    ?? messageBusOptions.JsonSerializerOptions
                    ?? new JsonSerializerOptions();

        var messageBody = message.Body.ToObjectFromJson<TMessage>(jsonSerializerOptions) ?? throw new JsonException($"{CouldNotDeserializeJsonToType} {typeof(TMessage)}");

        var messageContext = new AzureServiceBusBrokerMessageHandlerContext<TMessage>
        {
            Body = message.Body,
            Headers = message.ApplicationProperties,
            Message = messageBody,
            SequenceNumber = message.SequenceNumber,
        };

        return messageContext;
    }

    private ServiceBusReceiver CreateServiceBusReceiver(AzureServiceBusDeferredChannelConsumerOptions options)
    {
        return options.ChannelType switch
        {
            ChannelType.Topic => clientFactory.CreateReceiver(options.ChannelName, options.SubscriptionName, options.ServiceBusReceiverOptions),
            ChannelType.Queue => clientFactory.CreateReceiver(options.ChannelName, options.ServiceBusReceiverOptions),
            _ => throw new ArgumentOutOfRangeException(nameof(options.ChannelType))
        };
    }
}
