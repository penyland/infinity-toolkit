using Infinity.Toolkit.Messaging.Diagnostics;
using System.Runtime.CompilerServices;

namespace Infinity.Toolkit.Messaging.InMemory;

internal class InMemoryChannelReceiver
{
    private readonly string channelName;
    private readonly ILogger logger;
    private readonly ClientDiagnostics clientDiagnostics;

    private readonly ConcurrentDictionary<long, InMemoryMessage> deferredMessages = new();

    public string ChannelName => channelName;

    public InMemoryChannelReceiver(string channelName, ILoggerFactory loggerFactory)
    {
        this.channelName = channelName;
        logger = loggerFactory.CreateLogger<InMemoryChannelReceiver>();
        clientDiagnostics = new ClientDiagnostics(InMemoryBusDefaults.System, InMemoryBusDefaults.Name, ChannelName, InMemoryBusDefaults.System);
    }

    public Task<InMemoryReceivedMessage> ReceiveDeferredMessageAsync(long sequenceNumber)
    {
        if (deferredMessages.TryGetValue(sequenceNumber, out var message))
        {
            var receivedMessage = new InMemoryReceivedMessage(message);
            deferredMessages.TryRemove(sequenceNumber, out _);
            logger?.DeferredMessageConsumed(sequenceNumber, ChannelName);
            return Task.FromResult(receivedMessage);
        }
        else
        {
            throw new InvalidOperationException("The message has already been received.");
        }
    }

    public Task<IReadOnlyList<InMemoryReceivedMessage>> ReceiveDeferredMessagesAsync(IReadOnlyCollection<long> sequenceNumbers)
    {
        var receivedMessages = new List<InMemoryReceivedMessage>();

        foreach (var sequenceNumber in sequenceNumbers)
        {
            if (deferredMessages.TryGetValue(sequenceNumber, out var message))
            {
                var receivedMessage = new InMemoryReceivedMessage(message);
                deferredMessages.TryRemove(sequenceNumber, out _);
                receivedMessages.Add(receivedMessage);
            }
            else
            {
                throw new InvalidOperationException("The message has already been received.");
            }
        }

        logger?.DeferredMessagesConsumed(channelName);
        return Task.FromResult<IReadOnlyList<InMemoryReceivedMessage>>(receivedMessages);
    }

    public async IAsyncEnumerable<InMemoryReceivedMessage> ReceiveDeferredMessagesAsAsyncEnumerable(IReadOnlyCollection<long> sequenceNumbers, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var sequenceNumber in sequenceNumbers)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }

            if (deferredMessages.TryGetValue(sequenceNumber, out var message))
            {
                var receivedMessage = new InMemoryReceivedMessage(message);
                deferredMessages.TryRemove(sequenceNumber, out _);
                yield return receivedMessage;
            }
            else
            {
                throw new InvalidOperationException("The message has already been received.");
            }
        }

        logger?.DeferredMessagesConsumed(channelName);
        await Task.CompletedTask;
    }

    public Task<long> DeferMessageAsync(InMemoryMessage message, IDictionary<string, object?>? propertiesToModify = null, CancellationToken cancellationToken = default)
    {
        using var scope = clientDiagnostics.CreateDiagnosticActivityScope(
            ActivityKind.Consumer,
            DiagnosticProperty.OperationDefer,
            DiagnosticProperty.OperationReceive,
            message.ApplicationProperties);
        scope?.SetTag(DiagnosticProperty.MessagingDestinationName, ChannelName);
        scope?.SetTag(DiagnosticProperty.MessagingMessageId, message.MessageId);
        scope?.SetTag(DiagnosticProperty.MessageBusSequenceNumber, message.SequenceNumber);

        if (deferredMessages.TryAdd(message.SequenceNumber, message))
        {
            logger?.DeferredMessage(message.SequenceNumber, ChannelName);
            return Task.FromResult(message.SequenceNumber);
        }
        else
        {
            throw new InvalidOperationException($"The message with sequence number {message.SequenceNumber} has already been deferred.");
        }
    }
}
