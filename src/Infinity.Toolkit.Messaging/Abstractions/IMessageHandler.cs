namespace Infinity.Toolkit.Messaging.Abstractions;

/// <summary>
/// Defines a handler for a message.
/// </summary>
public interface IMessageHandler
{
    Task Handle(IMessageHandlerContext context, CancellationToken cancellationToken);
}

/// <summary>
/// Defines a handler for a message of type <typeparamref name="TMessage"/>.
/// </summary>
/// <typeparam name="TMessage">The type of message.</typeparam>
public interface IMessageHandler<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Handles a message.
    /// </summary>
    /// <param name="context">The context including the deserialized message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An awaitable Task.</returns>
    Task Handle(IMessageHandlerContext<TMessage> context, CancellationToken cancellationToken);
}
