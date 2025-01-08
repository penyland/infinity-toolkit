using Microsoft.Extensions.DependencyInjection;

namespace Infinity.Toolkit.Experimental.Mediator;

public interface ICommand { }

public interface IQuery { }

public interface IMediatorHandler<TIn>
    where TIn : class
{
    Task<Result> HandleAsync(MediatorHandlerContext<TIn> context, CancellationToken cancellationToken = default);
}

public interface IMediatorHandler<TIn, TResult>
    where TIn : class
{
    Task<Result<TResult>> HandleAsync(MediatorHandlerContext<TIn> context, CancellationToken cancellationToken = default);
}

public interface IMediator
{
    Task<Result> SendAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : class, ICommand;

    Task<Result<TResponse>> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : class, IQuery;
}

public class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private readonly IServiceProvider serviceProvider = serviceProvider;

    public Task<Result> SendAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : class, ICommand
    {
        var context = new MediatorHandlerContext<TRequest>
        {
            Body = new BinaryData(request),
            Request = request
        };

        var handler = serviceProvider.GetService<IMediatorHandler<TRequest>>();
        return handler switch
        {
            null => throw new InvalidOperationException($"No handler found for request of type {typeof(TRequest).Name}."),
            _ => handler.HandleAsync(context, cancellationToken)
        };
    }

    public Task<Result<TResult>> SendAsync<TRequest, TResult>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : class, IQuery
    {
        using var scope = serviceProvider.CreateScope();
        var handler = scope.ServiceProvider.GetService<IMediatorHandler<TRequest, TResult>>();

        var context = new MediatorHandlerContext<TRequest>
        {
            Body = new BinaryData(request),
            Request = request
        };

        return handler switch
        {
            null => throw new InvalidOperationException($"No handler found for request of type {typeof(TRequest).Name}."),
            _ => handler.HandleAsync(context, cancellationToken)
        };
    }
}
