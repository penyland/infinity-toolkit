namespace Infinity.Toolkit.Handlers;

public interface IRequestHandler<TResponse>
    where TResponse : class
{
    /// <summary>
    /// Represents a handler for a request that does not take any parameters and returns a result of type <typeparamref name="TResponse"/>.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>Returns the result of the request.</returns>
    Task<Result<TResponse>> HandleAsync(CancellationToken cancellationToken = default);
}

public interface IRequestHandler<TRequest, TResult>
    where TRequest : class
    where TResult : class
{
    /// <summary>
    /// Represents a handler for a request which takes a parameter of type <typeparamref name="TRequest"/> and returns a result of type <typeparamref name="TResult"/>.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>Returns the result of the request.</returns>
    Task<Result<TResult>> HandleAsync(IHandlerContext<TRequest> context, CancellationToken cancellationToken = default);
}
