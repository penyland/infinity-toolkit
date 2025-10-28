using Azure.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Data.Common;
using System.Text.Json.Serialization;

namespace Infinity.Toolkit.Azure;

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
    internal const string DefaultConfigSectionName = "AzureAppConfiguration";

    public string ApplicationName { get; set; }

    public Uri Endpoint { get; set; }

    public string GlobalKeyFilter { get; set; } = "Global";

    public bool UseFeatureFlags { get; set; } = true;

    public bool UseKeyVault { get; set; } = false;

    [JsonIgnore]
    public TokenCredential? TokenCredential { get; set; }

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
    public static IHostApplicationBuilder ConfigureAzureAppConfiguration(this IHostApplicationBuilder builder)
        => builder.ConfigureAzureAppConfiguration(AzureAppConfigSettings.DefaultConfigSectionName, null, null);

    public static IHostApplicationBuilder ConfigureAzureAppConfiguration(this IHostApplicationBuilder app, Action<AzureAppConfigSettings>? configure = null, Action<AzureAppConfigurationRefreshOptions>? refreshOptions = null)
        => app.ConfigureAzureAppConfiguration(AzureAppConfigSettings.DefaultConfigSectionName, configure, refreshOptions);

    public static IHostApplicationBuilder ConfigureAzureAppConfiguration(this IHostApplicationBuilder builder, string configSectionName = AzureAppConfigSettings.DefaultConfigSectionName, Action<AzureAppConfigSettings>? configureSettings = null, Action<AzureAppConfigurationRefreshOptions>? refreshOptions = null)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));
        var loggerFactory = LoggerFactory.Create(loggingBuilder =>
        {
            loggingBuilder
                .AddConfiguration(builder.Configuration.GetSection("Logging"))
#if DEBUG
                .AddDebug()
#endif
                .AddConsole();
        });
        var logger = loggerFactory.CreateLogger("Infinity.Toolkit.Azure");

        var settings = new AzureAppConfigSettings()
        {
            ApplicationName = builder.Environment.ApplicationName,
            TokenCredential = Identity.TokenCredentialHelper.GetTokenCredential()
        };

        builder.Configuration.GetSection(configSectionName).Bind(settings);
        configureSettings?.Invoke(settings);

        // Make sure we have a connection string or endpoint before adding Azure App Configuration
        if (builder.Configuration.GetConnectionString("AzureAppConfig") is null && settings.Endpoint is null && Environment.GetEnvironmentVariable("AZURE_APP_CONFIG_ENDPOINT") is null)
        {
            logger.LogWarning(message: $"""
                Azure App Configuration is not configured.
                Please provide a valid connection string or endpoint using one of the following sources:
                - 'ConnectionStrings:AzureAppConfig' (connection string)
                - '{configSectionName}:Endpoint' configuration section
                - 'AZURE_APP_CONFIG_ENDPOINT' environment variable
            """);
            return builder;
        }

        builder.Configuration.AddAzureAppConfiguration(options =>
        {
            if (builder.Configuration.GetConnectionString("AzureAppConfig") is string connectionString)
            {
                settings.ParseConnectionString(connectionString);
                options.Connect(connectionString);
            }
            else
            {
                // Try to use configured endpoint
                if (settings.Endpoint != null && Uri.TryCreate(settings.Endpoint.ToString(), UriKind.Absolute, out var endpointUri))
                {
                    // Endpoint from settings is valid
                }
                else
                {
                    // Try to get endpoint from environment variable
                    var envEndpoint = Environment.GetEnvironmentVariable("AZURE_APP_CONFIG_ENDPOINT");
                    if (!Uri.TryCreate(envEndpoint, UriKind.Absolute, out endpointUri))
                    {
                        logger?.LogWarning("Found invalid Azure App Configuration endpoint in environment variable 'AZURE_APP_CONFIG_ENDPOINT': {EnvEndpoint}", envEndpoint);
                    }
                }

                options.Connect(endpointUri, settings.TokenCredential);
            }

            if (!string.IsNullOrEmpty(settings.GlobalKeyFilter))
            {
                // Filter by global key filter
                options
                    .Select($"{settings.GlobalKeyFilter}*", LabelFilter.Null)
                    .Select($"{settings.GlobalKeyFilter}*", builder.Environment.EnvironmentName)
                    .TrimKeyPrefix($"{settings.GlobalKeyFilter}:");
            }

            if (!string.IsNullOrEmpty(settings.ApplicationName))
            {
                // Filter by application name
                options
                    .Select($"{settings.ApplicationName}*", LabelFilter.Null)
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

            if (refreshOptions != null)
            {
                options.ConfigureRefresh(refreshOptions);
            }
        });

        return builder;
    }
}
