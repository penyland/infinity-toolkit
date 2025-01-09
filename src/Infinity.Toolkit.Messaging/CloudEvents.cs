namespace Infinity.Toolkit.Messaging;

public static class CloudEvents
{
    public const string CloudEventsSpecVersion = "1.0.2";
    public const string DataContentType = "cloudEvents_datacontenttype";
    public const string Id = "cloudEvents_id";
    public const string Source = "cloudEvents_source";
    public const string SpecVersion = "cloudEvents_specversion";
    public const string Type = "cloudEvents_type";

    public const string MediaType = "application/cloudevents+json";

    public const string CloudEventsType = "{0}.{1}";
    public const string CloudEventsSource = "/{0}/{1}/{2}/";
}
