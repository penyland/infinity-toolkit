using Microsoft.AspNetCore.Authorization;
using System.Text;

namespace Infinity.Toolkit.Slack;

public class SlackSignatureAuthorizationRequirement : IAuthorizationRequirement
{
}

public class SlackSignatureAuthorizationHandler(IHttpContextAccessor httpContextAccessor, SlackSignatureValidator slackSignatureValidator) : AuthorizationHandler<SlackSignatureAuthorizationRequirement>
{
    private readonly IHttpContextAccessor httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, SlackSignatureAuthorizationRequirement requirement)
    {
        var signature = httpContextAccessor?.HttpContext?.Request.Headers["X-Slack-Signature"].ToString();

        if (!string.IsNullOrEmpty(signature))
        {
            var request = httpContextAccessor?.HttpContext?.Request;
            request?.EnableBuffering();

            using var reader = new StreamReader(request!.Body, Encoding.UTF8, true, 1024, true);
            var body = await reader.ReadToEndAsync();
            request.Body.Position = 0;

            var slackRequestTimestamp = httpContextAccessor?.HttpContext?.Request.Headers["X-Slack-Request-Timestamp"].ToString();

            if (slackSignatureValidator.IsValid(signature, slackRequestTimestamp!, body))
            {
                context.Succeed(requirement);
            }
        }
    }
}
