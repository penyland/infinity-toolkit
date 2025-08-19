using Microsoft.Extensions.DependencyInjection;

namespace Infinity.Toolkit.Handlers;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds a request handler to the service collection.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TRequestHandler">The request handler type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>A <see cref="RequestHandlerBuilder"/> instance used to configure the request handler.</returns>
    public static RequestHandlerBuilder AddRequestHandler<TRequest, TRequestHandler>(this IServiceCollection services)
        where TRequest : class
        where TRequestHandler : class, IRequestHandler<TRequest>
    {
        services.AddScoped<IRequestHandler<TRequest>, TRequestHandler>();      
        return new RequestHandlerBuilder(services, typeof(IRequestHandler<TRequest>), typeof(TRequestHandler));
    }

    /// <summary>
    /// Adds a request handler to the service collection that returns a result.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TRequestHandler">The request handler type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>A <see cref="RequestHandlerBuilder"/> instance used to configure the request handler.</returns>
    public static RequestHandlerBuilder AddRequestHandler<TRequest, TResult, TRequestHandler>(this IServiceCollection services)
        where TRequest : class
        where TResult : class
        where TRequestHandler : class, IRequestHandler<TRequest, TResult>
    {
        services.AddScoped<IRequestHandler<TRequest, TResult>, TRequestHandler>();
        return new RequestHandlerBuilder(services, typeof(IRequestHandler<TRequest, TResult>), typeof(TRequestHandler));
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
        return services.Decorate<IRequestHandler<TRequest>, TDecorator>();
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
        return services.Decorate<IRequestHandler<TRequest, TResult>, TDecorator>();
    }
}

/// <summary>
/// A builder used to configure a request handler.
/// </summary>
/// <param name="services"></param>
/// <param name="handlerType"></param>
public sealed class RequestHandlerBuilder(IServiceCollection services, Type serviceType, Type handlerType)
{
    internal IServiceCollection Services { get; } = services;

    internal Type ServiceType { get; } = serviceType;

    internal Type HandlerType { get; } = handlerType;
}

public static class RequestHandlerBuilderExtensions
{
    /// <summary>
    /// Decorates all registered instances of <see cref="IRequestHandler{TRequest}"/> using the specified type <typeparamref name="TDecoratedRequestHandler"/>.
    /// </summary>
    /// <typeparam name="TDecoratedRequestHandler">The decorator type.</typeparam>
    /// <returns>A <see cref="RequestHandlerBuilder"/> instance used to configure the request handler.</returns>
    public static RequestHandlerBuilder Decorate<TDecoratedRequestHandler>(this RequestHandlerBuilder builder)
        where TDecoratedRequestHandler : class
    {
        builder.Services.Decorate(builder.ServiceType, typeof(TDecoratedRequestHandler));
        return builder;
    }
}
