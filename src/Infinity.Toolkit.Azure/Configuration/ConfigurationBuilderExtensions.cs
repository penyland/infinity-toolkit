using Azure.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Data.Common;
using System.Text.Json.Serialization;

namespace Infinity.Toolkit.Azure.Configuration;

// Config section specifications
//"Infinity": {
//  "Azure": {
//    "AppConfig": {
//      "ApplicationName": "AzureSample",
//      "Endpoint": "https://<app-config-name>.azconfig.io",
//      "GlobalKeyFilter": "Global",
//      "UseFeatureFlags": true,
//      "UseKeyVault": true
//    }
//  }
//}

public class AzureAppConfigSettings
{
    internal const string DefaultConfigSectionName = "Infinity:Azure:AppConfig";

    public string ApplicationName { get; set; }

    public Uri Endpoint { get; set; }

    public string GlobalKeyFilter { get; set; } = "Global";

    public bool UseFeatureFlags { get; set; } = true;

    public bool UseKeyVault { get; set; } = true;

    [JsonIgnore]
    public TokenCredential? TokenCredential { get; set; } = Identity.TokenCredentialHelper.GetTokenCredential();

    internal void ParseConnectionString(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"""
                    ConnectionString is missing.
                    It should be provided in 'ConnectionStrings:<connectionName>'
                    or '{DefaultConfigSectionName}:Endpoint' key.'
                    configuration section.
                    """);
        }

        if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
        {
            Endpoint = uri;
        }
        else
        {
            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            if (builder.TryGetValue("Endpoint", out var endpoint) is false)
            {
                throw new InvalidOperationException($"""
                        The 'ConnectionStrings:<connectionName>' (or 'Endpoint' key in
                        '{DefaultConfigSectionName}') is missing.
                        """);
            }

            if (Uri.TryCreate(endpoint.ToString(), UriKind.Absolute, out uri) is false)
            {
                throw new InvalidOperationException($"""
                        The 'ConnectionStrings:<connectionName>' (or 'Endpoint' key in
                        '{DefaultConfigSectionName}') isn't a valid URI.
                        """);
            }

            Endpoint = uri;
        }
    }
}

public static class ConfigurationBuilderExtensions
{
    private const string DefaultConfigSectionName = "Infinity:Azure:AppConfig";

    public static IHostApplicationBuilder ConfigureAzureAppConfiguration(this IHostApplicationBuilder builder)
        => builder.ConfigureAzureAppConfiguration(DefaultConfigSectionName, null, null);

    public static IHostApplicationBuilder ConfigureAzureAppConfiguration(this IHostApplicationBuilder app, Action<AzureAppConfigSettings>? configure = null, Action<AzureAppConfigurationRefreshOptions>? refreshOptions = null)
        => app.ConfigureAzureAppConfiguration(DefaultConfigSectionName, configure, refreshOptions);

    public static IHostApplicationBuilder ConfigureAzureAppConfiguration(this IHostApplicationBuilder builder, string configSectionName = DefaultConfigSectionName, Action<AzureAppConfigSettings>? configureSettings = null, Action<AzureAppConfigurationRefreshOptions>? refreshOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        var settings = new AzureAppConfigSettings()
        {
            ApplicationName = builder.Environment.ApplicationName
        };

        builder.Configuration.GetSection(configSectionName).Bind(settings);
        configureSettings?.Invoke(settings);

        builder.Configuration.AddAzureAppConfiguration(options =>
        {
            if (builder.Configuration.GetConnectionString("AzureAppConfig") is string connectionString)
            {
                settings.ParseConnectionString(connectionString);
                options.Connect(builder.Configuration.GetConnectionString("AzureAppConfig"));
            }
            else
            {
                if (Uri.TryCreate(settings.Endpoint.ToString(), UriKind.Absolute, out var endpointUri))
                {
                    options.Connect(endpointUri, settings.TokenCredential);
                }
                else
                {
                    throw new InvalidOperationException($"""
                        The 'Endpoint' key in
                        '{configSectionName}') isn't a valid URI.
                        """);
                }
            }

            if (!string.IsNullOrEmpty(settings.GlobalKeyFilter))
            {
                // Filter by global key filter
                options
                    .Select($"{settings.GlobalKeyFilter}*")
                    .Select($"{settings.GlobalKeyFilter}*", builder.Environment.EnvironmentName)
                    .TrimKeyPrefix($"{settings.GlobalKeyFilter}:");
            }

            if (!string.IsNullOrEmpty(settings.ApplicationName))
            {
                // Filter by application name
                options
                    .Select($"{settings.ApplicationName}*")
                    .Select($"{settings.ApplicationName}*", builder.Environment.EnvironmentName)
                    .TrimKeyPrefix(settings.ApplicationName + ":");
            }

            if (settings.UseFeatureFlags)
            {
                options.UseFeatureFlags(featureFlagOptions =>
                {
                    // Expose refresh interval and label to settings
                    featureFlagOptions
                        .Select($"{settings.ApplicationName}*")
                        .Select($"{settings.ApplicationName}*", builder.Environment.EnvironmentName);
                });
            }

            if (settings.UseKeyVault)
            {
                options.ConfigureKeyVault(kvOptions =>
                {
                    kvOptions.SetCredential(settings.TokenCredential);
                });
            }

            options.ConfigureRefresh(refreshOptions);
        });

        return builder;
    }
}
