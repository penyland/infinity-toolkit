using Infinity.Toolkit.Messaging.Abstractions;
using Infinity.Toolkit.Messaging.Diagnostics;
using static Infinity.Toolkit.Messaging.Diagnostics.Errors;

namespace Infinity.Toolkit.Messaging.InMemory;

public class InMemoryDeferredChannelConsumerOptions : ChannelConsumerOptions { }

internal class InMemoryDeferredChannelConsumer<T>(
    InMemoryChannelClientFactory clientFactory,
    IOptionsMonitor<InMemoryDeferredChannelConsumerOptions> deferredChannelConsumerOptions,
    IOptions<MessageBusOptions> messageBusOptions,
    IOptions<InMemoryBusOptions> brokerOptions,
    MessageBusMetrics messageBusMetrics,
    ILoggerFactory loggerFactory) : IDeferredChannelConsumer<T>
    where T : class
{
    private readonly InMemoryChannelClientFactory clientFactory = clientFactory;
    private readonly MessageBusOptions messageBusOptions = messageBusOptions.Value;
    private readonly InMemoryBusOptions brokerOptions = brokerOptions.Value;
    private readonly MessageBusMetrics messageBusMetrics = messageBusMetrics;
    private readonly ILogger<InMemoryDeferredChannelConsumer<T>> logger = loggerFactory.CreateLogger<InMemoryDeferredChannelConsumer<T>>();
    private readonly InMemoryDeferredChannelConsumerOptions options = deferredChannelConsumerOptions.Get(typeof(T).AssemblyQualifiedName) ?? throw new ArgumentException(nameof(InMemoryDeferredChannelConsumerOptions));

    /// <inheritdoc/>
    public async Task<IMessageHandlerContext<T>> ConsumeDeferredMessageAsync(long sequenceNumber, bool autoPurgeMessage = true)
    {
        try
        {
            var receiver = clientFactory.GetOrAddChannelReceiver(options);
            var message = await receiver.ReceiveDeferredMessageAsync(sequenceNumber);
            var messageContext = CreateMessageContext<T>(message);

            messageBusMetrics.RecordMessageConsumed<T>(InMemoryBusDefaults.Name, receiver.ChannelName, operationName: DiagnosticProperty.OperationDefer);
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

            var receiver = clientFactory.GetOrAddChannelReceiver(options);
            var messages = await receiver.ReceiveDeferredMessagesAsync(sequenceNumbers);
            foreach (var message in messages)
            {
                if (message != null)
                {
                    var messageContext = CreateMessageContext<T>(message);
                    messageContexts.Add(messageContext);
                }
            }

            messageBusMetrics.RecordMessageConsumed<T>(InMemoryBusDefaults.Name, receiver.ChannelName, messageContexts.Count, operationName: DiagnosticProperty.OperationDefer);
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
        var receiver = clientFactory.GetOrAddChannelReceiver(options);
        await foreach (var message in receiver.ReceiveDeferredMessagesAsAsyncEnumerable(sequenceNumbers))
        {
            if (message != null)
            {
                var messageContext = CreateMessageContext<T>(message);
                messageBusMetrics.RecordMessageConsumed<T>(InMemoryBusDefaults.Name, receiver.ChannelName, operationName: DiagnosticProperty.OperationDefer);
                yield return messageContext;
            }
        }
    }

    private InMemoryBrokerMessageHandlerContext<TMessage> CreateMessageContext<TMessage>(InMemoryReceivedMessage message)
        where TMessage : class
    {
        var jsonSerializerOptions =
                    options.JsonSerializerOptions
                    ?? brokerOptions.JsonSerializerOptions
                    ?? messageBusOptions.JsonSerializerOptions
                    ?? new JsonSerializerOptions();

        var messageBody = message.Body.ToObjectFromJson<TMessage>(jsonSerializerOptions) ?? throw new JsonException($"{CouldNotDeserializeJsonToType} {typeof(TMessage)}");

        var messageContext = new InMemoryBrokerMessageHandlerContext<TMessage>
        {
            Body = message.Body,
            Headers = message.ApplicationProperties,
            Message = messageBody,
            SequenceNumber = message.SequenceNumber,
        };

        return messageContext;
    }
}
