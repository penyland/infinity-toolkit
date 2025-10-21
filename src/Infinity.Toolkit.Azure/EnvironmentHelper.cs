namespace Infinity.Toolkit.Azure;

internal static class EnvironmentHelper
{
    public static bool IsRunningInContainer => Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true");

    public static bool IsRunningInAzureAppService => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));

    public static bool IsRunningInAzureContainerApps => IsRunningInContainer && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CONTAINER_APP_NAME"));

    public static bool IsRunningInAzureFunctions => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FUNCTIONS_WORKER_RUNTIME"));

    public static bool IsRunningInAzure => IsRunningInAzureAppService || IsRunningInAzureContainerApps || IsRunningInAzureFunctions;
}
