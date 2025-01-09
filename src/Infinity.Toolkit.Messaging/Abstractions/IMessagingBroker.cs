namespace Infinity.Toolkit.Messaging.Abstractions;

/// <summary>
/// Represents a messaging broker with multiple channels.
/// </summary>
public interface IMessagingBroker
{
    bool AutoStartListening { get; }

    /// <summary>
    /// Checks if the message broker is processing messages.
    /// </summary>
    bool IsProcessing { get; }

    /// <summary>
    /// Gets the name of the broker.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Initialize the channels.
    /// </summary>
    Task InitAsync();

    /// <summary>
    /// Triggered when the bus host is ready to start.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Triggered when the bus host is performing a graceful shutdown.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken);
}
