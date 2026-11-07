using System.Diagnostics;

namespace Infinity.Toolkit.Handlers;

/// <summary>
/// Represents a message wrapped in an envelope.
/// </summary>
internal record Envelope
{
    public IEvent Event { get; init; }

    public string EventType { get; init; }

    public string MessageId { get; internal set; }

    public DateTimeOffset EnqueuedTimeUtc { get; internal set; }

    public long SequenceNumber { get; internal set; }

    public ActivityContext ParentContext { get; internal set; }
}
