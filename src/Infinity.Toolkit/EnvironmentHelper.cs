namespace Infinity.Toolkit;

public static partial class EnvironmentHelper
{
    public static bool IsRunningInContainer => Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true");

    public static bool IsRunningInAzureAppService => Equals(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"), "true");

    public static bool IsRunningInAzureContainerApps => IsRunningInContainer && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CONTAINER_APP_NAME"));
}
