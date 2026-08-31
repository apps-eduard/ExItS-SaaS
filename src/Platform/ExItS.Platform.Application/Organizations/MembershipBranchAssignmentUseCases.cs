using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public sealed record MembershipBranchAssignmentDto(
    Guid BranchId,
    string Name,
    string Code,
    bool IsPrimary);

public sealed class ListMembershipBranchAssignments(
    IOrganizationMembershipRepository memberships,
    IOrganizationBranchRepository branches,
    IOrganizationMembershipBranchAssignmentRepository assignments,
    IOrganizationBranchAccessService branchAccess)
{
    public async Task<ApplicationResult<IReadOnlyList<MembershipBranchAssignmentDto>>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationMembershipId membershipId,
        PlatformUserId actorUserId,
        CancellationToken cancellationToken = default)
    {
        var membership = await memberships.GetByIdAsync(membershipId, cancellationToken).ConfigureAwait(false);
        if (membership is null || membership.OrganizationId != organizationId)
        {
            return ApplicationResult<IReadOnlyList<MembershipBranchAssignmentDto>>.Failure(
                ApplicationErrorCodes.MembershipNotFound,
                "Organization membership was not found.");
        }

        var accessible = await branchAccess
            .ResolveAccessibleActiveBranchIdsAsync(actorUserId, organizationId, cancellationToken)
            .ConfigureAwait(false);
        var orgBranches = await branches.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
        var active = orgBranches
            .Where(b => b.Status == OrganizationBranchStatus.Active)
            .ToDictionary(b => b.Id.Value);

        IReadOnlyCollection<Guid> selected;
        if (OrganizationBranchAccessService.HasOrganizationWideBranchAccess(membership.Role))
        {
            selected = active.Keys.ToList();
        }
        else
        {
            var rows = await assignments.ListByMembershipAsync(membershipId, cancellationToken).ConfigureAwait(false);
            selected = rows.Select(r => r.BranchId.Value).ToList();
        }

        var result = selected
            .Where(active.ContainsKey)
            .Select(id =>
            {
                var branch = active[id];
                return new MembershipBranchAssignmentDto(
                    id,
                    branch.Name,
                    branch.Code,
                    branch.IsPrimary);
            })
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (accessible is not null)
        {
            result = result.Where(x => accessible.Contains(x.BranchId)).ToList();
        }

        return ApplicationResult<IReadOnlyList<MembershipBranchAssignmentDto>>.Success(result);
    }
}

public sealed record SetMembershipBranchAssignmentsCommand(IReadOnlyList<Guid> BranchIds);

public sealed class SetMembershipBranchAssignments(
    IOrganizationMembershipRepository memberships,
    IOrganizationBranchRepository branches,
    IOrganizationMembershipBranchAssignmentRepository assignments,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ApplicationResult<IReadOnlyList<MembershipBranchAssignmentDto>>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationMembershipId membershipId,
        SetMembershipBranchAssignmentsCommand command,
        string? actorReference,
        CancellationToken cancellationToken = default)
    {
        var membership = await memberships.GetByIdAsync(membershipId, cancellationToken).ConfigureAwait(false);
        if (membership is null || membership.OrganizationId != organizationId)
        {
            return ApplicationResult<IReadOnlyList<MembershipBranchAssignmentDto>>.Failure(
                ApplicationErrorCodes.MembershipNotFound,
                "Organization membership was not found.");
        }

        if (membership.Status != MembershipStatus.Active)
        {
            return ApplicationResult<IReadOnlyList<MembershipBranchAssignmentDto>>.Failure(
                DomainErrorCodes.MembershipNotActive,
                "Branch assignments can only be changed for an active membership.");
        }

        if (OrganizationBranchAccessService.HasOrganizationWideBranchAccess(membership.Role))
        {
            return ApplicationResult<IReadOnlyList<MembershipBranchAssignmentDto>>.Failure(
                DomainErrorCodes.InvalidOrganizationRole,
                "Organization Owner and Administrator have access to all branches and do not use explicit assignments.");
        }

        var orgBranches = await branches.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
        var activeIds = orgBranches
            .Where(b => b.Status == OrganizationBranchStatus.Active)
            .Select(b => b.Id.Value)
            .ToHashSet();
        var requested = (command.BranchIds ?? [])
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
        if (requested.Any(id => !activeIds.Contains(id)))
        {
            return ApplicationResult<IReadOnlyList<MembershipBranchAssignmentDto>>.Failure(
                ApplicationErrorCodes.BranchNotFound,
                "One or more branches are not active in this organization.");
        }

        if (requested.Count == 0)
        {
            return ApplicationResult<IReadOnlyList<MembershipBranchAssignmentDto>>.Failure(
                ApplicationErrorCodes.DomainViolation,
                "At least one branch assignment is required for organization staff.");
        }

        var branchIdEntities = requested.Select(OrganizationBranchId.From).ToList();
        await assignments.ReplaceForMembershipAsync(
            organizationId,
            membershipId,
            branchIdEntities,
            clock.UtcNow,
            actorReference,
            cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var lookup = orgBranches.ToDictionary(b => b.Id.Value);
        var dtos = requested
            .Select(id =>
            {
                var branch = lookup[id];
                return new MembershipBranchAssignmentDto(id, branch.Name, branch.Code, branch.IsPrimary);
            })
            .OrderByDescending(x => x.IsPrimary)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return ApplicationResult<IReadOnlyList<MembershipBranchAssignmentDto>>.Success(dtos);
    }
}

public sealed class ListBranchStaffAccess(
    IOrganizationMembershipRepository memberships,
    IOrganizationMembershipBranchAssignmentRepository assignments,
    IProductLocalRoleGrantRepository roleGrants,
    IPlatformUserRepository users)
{
    public async Task<ApplicationResult<IReadOnlyList<BranchStaffAccessItemDto>>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        CancellationToken cancellationToken = default)
    {
        var membershipPage = await memberships
            .ListByOrganizationAsync(organizationId, status: null, skip: 0, take: 500, cancellationToken)
            .ConfigureAwait(false);
        var currentMembers = membershipPage.Items
            .Where(m => m.Status is MembershipStatus.Active or MembershipStatus.Suspended)
            .ToList();

        var branchAssignments = await assignments
            .ListByBranchAsync(organizationId, branchId, cancellationToken)
            .ConfigureAwait(false);
        var assignedMembershipIds = branchAssignments.Select(a => a.MembershipId.Value).ToHashSet();

        var grants = await roleGrants
            .ListByOrganizationAsync(organizationId, ProductLocalRoleGrantStatus.Active, cancellationToken)
            .ConfigureAwait(false);
        var grantByUser = grants
            .GroupBy(g => g.UserIdentityId.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var userIds = currentMembers.Select(m => m.UserId).Distinct().ToList();
        var usersById = (await users.ListByIdsAsync(userIds, cancellationToken).ConfigureAwait(false))
            .ToDictionary(u => u.Id.Value);

        var items = new List<BranchStaffAccessItemDto>();
        foreach (var membership in currentMembers
                     .OrderBy(m => m.Role)
                     .ThenBy(m => usersById.TryGetValue(m.UserId.Value, out var u) ? u.DisplayName : m.UserId.Value.ToString("D"),
                         StringComparer.OrdinalIgnoreCase))
        {
            var wide = OrganizationBranchAccessService.HasOrganizationWideBranchAccess(membership.Role);
            var explicitAccess = assignedMembershipIds.Contains(membership.Id.Value);
            if (!wide && !explicitAccess)
            {
                continue;
            }

            usersById.TryGetValue(membership.UserId.Value, out var user);
            var displayName = user?.DisplayName?.Trim()
                ?? user?.NormalizedEmail?.Trim()
                ?? membership.UserId.Value.ToString("D");
            grantByUser.TryGetValue(membership.UserId.Value, out var grant);
            items.Add(new BranchStaffAccessItemDto(
                membership.Id.Value,
                membership.UserId.Value,
                displayName,
                membership.Role.ToString(),
                membership.Status.ToString(),
                grant?.RoleCode,
                grant is null ? null : ProductRoleDisplay.ToDisplayLabel(grant.RoleCode),
                explicitAccess,
                wide));
        }

        return ApplicationResult<IReadOnlyList<BranchStaffAccessItemDto>>.Success(items);
    }
}
