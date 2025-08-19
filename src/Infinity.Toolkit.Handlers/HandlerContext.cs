using System.Text.Json.Serialization;

namespace Infinity.Toolkit.Handlers;

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
