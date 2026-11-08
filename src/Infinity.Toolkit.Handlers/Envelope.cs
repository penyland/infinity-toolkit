using System.Diagnostics;

namespace Infinity.Toolkit.Handlers;

/// <summary>
/// Represents a message wrapped in an envelope.
/// </summary>
internal record Envelope
{
    public BinaryData Payload { get; internal set; }

    public Type Type { get; init; }

    public string MessageId { get; internal set; }

    public DateTimeOffset EnqueuedTimeUtc { get; internal set; }

    public long SequenceNumber { get; internal set; }

    public ActivityContext ParentContext { get; internal set; }
}
