using Infinity.Toolkit.Experimental;
using Microsoft.Extensions.DependencyInjection;

namespace Infinity.Toolkit.Handlers;

public interface ICommand { }

public interface IQuery { }

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
        var context = new HandlerContext<TRequest>
        {
            Body = new BinaryData(request),
            Request = request
        };

        var handler = serviceProvider.GetService<IRequestHandler<TRequest>>();
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
        var handler = scope.ServiceProvider.GetService<IRequestHandler<TRequest, TResult>>();

        var context = new HandlerContext<TRequest>
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
