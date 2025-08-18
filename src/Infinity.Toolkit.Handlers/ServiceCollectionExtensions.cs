using Microsoft.Extensions.DependencyInjection;

namespace Infinity.Toolkit.Handlers;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a request handler to the service collection.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TRequestHandler">The mediator handler type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>A <see cref="RequestHandlerBuilder"/> instance used to configure the request handler.</returns>
    public static IServiceCollection AddRequestHandler<TRequest, TRequestHandler>(this IServiceCollection services)
        where TRequest : class
        where TRequestHandler : class, IRequestHandler<TRequest>
    {
        services.AddTransient<IRequestHandler<TRequest>, TRequestHandler>();
        return services;
    }

    /// <summary>
    /// Adds a mediator handler to the service collection that returns a result.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TRequestHandler">The mediator handler type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>A <see cref="RequestHandlerBuilder"/> instance used to configure the mediator handler.</returns>
    public static IServiceCollection AddRequestHandler<TRequest, TResult, TRequestHandler>(this IServiceCollection services)
        where TRequest : class
        where TResult : class
        where TRequestHandler : class, IRequestHandler<TRequest, TResult>
    {
        services.AddTransient<IRequestHandler<TRequest, TResult>, TRequestHandler>();
        return services;
    }

    /// <summary>
    /// Decorates all registered instance of <see cref="IRequestHandler{TRequest}"/> using the specified type <typeparamref name="TDecorator"/>.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TDecorator">The decorator type.</typeparam>
    public static IServiceCollection DecorateRequestHandler<TRequest, TDecorator>(this IServiceCollection services)
        where TRequest : class
        where TDecorator : class, IRequestHandler<TRequest>
    {
        services.Decorate<IRequestHandler<TRequest>, TDecorator>();
        return services;
    }

    /// <summary>
    /// Decorates all registered instance of <see cref="IRequestHandler{TQuery, TQueryResult}"/> using the specified type <typeparamref name="TDecorator"/>.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TDecorator">The decorator type.</typeparam>
    public static IServiceCollection DecorateRequestHandler<TRequest, TResult, TDecorator>(this IServiceCollection services)
        where TRequest : class
        where TResult : class
        where TDecorator : class, IRequestHandler<TRequest, TResult>
    {
        services.Decorate<IRequestHandler<TRequest, TResult>, TDecorator>();
        return services;
    }
}

/// <summary>
/// A builder used to configure a request handler.
/// </summary>
/// <param name="services"></param>
/// <param name="handlerType"></param>
public sealed class RequestHandlerBuilder(IServiceCollection services, Type handlerType)
{
    internal IServiceCollection Services { get; } = services;

    internal Type HandlerType { get; } = handlerType;
}

public static class RequestHandlerBuilderExtensions
{
    /// <summary>
    /// Decorates all registered instances of <see cref="IRequestHandler{TRequest}"/> using the specified type <typeparamref name="TDecorator"/>.
    /// </summary>
    /// <typeparam name="TDecorator">The decorator type.</typeparam>
    /// <returns>A <see cref="RequestHandlerBuilder"/> instance used to configure the mediator handler.</returns>
    public static RequestHandlerBuilder Decorate<TDecorator>(this RequestHandlerBuilder builder)
        where TDecorator : class => builder.Decorate(typeof(TDecorator));

    /// <summary>
    /// Decorates all registered instances of <see cref="IRequestHandler{TRequest}"/> using the specified type <paramref name="decoratorType"/>.
    /// </summary>
    /// <param name="decoratorType">The decorator type.</param>
    /// <returns>A <see cref="RequestHandlerBuilder"/> instance used to configure the mediator handler.</returns>
    public static RequestHandlerBuilder Decorate(this RequestHandlerBuilder builder, Type decoratorType)
    {
        builder.Services.Decorate(builder.HandlerType, decoratorType);
        return builder;
    }
}
