using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infinity.Toolkit.Experimental.Mediator;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection services)
    {
        services.TryAddTransient<IMediator, Mediator>();
        return services;
    }

    public static IServiceCollection AddMediatorHandler<TRequest, TMediatorHandler>(this IServiceCollection services)
        where TRequest : class, ICommand
        where TMediatorHandler : class, IMediatorHandler<TRequest>
    {
        return services.AddTransient<IMediatorHandler<TRequest>, TMediatorHandler>();
    }

    public static IServiceCollection AddMediatorHandler<TRequest, TRequestResult, TMediatorHandler>(this IServiceCollection services)
        where TRequest : class, IQuery
        where TMediatorHandler : class, IMediatorHandler<TRequest, TRequestResult>
    {
        return services.AddTransient<IMediatorHandler<TRequest, TRequestResult>, TMediatorHandler>();
    }

    /// <summary>
    /// Decorates all registered instance of <see cref="IMediatorHandler{TRequest}"/> using the specified type <typeparamref name="TDecorator"/>.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    public static IServiceCollection DecorateMediatorHandler<TRequest, TDecorator>(this IServiceCollection services)
        where TDecorator : class, IMediatorHandler<TRequest>
        where TRequest : class, ICommand => services.Decorate<IMediatorHandler<TRequest>, TDecorator>();

    /// <summary>
    /// Decorates all registered instance of <see cref="IMediatorHandler{TQuery, TQueryResult}"/> using the specified type <typeparamref name="TDecorator"/>.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    public static IServiceCollection DecorateMediatorHandler<TQuery, TQueryResult, TDecorator>(this IServiceCollection services)
        where TDecorator : class, IMediatorHandler<TQuery, TQueryResult>
        where TQuery : class, IQuery => services.Decorate<IMediatorHandler<TQuery, TQueryResult>, TDecorator>();
}
