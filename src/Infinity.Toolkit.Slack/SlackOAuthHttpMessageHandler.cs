namespace Infinity.Toolkit.Slack;

public sealed class SlackOAuthHttpMessageHandler(IConfiguration configuration) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var botUserOAuthToken = configuration["SLACK_BOT_USER_OAUTH_TOKEN"] ?? throw new Exception("Missing configuration key SLACK_BOT_USER_OAUTH_TOKEN");

        request.Headers.Authorization = new("Bearer", botUserOAuthToken);
        return await base.SendAsync(request, cancellationToken);
    }
}
