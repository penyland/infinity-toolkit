namespace Infinity.Toolkit.Messaging.Abstractions;

public interface IMessageBus
{
    /// <summary>
    /// Checks if the message bus is processing messages.
    /// </summary>
    bool IsProcessing { get; }

    /// <summary>
    /// All registered message brokers on the message bus.
    /// </summary>
    IEnumerable<IBroker> Brokers { get; }

    /// <summary>
    /// Initializes the message bus.
    /// </summary>
    Task InitAsync();

    /// <summary>
    /// Starts the message bus.
    /// </summary>
    /// <param name="forceStart">A flag to force start all message brokers.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task StartAsync(bool forceStart = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Starts the message broker <paramref name="messageBroker"/>.
    /// </summary>
    /// <param name="messageBroker">A message broker.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task StartAsync(IBroker messageBroker, CancellationToken cancellationToken);

    /// <summary>
    /// Stops the message bus.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task StopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops the message broker <paramref name="messageBroker"/>.
    /// </summary>
    /// <param name="messageBroker">A message broker.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    Task StopAsync(IBroker messageBroker, CancellationToken cancellationToken);
}
