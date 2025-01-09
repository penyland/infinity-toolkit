using Infinity.Toolkit.Messaging;
using Infinity.Toolkit.Messaging.Abstractions;

namespace Infinity.Toolkit.Messaging;

public sealed class MessageHandlerBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = services ?? throw new ArgumentNullException(nameof(services));
}

public static class MessageHandlerBuilderExtensions
{
    /// <summary>
    /// Decorate a message handler of type <typeparamref name="TMessageHandler"/> for messages of type <typeparamref name="TMessage"/>.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="TMessageHandler">The type of the message handler.</typeparam>
    /// <returns>Returns the MessageBusBuilder.</returns>
    public static MessageHandlerBuilder Decorate<TMessage, TMessageHandler>(this MessageHandlerBuilder builder)
        where TMessage : class
        where TMessageHandler : class, IMessageHandler<TMessage>
    {
        builder.Services.Decorate<IMessageHandler<TMessage>, TMessageHandler>();
        return builder;
    }
}
