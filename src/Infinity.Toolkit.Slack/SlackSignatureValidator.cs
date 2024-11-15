using System.Security.Cryptography;
using System.Text;

namespace Infinity.Toolkit.Slack;

public class SlackSignatureValidator(IConfiguration configuration)
{
    private readonly string signingSecret = configuration["SLACK_SIGNING_SECRET"] ?? throw new ArgumentNullException(nameof(configuration));

    public bool IsValid(string signature, string timestamp, string body)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(signingSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"v0:{timestamp}:{body}"));
        var mySignature = $"v0={Convert.ToHexStringLower(hash)}";
        return mySignature == signature;
    }
}
