using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks.Dataflow;

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

