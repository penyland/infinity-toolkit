using System.Net.Mime;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using System.Threading;

namespace Infinity.Toolkit.Messaging.InMemory;


public interface IChannelProducer2
{
    Task SendAsync(object message);

    Task SendAsync<T>(T message);

    Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken);
}

internal class InMemoryChannelProducer2 : IChannelProducer2
{
    private readonly JsonSerializerOptions jsonSerializerOptions = new();

    public Task SendAsync(object message)
    {
        throw new NotImplementedException();
    }

    public Task SendAsync<T>(T payload)
    {
        var envelope = new EnvelopeBuilder()
                .WithBody(payload, jsonSerializerOptions)
                .WithMessageId(Guid.NewGuid().ToString())
                //.WithContentType(contentType)
                //.WithCorrelationId(correlationId)
                .WithEventType(typeof(T).Name)
                //.WithSource(channelProducerOptions.Source)
                //.WithHeaders(headers)
                .Build();

        return SendEnvelopeAsync(envelope, CancellationToken.None);
    }

    public Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
