namespace Infinity.Toolkit.Messaging.InMemory;

public sealed class InMemoryMessage : Envelope
{
    public InMemoryMessage() { }

    public InMemoryMessage(string body)
        : this(BinaryData.FromString(body)) { }

    public InMemoryMessage(ReadOnlyMemory<byte> body)
        : this(BinaryData.FromBytes(body)) { }

    public InMemoryMessage(BinaryData body) => Body = body ?? throw new ArgumentNullException(nameof(body));

    public InMemoryMessage(InMemoryReceivedMessage receivedMessage)
    {
        ArgumentNullException.ThrowIfNull(receivedMessage);

        ContentType = receivedMessage.ContentType;
        CorrelationId = receivedMessage.CorrelationId;
        MessageId = receivedMessage.MessageId;
        Body = receivedMessage.Body;
        ApplicationProperties = (IDictionary<string, object?>)receivedMessage.ApplicationProperties;
        SequenceNumber = receivedMessage.SequenceNumber;
    }
}

internal static class InMemoryMessageExtensions
{
    public static InMemoryMessage ToInMemoryMessage(this Envelope message)
    {
        var inMemoryMessage = new InMemoryMessage
        {
            Body = new BinaryData(message.Body),
            MessageId = message.MessageId,
            ContentType = message.ContentType,
            CorrelationId = message.CorrelationId
        };

        // Add any user defined properties
        message.ApplicationProperties?.ForEach(x => inMemoryMessage.ApplicationProperties.Add(x));

        return inMemoryMessage;
    }
}
