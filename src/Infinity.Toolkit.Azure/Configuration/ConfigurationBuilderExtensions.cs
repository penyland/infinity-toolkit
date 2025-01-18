using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Infinity.Toolkit.Azure.Configuration;

// Config section specifications
// Infinity: {
//   Azure: {
//     AppConfig: {
//       Endpoint: "https://<app-config-name>.azconfig.io",
//       GlobalKeyFilter: "Global",
//       UseFeatureFlags: true,
//     }
//   }
// }

public class AzureAppConfigSettings
{
    public string Endpoint { get; init; } = string.Empty;
    public string GlobalKeyFilter { get; init; } = "Global";
    public bool UseFeatureFlags { get; init; } = true;
}

public static class ConfigurationBuilderExtensions
{
    private const string ConfigSectionPath = "Infinity:Azure:AppConfig";

    public static IHostApplicationBuilder AddAzureAppConfiguration(this IHostApplicationBuilder builder, TokenCredential? tokenCredential = default)
    {
        var appConfigEndpointUri = Environment.GetEnvironmentVariable("AZURE_APP_CONFIG_ENDPOINT") ?? builder.Configuration.GetValue<string>($"{ConfigSectionPath}:Endpoint") ?? throw new ArgumentException("Azure App Configuration endpoint is not configured.");
        var globalKeyFilter = builder.Configuration.GetValue<string>($"{ConfigSectionPath}:GlobalKeyFilter", "Global");
        var useFeatureFlags = builder.Configuration.GetValue<bool>($"{ConfigSectionPath}:UseFeatureFlags", true);

        var credentials = tokenCredential ?? new DefaultAzureCredential();

        builder.Configuration.AddAzureAppConfiguration(options =>
        {
            options.Connect(new Uri(appConfigEndpointUri), credentials);

            if (!string.IsNullOrEmpty(globalKeyFilter))
            {
                // Filter by global key filter
                options
                    .Select("${globalKeyFilter}*")
                    .Select("${globalKeyFilter}*", builder.Environment.EnvironmentName);
            }

            if (!string.IsNullOrEmpty(builder.Environment.ApplicationName))
            {
                // Filter by application name
                options
                    .Select($"{builder.Environment.ApplicationName}*")
                    .Select($"{builder.Environment.ApplicationName}*", builder.Environment.EnvironmentName);
            }

            // Trim key prefixes
            if (!string.IsNullOrEmpty(globalKeyFilter))
            {
                options.TrimKeyPrefix($"{globalKeyFilter}:");
            }

            options.TrimKeyPrefix(builder.Environment.ApplicationName + ":");

            if (useFeatureFlags)
            {
                options.UseFeatureFlags(featureFlagOptions =>
                {
                    featureFlagOptions
                        .Select($"{builder.Environment.ApplicationName}*")
                        .Select($"{builder.Environment.ApplicationName}*", builder.Environment.EnvironmentName);
                });
            }

            options.ConfigureKeyVault(kv =>
            {
                kv.SetCredential(credentials);
            });
        });

        return builder;
    }
}
