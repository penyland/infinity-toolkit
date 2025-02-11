namespace Infinity.Toolkit.Messaging.InMemory;


public interface IChannelProducer2
{
    Task SendAsync(object message);

    Task SendAsync<T>(T message);

    Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken);
}

internal class InMemoryChannelProducer2(InMemoryChannelClientFactory clientFactory, IOptionsMonitor<InMemoryChannelProducerOptions> channelProducerOptions) : IChannelProducer2
{
    private readonly JsonSerializerOptions jsonSerializerOptions = new();

    public Task SendAsync(object message)
    {
        var envelope = new EnvelopeBuilder()
                .WithBody(message, jsonSerializerOptions)
                .WithMessageId(Guid.NewGuid().ToString())
                //.WithContentType(contentType)
                //.WithCorrelationId(correlationId)
                //.WithEventType(typeof(T).Name)
                //.WithSource(channelProducerOptions.Source)
                //.WithHeaders(headers)
                .Build();

        return SendEnvelopeAsync(envelope, CancellationToken.None);
    }

    public Task SendAsync<T>(T payload)
    {
        var envelope = new EnvelopeBuilder()
                .WithBody(payload, jsonSerializerOptions)
                .WithMessageId(Guid.NewGuid().ToString())
                //.WithContentType(contentType)
                //.WithCorrelationId(correlationId)
                .WithEventType(typeof(T).AssemblyQualifiedName ?? typeof(T).Name)
                //.WithSource(channelProducerOptions.Source)
                //.WithHeaders(headers)
                .Build();

        return SendEnvelopeAsync(envelope, CancellationToken.None);
    }

    public Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken)
    {
        var options = channelProducerOptions.Get("default");
        var sender = clientFactory.GetSender(options.ChannelName);
        var inMemoryMessage = envelope.ToInMemoryMessage();
        return sender.SendAsync(inMemoryMessage, cancellationToken);
    }
}
