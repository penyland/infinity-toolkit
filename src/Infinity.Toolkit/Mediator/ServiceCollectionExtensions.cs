using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infinity.Toolkit.Mediator;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection services)
    {
        services.TryAddTransient<IMediator, Mediator>();
        services.TryAddTransient<ICommandMediator, CommandMediator>();
        services.TryAddTransient<IQueryMediator, QueryMediator>();
        return services;
    }

    public static IServiceCollection AddCommandHandler<TCommand, TCommandHandler>(this IServiceCollection services)
        where TCommand : class, ICommand
        where TCommandHandler : class, ICommandHandler<TCommand>
    {
        return services.AddTransient<ICommandHandler<TCommand>, TCommandHandler>();
    }

    public static IServiceCollection AddCommandHandler<TCommand, TCommandResult, TCommandHandler>(this IServiceCollection services)
        where TCommand : class, ICommand
        where TCommandHandler : class, ICommandHandler<TCommand, TCommandResult>
    {
        return services.AddTransient<ICommandHandler<TCommand, TCommandResult>, TCommandHandler>();
    }

    /// <summary>
    /// Decorates all registered instance of <see cref="ICommandHandler{TCommand}"/> using the specified type <typeparamref name="TDecorator"/>.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    public static IServiceCollection DecorateCommandHandler<TCommand, TDecorator>(this IServiceCollection services)
        where TDecorator : class, ICommandHandler<TCommand>
        where TCommand : class, ICommand => services.Decorate<ICommandHandler<TCommand>, TDecorator>();

    public static IServiceCollection AddQueryHandler<TQuery, TQueryHandler, TResult>(this IServiceCollection services)
        where TQuery : class, IQuery
        where TQueryHandler : class, IQueryHandler<TQuery, TResult>
    {
        return services.AddTransient<IQueryHandler<TQuery, TResult>, TQueryHandler>();
    }

    /// <summary>
    /// Decorates all registered instance of <see cref="IQueryHandler{TQuery, TQueryResult}"/> using the specified type <typeparamref name="TDecorator"/>.
    /// </summary>
    /// <param name="services">The services to add to.</param>
    public static IServiceCollection DecorateQueryHandler<TQuery, TQueryResult, TDecorator>(this IServiceCollection services)
        where TDecorator : class, IQueryHandler<TQuery, TQueryResult>
        where TQuery : class, IQuery => services.Decorate<IQueryHandler<TQuery, TQueryResult>, TDecorator>();
}
