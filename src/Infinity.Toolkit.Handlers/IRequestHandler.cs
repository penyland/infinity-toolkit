using Infinity.Toolkit.Experimental;

namespace Infinity.Toolkit.Handlers;

public interface IRequestHandler<TIn>
    where TIn : class
{
    /// <summary>
    /// Represents a handler for a request that does not return a result except for success or failure.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>Returns the result of the request.</returns>
    Task<Result> HandleAsync(IHandlerContext<TIn> context, CancellationToken cancellationToken = default);
}

public interface IRequestHandler<TIn, TResult>
    where TIn : class
{
    /// <summary>
    /// Represents a handler for a request which returns a result.
    /// </summary>
    /// <param name="context">The request context.</param>
    /// <param name="cancellationToken">An optional cancellation token.</param>
    /// <returns>Returns the result of the request.</returns>
    Task<Result<TResult>> HandleAsync(IHandlerContext<TIn> context, CancellationToken cancellationToken = default);
}
