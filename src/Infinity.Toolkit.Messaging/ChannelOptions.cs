namespace Infinity.Toolkit.Messaging;

/// <summary>
/// The type of channels.
/// </summary>
public enum ChannelType
{
    Topic,
    Queue
}

/// <summary>
/// Represents the options for a channel.
/// </summary>
public abstract class ChannelOptions
{
    /// <summary>
    /// Gets or sets the type of the channel.
    /// </summary>
    public ChannelType ChannelType { get; set; } = ChannelType.Topic;

    /// <summary>
    /// The name of the channel.
    /// </summary>
    [Required]
    public string ChannelName { get; set; } = string.Empty;

    /// <summary>
    /// The property <see cref="EventTypeName" /> is used to identify the type of the event to be able to find the correct handler.
    ///
    /// The value MUST be the same as the value of the cloudEvents_type application header property in the received message, if not the message will not be processed.
    ///
    /// The default value is the name of the event type in lower case, without the namespace that the channel consumer is registered for.
    /// </summary>
    public string? EventTypeName { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="JsonSerializerOptions"/> to use when deserializing the message body.
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; } = default;

    /// <summary>
    /// Gets or sets the type of the event.
    /// </summary>
    internal Type EventType { get; set; } = default!;
}
