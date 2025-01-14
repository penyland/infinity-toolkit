namespace Infinity.Toolkit.Messaging.InMemory;

/// <summary>
/// Represents the options for the in-memory broker channel.
/// </summary>
public sealed class InMemoryChannelConsumerOptions : ChannelConsumerOptions
{
    /// <summary>
    /// Gets an optional predicate to filter messages.
    /// </summary>
    public Predicate<InMemoryMessage>? Predicate { get; set; } = x => true;
}

internal class ConfigureInMemoryBusChannelOptions(IOptions<MessageBusOptions> options) : IPostConfigureOptions<InMemoryChannelConsumerOptions>
{
    private readonly MessageBusOptions messageBusOptions = options.Value;

    public void PostConfigure(string? name, InMemoryChannelConsumerOptions options)
    {
        options.EventTypeName ??= options.EventType?.Name.ToLowerInvariant() ?? string.Empty;
        options.JsonSerializerOptions ??= messageBusOptions?.JsonSerializerOptions;
    }
}
