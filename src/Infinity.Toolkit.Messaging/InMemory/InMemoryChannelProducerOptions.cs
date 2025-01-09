namespace Infinity.Toolkit.Messaging.InMemory;

public sealed class InMemoryChannelProducerOptions : ChannelProducerOptions
{
}

internal class ConfigureInMemoryChannelProducerOptions(IOptions<MessageBusOptions> options) : IPostConfigureOptions<InMemoryChannelProducerOptions>
{
    private readonly MessageBusOptions messageBusOptions = options.Value;

    public void PostConfigure(string? name, InMemoryChannelProducerOptions options)
    {
        options.EventTypeName ??= options.EventType?.Name.ToLowerInvariant() ?? default;
        options.JsonSerializerOptions ??= messageBusOptions.JsonSerializerOptions;
        options.Source ??= new Uri($"im://{messageBusOptions?.ApplicationName}/{messageBusOptions?.Environment}/{options.Name}".ToLowerInvariant());
    }
}
