using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Application.Platform;

public static class GovernanceCriticalActionFlow
{
    public static async Task<ApiResult<string>> IssueStepUpTokenAsync(
        IPlatformAccessClient client,
        Guid organizationId,
        string actionCode,
        string targetType,
        Guid targetId,
        string currentPassword,
        CancellationToken cancellationToken = default)
    {
        var issued = await client.IssueGovernanceStepUpAsync(
            organizationId,
            new IssueGovernanceStepUpRequest(actionCode, targetType, targetId, currentPassword),
            cancellationToken).ConfigureAwait(false);
        if (!issued.IsSuccess || issued.Data is null || string.IsNullOrWhiteSpace(issued.Data.StepUpToken))
        {
            var error = issued.Error
                ?? new ApiError(
                    Title: null,
                    Detail: "Governance step-up failed.",
                    ErrorCode: "governance.step_up_failed",
                    CorrelationId: null,
                    StatusCode: null);

            return ApiResult<string>.Failure(issued.Status, error);
        }

        return ApiResult<string>.Success(issued.Data.StepUpToken);
    }
}
