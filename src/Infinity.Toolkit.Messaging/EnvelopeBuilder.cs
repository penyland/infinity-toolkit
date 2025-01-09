namespace Infinity.Toolkit.Messaging;

/// <summary>
/// Builds an <see cref="Envelope"/>.
/// </summary>
public sealed class EnvelopeBuilder
{
    private readonly Envelope envelope = new();

    public EnvelopeBuilder()
    {
        envelope.ApplicationProperties[CloudEvents.SpecVersion] = CloudEvents.CloudEventsSpecVersion;
        envelope.ApplicationProperties[CloudEvents.DataContentType] = MediaTypeNames.Application.Json;
        envelope.ApplicationProperties[Constants.Environment] =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Production";
        // Set message id
        envelope.MessageId = Guid.NewGuid().ToString();
        envelope.ApplicationProperties[CloudEvents.Id] = envelope.MessageId;
    }

    public EnvelopeBuilder WithBody<T>(T payload, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        envelope.Body = new BinaryData(payload, jsonSerializerOptions);
        return this;
    }

    public EnvelopeBuilder WithMessageId(string messageId)
    {
        var id = messageId ?? Guid.NewGuid().ToString();
        envelope.ApplicationProperties[CloudEvents.Id] = id;
        envelope.MessageId = id;
        return this;
    }

    public EnvelopeBuilder WithContentType(string contentType = MediaTypeNames.Application.Json)
    {
        envelope.ApplicationProperties[CloudEvents.DataContentType] = contentType;
        envelope.ContentType = contentType;
        return this;
    }

    public EnvelopeBuilder WithCorrelationId(string? correlationId)
    {
        envelope.CorrelationId = correlationId;
        return this;
    }

    public EnvelopeBuilder WithEventType(string eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        envelope.ApplicationProperties[CloudEvents.Type] = eventType.ToLower();
        return this;
    }

    public EnvelopeBuilder WithSource(Uri source)
    {
        ArgumentNullException.ThrowIfNull(source);
        envelope.ApplicationProperties[CloudEvents.Source] = source.OriginalString;
        return this;
    }

    public EnvelopeBuilder WithHeaders(IDictionary<string, object?>? headers)
    {
        headers?.ForEach(x => envelope.ApplicationProperties.TryAdd(x.Key, x.Value));
        return this;
    }

    public EnvelopeBuilder WithApplicationProperty(string key, object? value)
    {
        ArgumentNullException.ThrowIfNull(key);
        envelope.ApplicationProperties[key] = value;
        return this;
    }

    public Envelope Build()
    {
        return envelope;
    }
}

public static partial class EnvelopeBuilderExtensions
{
    public static EnvelopeBuilder WithEventType(this EnvelopeBuilder builder, string eventType) =>
        builder.WithEventType(eventType.ToLowerInvariant());

    public static EnvelopeBuilder WithEventType(this EnvelopeBuilder builder, string eventTypePrefix, string eventType) =>
        builder.WithEventType($"{eventTypePrefix}.{eventType}".ToLowerInvariant());

    public static EnvelopeBuilder WithEventType<T>(this EnvelopeBuilder builder) =>
        builder.WithEventType(typeof(T).Name.ToLowerInvariant());

    public static EnvelopeBuilder WithEventType<T>(this EnvelopeBuilder builder, string eventTypePrefix) =>
        builder.WithEventType($"{eventTypePrefix}.{typeof(T).Name}".ToLowerInvariant());
}
