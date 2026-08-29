using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Reporting;

/// <summary>
/// Shared fail-closed validation for optional report <c>branchId</c> query scope.
/// Absent branch = organization-wide aggregate. Invalid/cross-org branch never falls back to org totals.
/// </summary>
public static class PosReportBranchScope
{
    /// <summary>
    /// Returns a failure result when <paramref name="branchId"/> is set but not valid for the organization;
    /// otherwise <c>null</c> (caller may proceed).
    /// </summary>
    public static async Task<ApplicationResult?> ValidateOptionalAsync(
        IOrganizationBranchDirectory branches,
        Guid organizationId,
        Guid? branchId,
        CancellationToken cancellationToken = default)
    {
        if (branchId is null)
        {
            return null;
        }

        if (branchId.Value == Guid.Empty)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.DomainViolation,
                "branchId cannot be an empty GUID.");
        }

        var exists = await branches
            .ExistsInOrganizationAsync(organizationId, branchId.Value, cancellationToken)
            .ConfigureAwait(false);
        if (!exists)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.ReportBranchNotFound,
                "Branch was not found in the current organization.");
        }

        return null;
    }
}
