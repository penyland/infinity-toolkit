namespace Infinity.Toolkit;

public static partial class EnvironmentHelper
{
    public static bool IsRunningInContainer => Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true");

    public static bool IsRunningInKubernetes => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST"));

    public static bool IsRunningInDockerDesktop => IsRunningInContainer && Equals(Environment.GetEnvironmentVariable("DOCKER_DESKTOP_ENVIRONMENT"), "true");
}
