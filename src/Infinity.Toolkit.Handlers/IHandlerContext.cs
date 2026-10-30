using System.Text.Json.Serialization;

namespace Infinity.Toolkit.Handlers;

public interface IHandlerContext
{
    /// <summary>
    /// The message body.
    /// </summary>
    [JsonIgnore]
    BinaryData Body { get; }
}
