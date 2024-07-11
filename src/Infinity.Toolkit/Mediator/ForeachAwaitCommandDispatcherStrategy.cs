namespace Infinity.Toolkit.Mediator;

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
