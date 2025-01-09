namespace Infinity.Toolkit.Messaging.Diagnostics;

public static class Errors
{
    public const string CouldNotDeserializeJsonToType = "Could not deserialize message to:";
    public const string RequiredApplicationPropertyIsMissing = "Required application property is missing:";
    public const string CouldNotCreateMethodForMessageType = "Could not create method for message type:";
    public const string ChannelTypeIsNotSupported = "Channel type is not supported:";
    public const string ChannelOptionsNotFound = "Channel options for channel was not found:";
    public const string EventTypeWasNotRegistered = "Event type was not registered:";
    public const string CloudEventsTypeNotFound = "Cloud events type not found.";
    public const string CouldNotConsumeDeferredMessage = "Could not consume deferred message.";

    public static class Reasons
    {
        public const string EventTypeWasNotRegistered = "MessageTypeWasNotRegistered";
        public const string MissingRequiredApplicationProperty = "MissingRequiredApplicationProperty";
        public const string MissingCloudEventsTypeProperty = "MissingCloudEventsTypeProperty";
    }
}
