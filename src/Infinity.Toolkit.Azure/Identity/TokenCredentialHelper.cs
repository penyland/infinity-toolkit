using Azure.Core;
using Azure.Identity;

namespace Infinity.Toolkit.Azure.Identity;

public static class TokenCredentialHelper
{
    /// <summary>
    /// Helps creating a ChainedTokenCredential used to authenticate with Azure services.
    /// By default if the application is running in Azure (App Service, Functions, Container Apps), the following credential types are included:
    /// EnvironmentCredential
    /// ManagedIdentityCredential
    ///
    /// If the application is not running in Azure, only the EnvironmentCredential is included by default.
    ///
    /// To include additional credential types, set the value of the corresponding environment variable to "true".
    /// INCLUDE_VISUAL_STUDIO_CREDENTIAL
    /// INCLUDE_VISUAL_STUDIO_CODE_CREDENTIAL
    /// INCLUDE_INTERACTIVE_BROWSER_CREDENTIAL
    /// INCLUDE_AZURE_DEVELOPER_CLI_CREDENTIAL
    /// INCLUDE_AZURE_POWER_SHELL_CREDENTIAL
    /// INCLUDE_AZURE_CLI_CREDENTIAL
    /// INCLUDE_WORKLOAD_IDENTITY_CREDENTIAL
    /// </summary>
    /// <param name="clientId">The client ID of the user-assigned managed identity. If null, the AZURE_CLIENT_ID environment variable will be used.</param>
    /// <returns>A ChainedTokenCredential with the specified credential types or DefaultAzureCredentials if no credentials are specified.</returns>
    public static TokenCredential GetTokenCredential(string? clientId = null)
    {
        TokenCredential[] tokenCredentials =[];

        // First check if the app is running in Azure
        if (EnvironmentHelper.IsRunningInAzure)
        {
            tokenCredentials = [
                new EnvironmentCredential(),
                new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(clientId ?? Environment.GetEnvironmentVariable("AZURE_CLIENT_ID") ?? string.Empty))];

            var tokenCredential = new ChainedTokenCredential(tokenCredentials);
            return tokenCredential;
        }
        else
        {
            if (Equals(Environment.GetEnvironmentVariable("INCLUDE_AZURE_CLI_CREDENTIAL"), "true"))
            {
                tokenCredentials = [.. tokenCredentials, new AzureCliCredential()];
            }

            if (Equals(Environment.GetEnvironmentVariable("INCLUDE_WORKLOAD_IDENTITY_CREDENTIAL"), "true"))
            {
                tokenCredentials = [.. tokenCredentials, new WorkloadIdentityCredential()];
            }

            if (Equals(Environment.GetEnvironmentVariable("INCLUDE_AZURE_DEVELOPER_CLI_CREDENTIAL"), "true"))
            {
                tokenCredentials = [.. tokenCredentials, new AzureDeveloperCliCredential()];
            }

            if (Equals(Environment.GetEnvironmentVariable("INCLUDE_VISUAL_STUDIO_CREDENTIAL"), "true"))
            {
                tokenCredentials = [.. tokenCredentials, new VisualStudioCredential()];
            }

            if (Equals(Environment.GetEnvironmentVariable("INCLUDE_VISUAL_STUDIO_CODE_CREDENTIAL"), "true"))
            {
                tokenCredentials = [.. tokenCredentials, new VisualStudioCodeCredential()];
            }

            if (Equals(Environment.GetEnvironmentVariable("INCLUDE_AZURE_POWER_SHELL_CREDENTIAL"), "true"))
            {
                tokenCredentials = [.. tokenCredentials, new AzurePowerShellCredential()];
            }

            if (Equals(Environment.GetEnvironmentVariable("INCLUDE_INTERACTIVE_BROWSER_CREDENTIAL"), "true"))
            {
                tokenCredentials = [.. tokenCredentials, new InteractiveBrowserCredential()];
            }

            return tokenCredentials.Length switch
            {
                0 => new DefaultAzureCredential(),
                _ => new ChainedTokenCredential(tokenCredentials)
            };
        }
    }
}
