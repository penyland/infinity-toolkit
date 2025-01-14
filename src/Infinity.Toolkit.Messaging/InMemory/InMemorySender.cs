using Infinity.Toolkit.Messaging.Diagnostics;

namespace Infinity.Toolkit.Messaging.InMemory;

/// <summary>
/// Represents a sender that can send messages to a channel with the specified name.
/// </summary>
internal sealed class InMemorySender : IAsyncDisposable
{
    private readonly ConcurrentDictionary<long, InMemoryMessage> scheduledMessages = new();
    private readonly ChannelWriter<InMemoryMessage> writer;
    private readonly SequenceNumberGenerator sequenceNumberGenerator;
    private readonly ILogger logger;
    private readonly ClientDiagnostics clientDiagnostics;

    public InMemorySender(string channelName, ChannelWriter<InMemoryMessage> writer, SequenceNumberGenerator sequenceNumberGenerator, ILoggerFactory loggerFactory)
    {
        if (string.IsNullOrEmpty(channelName))
        {
            throw new ArgumentException($"'{nameof(channelName)}' cannot be null or empty.", nameof(channelName));
        }

        this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
        this.sequenceNumberGenerator = sequenceNumberGenerator;

        ChannelName = channelName;
        clientDiagnostics = new ClientDiagnostics(InMemoryBusDefaults.System, InMemoryBusDefaults.Name, channelName, InMemoryBusDefaults.System);
        logger = loggerFactory.CreateLogger<InMemorySender>();
    }

    public string ChannelName { get; }

    public Task CloseAsync()
    {
        writer.Complete();
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await CloseAsync();
        GC.SuppressFinalize(this);
    }

    public async Task<long> ScheduleSendAsync(InMemoryMessage message, DateTimeOffset scheduledEnqueueTimeUtc, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        message.ScheduledEnqueueTime = scheduledEnqueueTimeUtc;
        await SendAsync(message, cancellationToken);

        return message.SequenceNumber;
    }

    public async Task SendAsync(InMemoryMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        using var scope = clientDiagnostics.CreateDiagnosticActivityScope(
            ActivityKind.Producer,
            DiagnosticProperty.OperationSend,
            DiagnosticProperty.OperationPublish,
            message.ApplicationProperties);

        scope?.SetTag(DiagnosticProperty.MessagingDestinationName, ChannelName);
        scope?.SetTag(DiagnosticProperty.MessagingMessageId, message.MessageId);

        message.EnqueuedTimeUtc = DateTimeOffset.UtcNow;
        message.SequenceNumber = sequenceNumberGenerator.Generate();
        message.ApplicationProperties.TryAdd(Constants.EnqueuedSequenceNumberName, message.SequenceNumber);
        message.ApplicationProperties.TryAdd(Constants.EnqueuedTimeUtcName, DateTimeOffset.UtcNow);
        message.ApplicationProperties.TryAdd(DiagnosticProperty.TraceParent, scope?.Id);
        message.ApplicationProperties.TryAdd(DiagnosticProperty.TraceState, scope?.TraceStateString);

        if (message.ScheduledEnqueueTime.HasValue)
        {
            SendDelayed(message);
        }
        else
        {
            await writer.WriteAsync(message, cancellationToken);
        }
    }

    public async Task SendMessagesAsync(IEnumerable<InMemoryMessage> messages, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);

        foreach (var message in messages)
        {
            await writer.WriteAsync(message, cancellationToken);
        }
    }

    internal Task CancelScheduledMessageAsync(long sequenceNumber, CancellationToken cancellationToken)
    {
        if (scheduledMessages.TryGetValue(sequenceNumber, out var message))
        {
            using var scope = clientDiagnostics.CreateDiagnosticActivityScope(
                ActivityKind.Producer,
                DiagnosticProperty.OperationCancel,
                DiagnosticProperty.OperationPublish,
                message.ApplicationProperties);
            scheduledMessages.TryRemove(sequenceNumber, out _);
        }
        else
        {
            logger.MessageWithSequenceNumberNotFoundWhenCancellingScheduledMessage(sequenceNumber);
            throw new InvalidOperationException(LogMessages.MessageWithSequenceNumberNotFound);
        }

        return Task.CompletedTask;
    }

    private void SendDelayed(InMemoryMessage message)
    {
        // Add metrics
        scheduledMessages.TryAdd(message.SequenceNumber, message);

        _ = Task.Run(async () =>
        {
            var delay = message.ScheduledEnqueueTime!.Value - DateTimeOffset.UtcNow;
            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay);
            }

            if (scheduledMessages.TryGetValue(message.SequenceNumber, out var inMemoryMessage))
            {
                await writer.WriteAsync(inMemoryMessage);
                scheduledMessages.TryRemove(message.SequenceNumber, out _);
            }
            else
            {
                throw new InvalidOperationException($"Message with sequence number {message.SequenceNumber} was not found.");
            }
        })
        .ContinueWith(
            _ =>
            {
                if (_.Exception?.InnerException is { } inner)
                {
                    logger?.ErrorSendingScheduledMessage(message.SequenceNumber, inner.Message);
                }
            },
            TaskContinuationOptions.OnlyOnFaulted);
    }
}
