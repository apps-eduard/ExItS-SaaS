using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Governance;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Organizations;

internal static class GovernanceStepUpEndpoints
{
    public static IEndpointRouteBuilder MapGovernanceStepUpEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/platform/organizations/{organizationId:guid}/governance/step-up", async (
            Guid organizationId,
            IssueGovernanceStepUpRequest body,
            IssueGovernanceStepUpGrant useCase,
            PlatformOrganizationAuthz authz,
            PlatformAuthz platformAuthz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = platformAuthz.CurrentActor.PlatformUserId;
            if (actor is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.SessionInvalid,
                    "Authenticated Platform user is required.",
                    StatusCodes.Status401Unauthorized);
            }

            var result = await useCase.ExecuteAsync(
                actor,
                PlatformOrganizationId.From(organizationId),
                new IssueGovernanceStepUpCommand(
                    body.ActionCode ?? string.Empty,
                    body.TargetType ?? string.Empty,
                    body.TargetId,
                    body.CurrentPassword ?? string.Empty),
                ct).ConfigureAwait(false);

            if (result.IsSuccess)
            {
                await authz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.GovernanceStepUpSucceeded,
                    body.TargetType ?? "GovernanceAction",
                    body.TargetId?.ToString("D") ?? organizationId.ToString("D"),
                    organizationId,
                    summary: $"Password step-up issued for {body.ActionCode}. Auth strength: PasswordStepUp.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        return app;
    }
}
