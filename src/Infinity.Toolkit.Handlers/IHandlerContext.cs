using System.Text.Json.Serialization;

namespace Infinity.Toolkit.Handlers;

public interface IRequestHandlerContext
{
    /// <summary>
    /// The message body.
    /// </summary>
    [JsonIgnore]
    BinaryData Payload { get; }
}
