using Microsoft.AspNetCore.Authorization;

namespace Infinity.Toolkit.Slack;

public class SlackSignatureAuthorizationRequirement : IAuthorizationRequirement { }

public class SlackSignatureAuthorizationHandler(IHttpContextAccessor httpContextAccessor, SlackSignatureValidator slackSignatureValidator) : AuthorizationHandler<SlackSignatureAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, SlackSignatureAuthorizationRequirement requirement)
    {
        var signature = httpContextAccessor?.HttpContext?.Request.Headers["X-Slack-Signature"].ToString();

        if (!string.IsNullOrEmpty(signature))
        {
            var slackRequestTimestamp = httpContextAccessor?.HttpContext?.Request.Headers["X-Slack-Request-Timestamp"].ToString();
            var form = httpContextAccessor?.HttpContext?.Request.Form;
            var body = string.Join("&", form!.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value.ToString())}"));

            if (!slackSignatureValidator.IsValid(signature, slackRequestTimestamp!, body))
            {
                context.Fail();
                return Task.CompletedTask;
            }
        }
        else
        {
            context.Fail();
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
