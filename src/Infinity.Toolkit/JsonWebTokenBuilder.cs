using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace Infinity.Toolkit;

public sealed class JsonWebTokenBuilder
{
    private readonly List<Claim> claims = [];
    private SecurityKey? securityKey = default;
    private string audience = "";
    private string issuer = "";
    private string subject = "";
    private int expiryInMinutes = 5;

    public JsonWebTokenBuilder AddAudience(string audience)
    {
        this.audience = audience ?? throw new ArgumentNullException(nameof(audience));
        return this;
    }

    public JsonWebTokenBuilder AddClaim(string type, string value)
    {
        claims.Add(new Claim(type, value));
        return this;
    }

    public JsonWebTokenBuilder AddExpiry(int expiryInMinutes)
    {
        if (expiryInMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(expiryInMinutes), "Expiry time must be greater than zero.");
        }

        this.expiryInMinutes = expiryInMinutes;
        return this;
    }

    public JsonWebTokenBuilder AddIssuer(string issuer)
    {
        this.issuer = issuer ?? throw new ArgumentNullException(nameof(issuer));
        return this;
    }

    public JsonWebTokenBuilder AddSecurityKey(SecurityKey securityKey)
    {
        this.securityKey = securityKey ?? throw new ArgumentNullException(nameof(securityKey));
        return this;
    }

    public JsonWebTokenBuilder AddSubject(string subject)
    {
        this.subject = subject;
        return this;
    }

    public JwToken Build()
    {
        List<Claim> c = [.. claims, new Claim(JwtRegisteredClaimNames.Sub, subject), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())];

        var token = new SecurityTokenDescriptor
        {
            Audience = audience,
            Claims = claims.ToDictionary(t => t.Type, v => v.Value as object),
            Issuer = issuer,
            IssuedAt = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddMinutes(expiryInMinutes),
            SigningCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(claims),
        };

        return new JwToken(token);
    }
}

public sealed class JwToken(SecurityTokenDescriptor token)
{
    private readonly SecurityTokenDescriptor token = token ?? throw new ArgumentNullException(nameof(token));

    public DateTime ValidTo => token.Expires ?? DateTime.MinValue;

    public string Value => new JsonWebTokenHandler().CreateToken(token);

    public static implicit operator string(JwToken jwtToken)
    {
        return jwtToken.Value;
    }
}
