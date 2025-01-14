namespace Infinity.Toolkit.Messaging.Diagnostics;

internal class MessageBusActivityFactory
{
    public MessageBusActivityFactory()
    {
    }

    public static MessageBusActivitySource Create()
    {
        return new MessageBusActivitySource();
    }
}

internal class MessageBusActivitySource
{
    internal static readonly ActivitySource ActivitySource = new(DiagnosticProperty.ActivitySourceName, Assembly.GetExecutingAssembly().GetName().Version?.ToString());

    public static Activity? StartActivity(string operationName, ActivityKind kind, ActivityContext activityContext, TagList? tags = default, IEnumerable<ActivityLink>? links = default)
    {
        if (!ActivitySource.HasListeners())
        {
            return null;
        }

        return ActivitySource.StartActivity(
            operationName,
            kind,
            activityContext,
            tags,
            links);
    }
}
