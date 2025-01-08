using System.Text.Json.Serialization;

namespace Infinity.Toolkit.Experimental.Mediator;

public class MediatorHandlerContext<TContext> : IMediatorHandlerContext<TContext>
{
    public Type Type { get; } = typeof(TContext);

    public BinaryData Body { get; init; }

    public TContext Request { get; init; }
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
