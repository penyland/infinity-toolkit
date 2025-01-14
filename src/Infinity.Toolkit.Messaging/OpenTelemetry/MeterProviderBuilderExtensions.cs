using Infinity.Toolkit.Messaging.Diagnostics;
using OpenTelemetry.Metrics;

namespace Infinity.Toolkit.Messaging.OpenTelemetry;

public static class MeterProviderBuilderExtensions
{
    public static MeterProviderBuilder AddMessagingInstrumentation(this MeterProviderBuilder builder) =>
        builder.AddMeter(DiagnosticProperty.ActivitySourceName)
            .AddView(DiagnosticProperty.Metrics.MessagingProcessDuration, new ExplicitBucketHistogramConfiguration
            {
                Boundaries = [0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10]
            })
            .AddView(DiagnosticProperty.Metrics.MessageHandlerElapsedTime, new ExplicitBucketHistogramConfiguration
            {
                Boundaries = [0, 0.005, 0.01, 0.025, 0.05, 0.075, 0.1, 0.25, 0.5, 0.75, 1, 2.5, 5, 7.5, 10]
            });
}
