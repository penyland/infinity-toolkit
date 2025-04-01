namespace Infinity.Toolkit.Messaging;

public class MessageBusBrokerOptions
{
    /// <summary>
    /// Gets or sets whether the message broker should start listening for messages automatically. Default is <c>true</c>.
    /// </summary>
    public bool AutoStartListening { get; set; } = true;

    /// <summary>
    /// Gets or sets the display name of the broker.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the global <see cref="JsonSerializerOptions"/> to use when deserializing the message body. If not set on a channel, this will be used.
    /// </summary>
    [JsonIgnore]
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }

    /// <summary>
    /// Gets the channel consumer registry.
    /// </summary>
    [JsonIgnore]
    internal Dictionary<string, ChannelConsumerRegistration> ChannelConsumerRegistry { get; } = [];

    internal Dictionary<string, ChannelProducerRegistration> ChannelProducerRegistry { get; } = [];
}

internal class ChannelProducerRegistration
{
    public string BrokerName { get; set; } = string.Empty;

    public Type? EventType { get; set; }

    public object? Key { get; set; }
}

internal class ConfigureMessageBusBrokerOptions : IPostConfigureOptions<MessageBusBrokerOptions>
{
    public void PostConfigure(string? name, MessageBusBrokerOptions options)
    {
        options.JsonSerializerOptions ??= new();
    }
}
