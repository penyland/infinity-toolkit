using Microsoft.Extensions.DependencyInjection;

namespace Infinity.Toolkit.Experimental.Mediator;

public interface ICommand { }

public interface IQuery { }

public interface IMediatorHandler<TIn>
    where TIn : class
{
    /// <summary>
    /// Represents a handler for a request that does not return a result except for success or failure.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>Returns the result of the request.</returns>
    Task<Result> HandleAsync(MediatorHandlerContext<TIn> context, CancellationToken cancellationToken = default);
}

public interface IMediatorHandler<TIn, TResult>
    where TIn : class
{
    /// <summary>
    /// Represents a handler for a request which returns a result.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>Returns the result of the request.</returns>
    Task<Result<TResult>> HandleAsync(MediatorHandlerContext<TIn> context, CancellationToken cancellationToken = default);
}

public interface IMediator
{
    /// <summary>
    /// Sends a request to the mediator, the request is typically a command which does not return a result.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <param name="request">The content of the request.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>A success or failure result.</returns>
    Task<Result> SendAsync<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : class, ICommand;

    /// <summary>
    /// Sends a request to the mediator, the request is typically a query which returns a result.
    /// </summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <param name="request">The content of the request.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>The result of the request.</returns>
    Task<Result<TResponse>> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : class, IQuery;
}

internal sealed class Mediator(IServiceProvider serviceProvider) : IMediator
{
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
