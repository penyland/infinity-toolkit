using Infinity.Toolkit.Messaging.Diagnostics;
using OpenTelemetry.Trace;

namespace Infinity.Toolkit.Messaging.OpenTelemetry;

public static class TracerProviderBuilderExtensions
{
    public static TracerProviderBuilder AddMessagingInstrumentation(this TracerProviderBuilder builder) => builder.AddSource(DiagnosticProperty.ActivitySourceName);
}
