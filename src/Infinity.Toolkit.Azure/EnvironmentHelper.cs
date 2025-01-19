namespace Infinity.Toolkit.Azure;
public static class EnvironmentHelper
{
    public static bool RunningInContainer => Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true");

    public static bool RunningInAzureContainerApps => RunningInContainer && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CONTAINER_APP_NAME"));
}
