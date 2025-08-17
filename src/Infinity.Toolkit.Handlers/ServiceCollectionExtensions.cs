using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infinity.Toolkit.Handlers;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the mediator to the service collection.
    /// </summary>
    public static IServiceCollection AddMediator(this IServiceCollection services)
    {
        services.TryAddTransient<IMediator, Mediator>();
        return services;
    }

    /// <summary>
    /// Adds a mediator handler to the service collection.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TMediatorHandler">The mediator handler type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>A <see cref="MediatorHandlerBuilder"/> instance used to configure the mediator handler.</returns>
    public static MediatorHandlerBuilder AddMediatorHandler<TRequest, TMediatorHandler>(this IServiceCollection services)
        where TRequest : class, ICommand
        where TMediatorHandler : class, IRequestHandler<TRequest>
    {
        services.AddTransient<IRequestHandler<TRequest>, TMediatorHandler>();
        return new MediatorHandlerBuilder(services, typeof(TRequest));
    }

    /// <summary>
    /// Adds a mediator handler to the service collection that returns a result.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TMediatorHandler">The mediator handler type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>A <see cref="MediatorHandlerBuilder"/> instance used to configure the mediator handler.</returns>
    public static MediatorHandlerBuilder AddMediatorHandler<TRequest, TResult, TMediatorHandler>(this IServiceCollection services)
        where TRequest : class, IQuery
        where TMediatorHandler : class, IRequestHandler<TRequest, TResult>
    {
        services.AddTransient<IRequestHandler<TRequest, TResult>, TMediatorHandler>();
        return new MediatorHandlerBuilder(services, typeof(IRequestHandler<TRequest, TResult>));
    }

    /// <summary>
    /// Decorates all registered instance of <see cref="IRequestHandler{TRequest}"/> using the specified type <typeparamref name="TDecorator"/>.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TDecorator">The decorator type.</typeparam>
    public static IServiceCollection DecorateMediatorHandler<TRequest, TDecorator>(this IServiceCollection services)
        where TDecorator : class, IRequestHandler<TRequest>
        where TRequest : class, ICommand => services.Decorate<IRequestHandler<TRequest>, TDecorator>();

    /// <summary>
    /// Decorates all registered instance of <see cref="IRequestHandler{TQuery, TQueryResult}"/> using the specified type <typeparamref name="TDecorator"/>.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResult">The result type.</typeparam>
    /// <typeparam name="TDecorator">The decorator type.</typeparam>
    public static IServiceCollection DecorateMediatorHandler<TRequest, TResult, TDecorator>(this IServiceCollection services)
        where TDecorator : class, IRequestHandler<TRequest, TResult>
        where TRequest : class, IQuery => services.Decorate<IRequestHandler<TRequest, TResult>, TDecorator>();
}

/// <summary>
/// A builder used to configure a mediator handler.
/// </summary>
/// <param name="services"></param>
/// <param name="handlerType"></param>
public sealed class MediatorHandlerBuilder(IServiceCollection services, Type handlerType)
{
    internal IServiceCollection Services { get; } = services;

    internal Type HandlerType { get; } = handlerType;
}

public static class MediatorHandlerBuilderExtensions
{
    /// <summary>
    /// Decorates all registered instances of <see cref="IRequestHandler{TRequest}"/> using the specified type <typeparamref name="TDecorator"/>.
    /// </summary>
    /// <typeparam name="TDecorator">The decorator type.</typeparam>
    /// <returns>A <see cref="MediatorHandlerBuilder"/> instance used to configure the mediator handler.</returns>
    public static MediatorHandlerBuilder Decorate<TDecorator>(this MediatorHandlerBuilder builder)
        where TDecorator : class => builder.Decorate(typeof(TDecorator));

    /// <summary>
    /// Decorates all registered instances of <see cref="IRequestHandler{TRequest}"/> using the specified type <paramref name="decoratorType"/>.
    /// </summary>
    /// <param name="decoratorType">The decorator type.</param>
    /// <returns>A <see cref="MediatorHandlerBuilder"/> instance used to configure the mediator handler.</returns>
    public static MediatorHandlerBuilder Decorate(this MediatorHandlerBuilder builder, Type decoratorType)
    {
        builder.Services.Decorate(builder.HandlerType, decoratorType);
        return builder;
    }
}
