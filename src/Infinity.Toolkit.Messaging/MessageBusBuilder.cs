using Infinity.Toolkit.Messaging.Abstractions;

namespace Infinity.Toolkit.Messaging;

public sealed class MessageBusBuilder(IServiceCollection services)
{
    public IServiceCollection Services { get; } = services ?? throw new ArgumentNullException(nameof(services));
}

public static partial class MessageBusBuilderExtensions
{
    /// <summary>
    /// Map a message handler to a message type of type <typeparamref name="TMessage"/> on all registered brokers.
    /// Message handlers are registered as transient services.
    ///
    /// </summary>
    /// <typeparam name="TMessage">The type of the message.</typeparam>
    /// <typeparam name="TMessageHandler">The type of the message handler.</typeparam>
    /// <param name="configure">An optional lambda used for configuring the message handler.</param>
    /// <returns>A <see cref="MessageHandlerBuilder"/> that can be used to configure the handler.</returns>
    public static MessageHandlerBuilder MapMessageHandler<TMessage, TMessageHandler>(this MessageBusBuilder builder, Func<IServiceProvider, TMessageHandler>? configure = null)
        where TMessage : class
        where TMessageHandler : class, IMessageHandler<TMessage>
    {
        // Check if the message handler type is already registered
        if (builder.Services.Any(x => x.ServiceType == typeof(TMessageHandler)))
        {
            throw new InvalidOperationException($"The message handler {typeof(TMessageHandler).Name} is already registered.");
        }

        // Check that TMessagehandler implements IMessageHandler
        if (!typeof(TMessageHandler).GetInterfaces().Any(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IMessageHandler<>)))
        {
            throw new InvalidOperationException($"The message handler {typeof(TMessageHandler).Name} does not implement the IMessageHandler interface.");
        }

        var genericType = typeof(IMessageHandler<>).MakeGenericType(typeof(TMessage));

        if (configure is not null)
        {
            builder.Services
                .AddTransient<TMessageHandler, TMessageHandler>(configure)
                .AddTransient(genericType, configure);
        }
        else
        {
            builder.Services
                .AddTransient<TMessageHandler, TMessageHandler>()
                .AddTransient(genericType, typeof(TMessageHandler));
        }

        return new MessageHandlerBuilder(builder.Services);
    }

    /// <summary>
    /// Adds an `IMessagingErrorHandler` implementation to services. `IMessagingErrorHandler` implementations
    /// are used by the exception handler middleware to handle unexpected request exceptions.
    /// Multiple handlers can be added and they're called by the middleware in the order
    /// they're added.
    /// </summary>
    /// <typeparam name="TErrorHandler">The type of the exception handler implementation.</typeparam>
    /// <returns>A <see cref="MessageHandlerBuilder"/> that can be used to configure the handler.</returns>
    public static MessageBusBuilder AddExceptionHandler<TErrorHandler>(this MessageBusBuilder builder)
        where TErrorHandler : class, IMessagingExceptionHandler
    {
        builder.Services.AddSingleton<IMessagingExceptionHandler, TErrorHandler>();
        return builder;
    }

    /// <summary>
    /// Adds an `IMessagingErrorHandler` implementation to services. `IMessagingErrorHandler` implementations
    /// are used by the exception handler middleware to handle unexpected request exceptions.
    /// Multiple handlers can be added and they're called by the middleware in the order
    /// they're added.
    /// </summary>
    /// <typeparam name="TErrorHandler">The type of the exception handler implementation.</typeparam>
    /// <param name="implementationInstance">The instance of the service.</param>
    /// <returns>A <see cref="MessageHandlerBuilder"/> that can be used to configure the handler.</returns>
    public static MessageBusBuilder AddExceptionHandler<TErrorHandler>(this MessageBusBuilder builder, TErrorHandler implementationInstance)
        where TErrorHandler : class, IMessagingExceptionHandler
    {
        builder.Services.AddSingleton<IMessagingExceptionHandler>(implementationInstance);
        return builder;
    }

    /// <summary>
    /// Add a broker of type <typeparamref name="TBroker"/> with options of type <typeparamref name="TBrokerOptions"/> to the message bus.
    /// </summary>
    /// <typeparam name="TBroker">The type of broker.</typeparam>
    /// <typeparam name="TBrokerOptions">The type of broker options.</typeparam>
    /// <param name="builder">The message bus builder.</param>
    /// <param name="brokerType">The type of broker.</param>
    /// <param name="configureOptions">The action to configure the broker options.</param>
    /// <returns>Returns the MessageBusBuilder used to add the broker.</returns>
    internal static MessageBusBuilder AddBroker<TBroker, TBrokerOptions>(this MessageBusBuilder builder, string brokerType, Action<TBrokerOptions> configureOptions)
        where TBroker : class, IBroker
        where TBrokerOptions : MessageBusBrokerOptions
    {
        // Check that the broker type is not already registered
        if (builder.Services.Any(x => x.ServiceKey is null && x.ImplementationType == typeof(TBroker)))
        {
            // Log a warning
            return builder;
        }

        builder.Services.Configure(configureOptions);
        builder.Services.TryAddSingleton<IBroker, TBroker>();

        return builder;
    }
}
