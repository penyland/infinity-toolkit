using System.Text.Json.Serialization;

namespace Infinity.Toolkit.Mediator;

public class MediatorCommandHandlerContext<TCommand> : MediatorHandlerContextBase<TCommand>
    where TCommand : class
{
    public TCommand Command { get; init; }
}

public class MediatorQueryHandlerContext<TQuery> : MediatorHandlerContextBase<TQuery>
    where TQuery : class
{
    public TQuery Query { get; init; }
}

public abstract class MediatorHandlerContextBase<TContext> : IMediatorHandlerContext<TContext>
{
    public Type Type { get; } = typeof(TContext);

    public BinaryData Body { get; init; }

    public TContext Request { get; }
}

public interface IMediatorHandlerContext<TContext>
{
    TContext Request { get; }

    /// <summary>
    /// The message body.
    /// </summary>
    [JsonIgnore]
    BinaryData Body { get; }
}
