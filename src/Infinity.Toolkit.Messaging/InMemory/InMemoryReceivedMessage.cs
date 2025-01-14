namespace Infinity.Toolkit.Messaging.InMemory;

public sealed class InMemoryReceivedMessage
{
    internal InMemoryReceivedMessage(BinaryData body)
    {
        Body = body ?? throw new ArgumentNullException(nameof(body));
    }

    internal InMemoryReceivedMessage(InMemoryMessage message)
        : this(message?.Body ?? throw new ArgumentNullException(nameof(message)))
    {
        ContentType = message.ContentType;
        CorrelationId = message.CorrelationId;
        MessageId = message.MessageId;
        EnqueuedTimeUtc = message.EnqueuedTimeUtc;
        ScheduledEnqueueTime = message.ScheduledEnqueueTime ?? EnqueuedTimeUtc;
        SequenceNumber = message.SequenceNumber;
        ApplicationProperties = (IReadOnlyDictionary<string, object?>)message.ApplicationProperties;
    }

    public string? ContentType { get; }

    public string? CorrelationId { get; }

    public string MessageId { get; }

    public DateTimeOffset ScheduledEnqueueTime { get; }

    public DateTimeOffset EnqueuedTimeUtc { get; internal set; }

    public long SequenceNumber { get; set; }

    public BinaryData Body { get; set; } = default!;

    public IReadOnlyDictionary<string, object?> ApplicationProperties { get; set; } = new Dictionary<string, object?>();
}
