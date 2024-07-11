namespace Infinity.Toolkit.Mediator;

public interface ICommandDispatcherStrategy
{
    ValueTask DispatchAsync<TCommand>(IEnumerable<ICommandHandler<TCommand>> handlers, TCommand command, CancellationToken cancellationToken = default)
        where TCommand : ICommand;
}
