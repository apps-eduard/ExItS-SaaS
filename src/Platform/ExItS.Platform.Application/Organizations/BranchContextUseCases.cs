using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public sealed record OrganizationBranchContextDto(
    Guid OrganizationId,
    Guid BranchId,
    string Name,
    string Code,
    string Status,
    bool IsPrimary);

public sealed record SelectOrganizationBranchContextCommand(Guid BranchId);

/// <summary>
/// Validates that an authenticated org member may select an active branch in the current organization.
/// Does not grant POS selling rights. Staff require explicit branch assignments (P28-WP15C).
/// </summary>
public sealed class SelectOrganizationBranchContext(
    IOrganizationBranchRepository branches,
    IOrganizationBranchAccessService branchAccess)
{
    public async Task<ApplicationResult<OrganizationBranchContextDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        PlatformUserId actorUserId,
        CancellationToken cancellationToken = default)
    {
        var branch = await branches.GetByIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        if (branch is null || branch.OrganizationId != organizationId)
        {
            return ApplicationResult<OrganizationBranchContextDto>.Failure(
                ApplicationErrorCodes.BranchNotFound,
                "The selected branch was not found in this organization.");
        }

        if (branch.Status != OrganizationBranchStatus.Active)
        {
            return ApplicationResult<OrganizationBranchContextDto>.Failure(
                ApplicationErrorCodes.BranchNotSelectable,
                "Only an Active branch in the current organization can be selected.");
        }

        if (!await branchAccess
                .CanAccessBranchAsync(actorUserId, organizationId, branchId, cancellationToken)
                .ConfigureAwait(false))
        {
            return ApplicationResult<OrganizationBranchContextDto>.Failure(
                ApplicationErrorCodes.BranchAccessDenied,
                "You are not authorized to access this branch in the current organization.");
        }

        return ApplicationResult<OrganizationBranchContextDto>.Success(
            new OrganizationBranchContextDto(
                branch.OrganizationId.Value,
                branch.Id.Value,
                branch.Name,
                branch.Code,
                branch.Status.ToString(),
                branch.IsPrimary));
    }
}
