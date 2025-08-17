using Infinity.Toolkit.Experimental;
using System.Text.Json.Serialization;

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

public class HandlerContext<TContext> : IHandlerContext<TContext>
{
    public Type Type { get; } = typeof(TContext);

    public BinaryData Body { get; init; }

    public TContext Request { get; init; }
}

public interface IHandlerContext<TContext>
{
    TContext Request { get; }

    /// <summary>
    /// The message body.
    /// </summary>
    [JsonIgnore]
    BinaryData Body { get; }
}
