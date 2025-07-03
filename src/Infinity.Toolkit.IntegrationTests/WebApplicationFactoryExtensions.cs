using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace Infinity.Toolkit.AspNetCore.Testing;

public static class WebApplicationFactoryExtensions
{
    public const string AuthenticationScheme = "IntegrationTest";

    /// <summary>
    /// Creates a HttpClient with a custom authentication scheme.
    /// </summary>
    /// <typeparam name="T">The type of the application being tested.</typeparam>
    /// <param name="factory">The WebApplicationFactory instance.</param>
    /// <param name="action">Optional action to configure additional services.</param>
    /// <param name="authenticationScheme">The authentication scheme to use. Defaults to "IntegrationTest".</param>
    /// <returns>A configured HttpClient instance.</returns>
    public static HttpClient CreateClientWithAuthentication<T>(this WebApplicationFactory<T> factory, Action<IServiceCollection>? action = null, string authenticationScheme = AuthenticationScheme)
        where T : class
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddAuthentication(authenticationScheme)
                        .AddScheme<IntegrationTestAuthenticationSchemeOptions, IntegrationTestAuthenticationHandler>(authenticationScheme, op => { });

                services
                    .AddAuthorization()
                    .AddScoped(_ => IntegrationTestClaimsProvider.CreateTestUser("Test"));

                action?.Invoke(services);
            });
        })
        .CreateClient();
    }

    /// <summary>
    /// Creates a HttpClient with a JWT token in the Authorization header.
    /// </summary>
    /// <typeparam name="T">The type of the application being tested.</typeparam>
    /// <param name="factory">The WebApplicationFactory instance.</param>
    /// <param name="action">An optional action to configure additional services.</param>
    /// <param name="authenticationScheme">Optional. The authentication scheme to use for the JWT token. Defaults to JwtBearerDefaults.AuthenticationScheme.</param>
    /// <returns>A configured HttpClient instance with a valid JWT token in the Authorization header.</returns>
    public static HttpClient CreateClientWithJwtAuthentication<T>(this WebApplicationFactory<T> factory, Action<IServiceCollection>? action = null, string authenticationScheme = JwtBearerDefaults.AuthenticationScheme, string configurationSectionKey = "JwtSettings")
    where T : class
    {
        factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services => action?.Invoke(services));
        });

        var configuration = factory.Services.GetService<IConfiguration>() ?? throw new InvalidOperationException("Configuration service not found.");
        var jwtSettings = (configuration?.GetSection(configurationSectionKey)) ?? throw new InvalidOperationException($"Configuration section '{configurationSectionKey}' not found.");

        var jwtToken = new JsonWebTokenBuilder()
            .AddAudience(jwtSettings?["Audience"] ?? "test-audience")
            .AddIssuer(jwtSettings?["Issuer"] ?? "test-issuer")
            .AddSubject(jwtSettings?["Subject"] ?? "test-subject")
            .AddSecurityKey(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings?["SigningKey"] ?? "invalid-security-key")))
            .AddExpiry(jwtSettings?.GetValue<int>("Expiration") ?? 5)
            .AddClaim(ClaimTypes.NameIdentifier, "TestUser")
            .Build();

        var httpClient = factory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(authenticationScheme, jwtToken);

        return httpClient;
    }
}

public class IntegrationTestAuthenticationSchemeOptions : AuthenticationSchemeOptions
{
}

public class IntegrationTestAuthenticationHandler(IOptionsMonitor<IntegrationTestAuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, IntegrationTestClaimsProvider claimsProvider) : AuthenticationHandler<IntegrationTestAuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity(claimsProvider.Claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, WebApplicationFactoryExtensions.AuthenticationScheme);

        var result = AuthenticateResult.Success(ticket);

        return Task.FromResult(result);
    }
}

public class IntegrationTestClaimsProvider
{
    internal IntegrationTestClaimsProvider(IList<Claim>? claims = default)
    {
        Claims = claims ?? [];
    }

    public IList<Claim> Claims { get; }

    /// <summary>
    /// Creates a test user with a unique identifier and name claim.
    /// </summary>
    /// <param name="name">The name of the user.</param>
    public static IntegrationTestClaimsProvider CreateTestUser(string name)
    {
        var provider = new IntegrationTestClaimsProvider();
        provider.Claims.Add(new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));
        provider.Claims.Add(new Claim(ClaimTypes.Name, name));

        return provider;
    }

    /// <summary>
    /// Creates a test user with a unique identifier, name claim, and role claims.
    /// </summary>
    /// <param name="name">The name of the user.</param>
    /// <param name="roleNames">An array of role names to assign to the user.</param>
    /// <param name="defaultInboundRoleClaimType">If true, uses the default role claim type (ClaimTypes.Role); otherwise, uses "roles".</param>
    public static IntegrationTestClaimsProvider CreateTestUserWithRole(string name, string[] roleNames, bool defaultInboundRoleClaimType = false)
    {
        var provider = CreateTestUser(name);

        var claimTypeRoles = defaultInboundRoleClaimType ? ClaimTypes.Role : "roles";

        foreach (var role in roleNames)
        {
            provider.Claims.Add(new Claim(claimTypeRoles, role));
        }

        return provider;
    }
}
