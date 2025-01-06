using Infinity.Toolkit.Experimental;
using Microsoft.Extensions.DependencyInjection;

namespace Infinity.Toolkit.Experimental.Mediator;

public class CommandMediator(IServiceProvider serviceProvider) : ICommandMediator
{
    public Task<Result> SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class, ICommand
    {
        var commandHandlerContext = new MediatorCommandHandlerContext<TCommand>
        {
            Body = new BinaryData(command),
            Command = command
        };

        var handler = serviceProvider.GetService<ICommandHandler<TCommand>>();
        return handler switch
        {
            null => throw new InvalidOperationException($"No handler found for command of type {typeof(TCommand).Name}."),
            _ => handler.HandleAsync(commandHandlerContext, cancellationToken)
        };
    }

    public Task<Result<TCommandResult>> SendAsync<TCommand, TCommandResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class, ICommand
    {
        using var scope = serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetService<ICommandHandler<TCommand, TCommandResult>>();

        var context = new MediatorCommandHandlerContext<TCommand>
        {
            Body = new BinaryData(command),
            Command = command
        };

        return handler switch
        {
            null => throw new InvalidOperationException($"No handler found for query of type {typeof(TCommand).Name}."),
            _ => handler.HandleAsync(context, cancellationToken)
        };
    }
}
