using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infinity.Toolkit.Mediator;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMediator(this IServiceCollection services)
    {
        services.TryAddScoped<ICommandDispatcher, CommandDispatcher>();
        services.TryAddScoped<ICommandDispatcherStrategy, ForeachAwaitCommandDispatcherStrategy>();
        return services;
    }

    public static IServiceCollection AddCommandHandler<TCommand, TCommandHandler>(this IServiceCollection services)
        where TCommand : ICommand
        where TCommandHandler : class, ICommandHandler<TCommand>
    {
        services.AddTransient<ICommandHandler<TCommand>, TCommandHandler>();
        return services;
    }

    public static IServiceCollection AddPipeline<TIn, TOut>(this IServiceCollection services, IPipeline<TIn, TOut> pipeline)
    {
        return services.AddTransient<IPipeline<TIn, TOut>>(p => pipeline);
    }

    public static IServiceCollection AddPipeline<TIn, TOut>(this IServiceCollection services, Func<IServiceProvider, IPipeline<TIn, TOut>> pipelineFactory)
    {
        return services.AddTransient<IPipeline<TIn, TOut>>(pipelineFactory);
    }
}
