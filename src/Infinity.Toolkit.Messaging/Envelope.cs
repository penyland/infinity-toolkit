namespace Infinity.Toolkit.Messaging;

/// <summary>
/// Represents a message wrapped in an envelope.
/// </summary>
public class Envelope
{
    public BinaryData Body { get; internal set; }

    public string? ContentType { get; internal set; }

    public string? CorrelationId { get; internal set; }

    public string MessageId { get; internal set; }

    public DateTimeOffset EnqueuedTimeUtc { get; internal set; }

    public DateTimeOffset? ScheduledEnqueueTime { get; internal set; }

    public long SequenceNumber { get; internal set; }

    public IDictionary<string, object?> ApplicationProperties { get; init; } = new Dictionary<string, object?>(20);
}
