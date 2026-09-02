using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

/// <summary>
/// Central branch-access resolution for Platform and POS clients (P28-WP15C).
/// </summary>
public interface IOrganizationBranchAccessService
{
    Task<bool> CanAccessBranchAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Active branch ids the user may select. Empty when none. Null = all Active branches
    /// (Owner/Administrator role, or ordinary member with <see cref="BranchAccessScope.AllActive"/>).
    /// </summary>
    Task<IReadOnlySet<Guid>?> ResolveAccessibleActiveBranchIdsAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);
}

public sealed class OrganizationBranchAccessService(
    IOrganizationMembershipRepository memberships,
    IOrganizationBranchRepository branches,
    IOrganizationMembershipBranchAssignmentRepository assignments) : IOrganizationBranchAccessService
{
    public static bool HasOrganizationWideBranchAccess(OrganizationRole role) =>
        role is OrganizationRole.OrganizationOwner or OrganizationRole.OrganizationAdministrator;

    /// <summary>
    /// Ordinary member with dynamic all-active scope (not Owner/Admin — those use role alone).
    /// </summary>
    public static bool HasDynamicAllActiveBranchAccess(OrganizationMembership membership) =>
        !HasOrganizationWideBranchAccess(membership.Role)
        && membership.BranchAccessScope == BranchAccessScope.AllActive;

    public async Task<bool> CanAccessBranchAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var branch = await branches.GetByIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        if (branch is null
            || branch.OrganizationId != organizationId
            || branch.Status != OrganizationBranchStatus.Active)
        {
            return false;
        }

        var membership = await memberships
            .FindActiveByUserAndOrganizationAsync(userId, organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            return false;
        }

        if (HasOrganizationWideBranchAccess(membership.Role)
            || HasDynamicAllActiveBranchAccess(membership))
        {
            return true;
        }

        var assigned = await assignments
            .ListByMembershipAsync(membership.Id, cancellationToken)
            .ConfigureAwait(false);
        return assigned.Any(a => a.BranchId == branchId);
    }

    public async Task<IReadOnlySet<Guid>?> ResolveAccessibleActiveBranchIdsAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var membership = await memberships
            .FindActiveByUserAndOrganizationAsync(userId, organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            return new HashSet<Guid>();
        }

        if (HasOrganizationWideBranchAccess(membership.Role)
            || HasDynamicAllActiveBranchAccess(membership))
        {
            return null;
        }

        var orgBranches = await branches
            .ListByOrganizationAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var activeBranchIds = orgBranches
            .Where(b => b.Status == OrganizationBranchStatus.Active)
            .Select(b => b.Id.Value)
            .ToHashSet();

        var assigned = await assignments
            .ListByMembershipAsync(membership.Id, cancellationToken)
            .ConfigureAwait(false);
        activeBranchIds.IntersectWith(assigned.Select(a => a.BranchId.Value));
        return activeBranchIds;
    }
}
