using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;

namespace Infinity.Toolkit.OpenApi;

public class OpenApiOAuth2Settings
{
    public const string AuthenticationScheme = "oauth2";

    internal const string DefaultConfigSectionName = "AzureAd";

    public string Instance { get; set; } = default!;

    public string ClientId { get; set; }

    public string TenantId { get; set; } = default!;

    public string AppIdentifier { get; set; } = default!;

    public string Scopes { get; set; } = default!;

    public string[]? ScopesArray => Scopes.Split(" ", StringSplitOptions.RemoveEmptyEntries);

    public IDictionary<string, string> ScopesDictionary => ScopesArray?.ToDictionary(x => x, x => x) ?? [];

    public string AuthorityUrl => $"{Instance}{TenantId}";

    public string AuthorizationUrl => $"{AuthorityUrl}/oauth2/v2.0/authorize";

    public string TokenUrl => $"{AuthorityUrl}/oauth2/v2.0/token";
}

public static class AzureAdOAuth2SecuritySchemeDefinitionDocumentTransformerExtensions
{
    public static void AddOAuth2OpenApiSecuritySchemeDefinition(this IServiceCollection services, Action<OpenApiOAuth2Settings>? configureSettings = null, string configSectionPath = OpenApiOAuth2Settings.DefaultConfigSectionName)
    {
        services.AddOptions<OpenApiOAuth2Settings>()
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

public sealed class OAuth2SecuritySchemeDefinitionDocumentTransformer(IOptions<OpenApiOAuth2Settings> options) : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var oauthOptions = options.Value;

        var securityScheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Name = "oauth2",
            Scheme = "oauth2",
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri(oauthOptions.AuthorizationUrl),
                    TokenUrl = new Uri(oauthOptions.TokenUrl),
                    Scopes = oauthOptions.ScopesDictionary,
                    Extensions = new Dictionary<string, IOpenApiExtension>
                    {
                        ["x-usePkce"] = new JsonNodeExtension("SHA-256")
                    }
                }
            }
        };

        document.Components ??= new();
        document.AddComponent("oauth2", securityScheme);

        return Task.CompletedTask;
    }
}
