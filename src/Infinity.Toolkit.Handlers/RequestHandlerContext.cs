namespace Infinity.Toolkit.Handlers;

public class RequestHandlerContext<TContext> : IRequestHandlerContext<TContext>
    where TContext : class
{
    public BinaryData Body { get; init; }

    public TContext Request { get; init; }
}

public interface IRequestHandlerContext<TContext> : IHandlerContext
    where TContext : class
{
    public static Type Type { get; } = typeof(TContext);

    TContext Request { get; }
}
