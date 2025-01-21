using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Interfaces;
using Microsoft.OpenApi.Models;

namespace Infinity.Toolkit.OpenApi;

public class OAuth2Settings
{
    internal const string DefaultConfigSectionName = "AzureAd";

    public string Instance { get; set; } = default!;

    public string ClientId { get; set; }

    public string TenantId { get; set; } = default!;

    public string AppIdentifier { get; set; } = default!;

    public string Scopes { get; set; } = default!;

    public Uri AuthorizationUrl => new($"{Instance.TrimEnd('/')}/{TenantId}/oauth2/v2.0/authorize", UriKind.Absolute);

    public Uri TokenUrl => new($"{Instance}/{TenantId}/oauth2/v2.0/token", UriKind.Absolute);
}

public static class AzureAdOAuth2SecuritySchemeDefinitionDocumentTransformerExtensions
{
    public static void AddOAuth2OpenApiSecuritySchemeDefinition(this IServiceCollection services, Action<OAuth2Settings>? configureSettings = null, string configSectionPath = OAuth2Settings.DefaultConfigSectionName)
    {
        services.AddOptions<OAuth2Settings>()
            .BindConfiguration(configSectionPath)
            .Configure(configureSettings ?? (_ => { }))
            .PostConfigure(settings =>
            {
                if (settings.Scopes == null || settings.Scopes.Length == 0)
                {
                    throw new InvalidOperationException("Scopes are required.");
                }
            });

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<OAuth2SecuritySchemeDefinitionDocumentTransformer>();
        });
    }
}

public sealed class OAuth2SecuritySchemeDefinitionDocumentTransformer(IOptions<OAuth2Settings> options) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        const string AuthenticationScheme = "oauth2";

        var securityScheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Name = AuthenticationScheme,
            Scheme = AuthenticationScheme,
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = AuthenticationScheme },
            In = ParameterLocation.Header,
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = options.Value.AuthorizationUrl,
                    TokenUrl = options.Value.TokenUrl,
                    Scopes = options.Value.Scopes.Split(" ").ToDictionary(x => x, x => x),
                    Extensions = new Dictionary<string, IOpenApiExtension>
                    {
                        ["x-usePkce"] = new OpenApiString("SHA-256")
                    }
                }
            }
        };

        document.Components ??= new();
        document.Components.SecuritySchemes.Add(AuthenticationScheme, securityScheme);
        return Task.CompletedTask;
    }
}
