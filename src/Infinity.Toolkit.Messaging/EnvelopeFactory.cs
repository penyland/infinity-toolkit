namespace Infinity.Toolkit.Messaging;

public sealed class EnvelopeFactory(IOptions<MessageBusOptions> messageBusOptions)
{
    private readonly MessageBusOptions messageBusOptions = messageBusOptions.Value;

    public Envelope CreateEnvelope<T>(T message)
    {
        var envelope = new EnvelopeBuilder()
            .WithBody(message)
            .WithSource(messageBusOptions.Source)
            .WithEventType<T>(messageBusOptions.EventTypeIdentifierPrefix)
            .Build();

        return envelope;
    }
}
