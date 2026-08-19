using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Governance;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Organizations;

internal static class GovernanceStepUpHelper
{
    public static async Task<IResult?> EnsureConsumedAsync(
        ConsumeGovernanceStepUpGrant consumer,
        PlatformUserId? actorUserId,
        PlatformOrganizationId organizationId,
        string actionCode,
        string targetType,
        Guid? targetId,
        string? stepUpToken,
        CancellationToken cancellationToken)
    {
        var stepUp = await consumer.ExecuteAsync(
            actorUserId,
            organizationId,
            actionCode,
            targetType,
            targetId,
            stepUpToken,
            cancellationToken).ConfigureAwait(false);
        if (stepUp.IsSuccess)
        {
            return null;
        }

        var status = stepUp.ErrorCode switch
        {
            ApplicationErrorCodes.GovernanceStepUpExpired => StatusCodes.Status410Gone,
            ApplicationErrorCodes.GovernanceStepUpConsumed => StatusCodes.Status409Conflict,
            ApplicationErrorCodes.PasswordInvalid or ApplicationErrorCodes.CredentialLockedOut => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status403Forbidden
        };

        return PlatformApiResults.Problem(
            stepUp.ErrorCode ?? ApplicationErrorCodes.StepUpRequired,
            stepUp.ErrorMessage ?? "Password step-up is required.",
            status);
    }
}

internal sealed record IssueGovernanceStepUpRequest(
    string ActionCode,
    string TargetType,
    Guid? TargetId,
    string CurrentPassword);

internal sealed record GovernanceCriticalActionRequest(
    string? Reason,
    string? StepUpToken);
