namespace Infinity.Toolkit.Mediator;

public interface ICommand { }

public interface IQuery { }

public interface IMediatorHandler<TIn>
{
}

public interface IMediatorHandler<TIn, TResult>
    where TIn : class
{
}

public interface ICommandHandler<TCommand> : IMediatorHandler<TCommand>
    where TCommand : class, ICommand
{
    Task<Result> HandleAsync(MediatorCommandHandlerContext<TCommand> context, CancellationToken cancellationToken = default);
}

public interface ICommandHandler<TCommand, TCommandResult> : IMediatorHandler<TCommand, TCommandResult>
    where TCommand : class, ICommand
{
    Task<Result<TCommandResult>> HandleAsync(MediatorCommandHandlerContext<TCommand> context, CancellationToken cancellationToken = default);
}

public interface IQueryHandler<TQuery, TResult> : IMediatorHandler<TQuery, TResult>
    where TQuery : class, IQuery
{
    Task<Result<TResult>> HandleAsync(MediatorQueryHandlerContext<TQuery> context, CancellationToken cancellationToken = default);
}

public interface IMediator
{
    Task<Result> SendAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : class, ICommand;

    Task<Result<TResponse>> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : class, IQuery;
}

public interface ICommandMediator
{
    Task<Result> SendAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class, ICommand;

    Task<Result<TCommandResult>> SendAsync<TCommand, TCommandResult>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class, ICommand;
}

public interface IQueryMediator
{
    Task<Result<TResult>> SendAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : class, IQuery;
}

public class Mediator : IMediator
{
    private readonly IServiceProvider serviceProvider;

    public Mediator(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

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

    public Task<Result<TResult>> SendAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : class, IQuery
    {
        using var scope = serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetService<IQueryHandler<TQuery, TResult>>();

        var queryHandlerContext = new MediatorQueryHandlerContext<TQuery>
        {
            Body = new BinaryData(query),
            Query = query
        };

        return handler switch
        {
            null => throw new InvalidOperationException($"No handler found for query of type {typeof(TQuery).Name}."),
            _ => handler.HandleAsync(queryHandlerContext, cancellationToken)
        };
    }
}
