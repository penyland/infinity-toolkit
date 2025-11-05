using System.Text.Json.Serialization;

namespace Infinity.Toolkit.Handlers;

public class HandlerContext<TContext> : IHandlerContext<TContext>
{
    /// <summary>
    /// Gets the runtime type of the context associated with this instance.
    /// </summary>
    public Type Type { get; } = typeof(TContext);

    ///<inheritdoc/>
    public BinaryData Body { get; init; }

    ///<inheritdoc/>
    public TContext Request { get; init; }

    /// <summary>
    /// Creates a new instance of <see cref="HandlerContext{TContext}"/> using the specified request object.
    /// </summary>
    /// <remarks>The returned context includes both the original request and a JSON-serialized body, which can
    /// be used for further processing or transmission.</remarks>
    /// <param name="request">The request object to be encapsulated in the handler context. Cannot be null.</param>
    /// <returns>A <see cref="HandlerContext{TContext}"/> containing the provided request and its serialized representation.</returns>
    public static HandlerContext<TContext> Create(TContext request)
    {
        return new HandlerContext<TContext>
        {
            Request = request,
            Body = BinaryData.FromObjectAsJson(request)
        };
    }
}

public interface IHandlerContext<TContext>
{
    /// <summary>
    /// Gets the request context associated with the current operation.
    /// </summary>
    TContext Request { get; }

    /// <summary>
    /// Gets the message payload as binary data.
    /// </summary>
    /// <remarks>Use this property to access the raw content of the message for serialization,
    /// deserialization, or transmission. The format and interpretation of the data depend on the context in which the
    /// message is used.</remarks>
    [JsonIgnore]
    BinaryData Body { get; }
}
