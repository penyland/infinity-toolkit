using Azure.Core;
using Azure.Identity;

namespace Infinity.Toolkit.Azure.Identity;

public static class TokenCredentialHelper
{
    /// <summary>
    /// Helps creating a ChainedTokenCredential used to authenticate with Azure services.
    /// By default, the following credential types are included:
    /// EnvironmentCredential
    /// ManagedIdentityCredential
    ///
    /// To include additional credential types, set the corresponding environment variable to "true".
    /// INCLUDE_VISUAL_STUDIO_CREDENTIAL
    /// INCLUDE_VISUAL_STUDIO_CODE_CREDENTIAL
    /// INCLUDE_INTERACTIVE_BROWSER_CREDENTIAL
    /// INCLUDE_SHARED_TOKEN_CACHE_CREDENTIAL
    /// INCLUDE_AZURE_DEVELOPER_CLI_CREDENTIAL
    /// INCLUDE_AZURE_POWER_SHELL_CREDENTIAL
    /// INCLUDE_AZURE_CLI_CREDENTIAL
    /// INCLUDE_WORKLOAD_IDENTITY_CREDENTIAL
    /// 
    /// </summary>
    /// <returns>A ChainedTokenCredential with the specified credential types.</returns>
    public static TokenCredential GetTokenCredential()
    {
        TokenCredential[] tokenCredentials =[];

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

        if (Equals(Environment.GetEnvironmentVariable("INCLUDE_SHARED_TOKEN_CACHE_CREDENTIAL"), "true"))
        {
            tokenCredentials = [.. tokenCredentials, new SharedTokenCacheCredential()];
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

        tokenCredentials = [.. tokenCredentials, new EnvironmentCredential(), new ManagedIdentityCredential()];

        var tokenCredential = new ChainedTokenCredential(tokenCredentials);

        return tokenCredential;
    }
}
