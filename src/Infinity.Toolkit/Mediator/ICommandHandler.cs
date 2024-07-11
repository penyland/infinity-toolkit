using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Infinity.Toolkit.Mediator;

public interface ICommandHandler<TCommand>
{
    ValueTask HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public interface ICommand { }

public record Result(bool Success, string Message);

public interface ICommandDispatcher
{
    ValueTask DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand;
}

public interface ICommandDispatcherStrategy
{
    ValueTask DispatchAsync<TCommand>(IEnumerable<ICommandHandler<TCommand>> handlers, TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand;
}

public class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider serviceProvider;
    private readonly ICommandDispatcherStrategy dispatcherStrategy;

    public CommandDispatcher(IServiceProvider serviceProvider, ICommandDispatcherStrategy dispatcherStrategy)
    {
        this.serviceProvider = serviceProvider;
        this.dispatcherStrategy = dispatcherStrategy;
    }

    public ValueTask DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default) where TCommand : ICommand
    {
        using var scope = serviceProvider.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<ICommandHandler<TCommand>>();
        return dispatcherStrategy.DispatchAsync(handlers, command, cancellationToken);
    }
}

public class ForeachAwaitCommandDispatcherStrategy : ICommandDispatcherStrategy
{
    public async ValueTask DispatchAsync<TCommand>(IEnumerable<ICommandHandler<TCommand>> handlers, TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        foreach (var handler in handlers)
        {
            await handler.HandleAsync(command, cancellationToken);
        }
    }
}

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
}
