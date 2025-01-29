using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Microsoft.Extensions.DependencyInjection;

public static class AuthenticationBuilderExtensions
{
    /// <summary>
    /// Adds a policy scheme that can dynamically select the authentication scheme based on the token issuer.
    /// </summary>
    /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
    /// <param name="configure">A delegate that allows configuring <see cref="MultipleBearerPolicySchemeOptions"/>.</param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static AuthenticationBuilder AddMultipleBearerPolicySchemes(this AuthenticationBuilder builder, Action<MultipleBearerPolicySchemeOptions>? configure = null)
    {
        var options = new MultipleBearerPolicySchemeOptions();
        configure?.Invoke(options);

        return builder.AddPolicyScheme(MultipleBearerPolicySchemeOptions.DefaultSelectorScheme, "displayName", configureOptions =>
        {
            configureOptions.ForwardDefaultSelector = context =>
            {
                var authorization = context.Request.Headers.Authorization.ToString();
                if (!string.IsNullOrEmpty(authorization) && authorization.StartsWith("Bearer "))
                {
                    var token = authorization["Bearer ".Length..].Trim();
                    var jwtHandler = new JsonWebTokenHandler();
                    if (jwtHandler.CanReadToken(token))
                    {
                        var issuer = jwtHandler.ReadJsonWebToken(token).Issuer;
                        if (options.AuthenticationSchemeForIssuer.TryGetValue(issuer, out var scheme))
                        {
                            return scheme;
                        }
                    }
                }

                return JwtBearerDefaults.AuthenticationScheme;
            };
        });
    }
}

/// <summary>
/// Options for the multiple bearer policy scheme.
/// </summary>
public class MultipleBearerPolicySchemeOptions
{
    /// <summary>
    /// The default selector scheme.
    /// </summary>
    public const string DefaultSelectorScheme = "defaultSelectorScheme";

    /// <summary>
    /// Dictionary of token issuers and their corresponding authentication schemes.
    /// </summary>
    public IDictionary<string, string> AuthenticationSchemeForIssuer { get; set; } = new Dictionary<string, string>();
}
