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

public sealed record MembershipAreaAssignmentDto(
    Guid AreaId,
    string Name,
    string? Code);

public sealed record MembershipBranchAccessDto(
    string Scope,
    IReadOnlyList<MembershipBranchAssignmentDto> Branches,
    IReadOnlyList<MembershipAreaAssignmentDto>? Areas = null);

public sealed class ListMembershipBranchAssignments(
    IOrganizationMembershipRepository memberships,
    IOrganizationBranchRepository branches,
    IOrganizationMembershipBranchAssignmentRepository assignments,
    IOrganizationAreaRepository areas,
    IOrganizationMembershipAreaAssignmentRepository areaAssignments,
    IOrganizationBranchAccessService branchAccess)
{
    public async Task<ApplicationResult<MembershipBranchAccessDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationMembershipId membershipId,
        PlatformUserId actorUserId,
        CancellationToken cancellationToken = default)
    {
        var membership = await memberships.GetByIdAsync(membershipId, cancellationToken).ConfigureAwait(false);
        if (membership is null || membership.OrganizationId != organizationId)
        {
            return ApplicationResult<MembershipBranchAccessDto>.Failure(
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

        var scope = OrganizationBranchAccessService.HasOrganizationWideBranchAccess(membership.Role)
            ? BranchAccessScope.AllActive
            : membership.BranchAccessScope;

        IReadOnlyCollection<Guid> selected;
        IReadOnlyList<MembershipAreaAssignmentDto>? grantedAreas = null;
        if (OrganizationBranchAccessService.HasOrganizationWideBranchAccess(membership.Role)
            || membership.BranchAccessScope == BranchAccessScope.AllActive)
        {
            selected = active.Keys.ToList();
        }
        else if (membership.BranchAccessScope == BranchAccessScope.Areas)
        {
            var orgAreas = await areas.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
            var activeAreas = orgAreas
                .Where(a => a.Status == OrganizationAreaStatus.Active)
                .ToDictionary(a => a.Id.Value);
            var areaRows = await areaAssignments.ListByMembershipAsync(membershipId, cancellationToken).ConfigureAwait(false);
            var grantedIds = areaRows
                .Select(r => r.AreaId.Value)
                .Where(activeAreas.ContainsKey)
                .Distinct()
                .ToHashSet();
            grantedAreas = grantedIds
                .Select(id => new MembershipAreaAssignmentDto(id, activeAreas[id].Name, activeAreas[id].Code))
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            selected = active.Values
                .Where(b => b.AreaId is not null && grantedIds.Contains(b.AreaId.Value))
                .Select(b => b.Id.Value)
                .ToList();
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

        return ApplicationResult<MembershipBranchAccessDto>.Success(
            new MembershipBranchAccessDto(scope.ToString(), result, grantedAreas));
    }
}

public sealed record SetMembershipBranchAssignmentsCommand(
    string Scope,
    IReadOnlyList<Guid>? BranchIds,
    IReadOnlyList<Guid>? AreaIds = null);

public sealed class SetMembershipBranchAssignments(
    IOrganizationMembershipRepository memberships,
    IOrganizationBranchRepository branches,
    IOrganizationMembershipBranchAssignmentRepository assignments,
    IOrganizationAreaRepository areas,
    IOrganizationMembershipAreaAssignmentRepository areaAssignments,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ApplicationResult<MembershipBranchAccessDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationMembershipId membershipId,
        SetMembershipBranchAssignmentsCommand command,
        string? actorReference,
        CancellationToken cancellationToken = default)
    {
        var membership = await memberships.GetByIdAsync(membershipId, cancellationToken).ConfigureAwait(false);
        if (membership is null || membership.OrganizationId != organizationId)
        {
            return ApplicationResult<MembershipBranchAccessDto>.Failure(
                ApplicationErrorCodes.MembershipNotFound,
                "Organization membership was not found.");
        }

        if (membership.Status != MembershipStatus.Active)
        {
            return ApplicationResult<MembershipBranchAccessDto>.Failure(
                DomainErrorCodes.MembershipNotActive,
                "Branch assignments can only be changed for an active membership.");
        }

        if (OrganizationBranchAccessService.HasOrganizationWideBranchAccess(membership.Role))
        {
            return ApplicationResult<MembershipBranchAccessDto>.Failure(
                DomainErrorCodes.InvalidOrganizationRole,
                "Organization Owner and Administrator have access to all branches and do not use explicit assignments.");
        }

        if (string.IsNullOrWhiteSpace(command.Scope)
            || !Enum.TryParse<BranchAccessScope>(command.Scope, ignoreCase: true, out var scope))
        {
            return ApplicationResult<MembershipBranchAccessDto>.Failure(
                ApplicationErrorCodes.DomainViolation,
                "Branch access scope must be Explicit, AllActive, or Areas.");
        }

        var orgBranches = await branches.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
        var activeLookup = orgBranches
            .Where(b => b.Status == OrganizationBranchStatus.Active)
            .ToDictionary(b => b.Id.Value);
        var activeIds = activeLookup.Keys.ToHashSet();

        IReadOnlyList<MembershipBranchAssignmentDto> responseBranches;
        IReadOnlyList<MembershipAreaAssignmentDto>? responseAreas = null;
        if (scope == BranchAccessScope.AllActive)
        {
            membership.SetBranchAccessScope(BranchAccessScope.AllActive, clock.UtcNow, actorReference);
            await memberships.UpdateAsync(membership, cancellationToken).ConfigureAwait(false);
            await assignments.ReplaceForMembershipAsync(
                organizationId,
                membershipId,
                [],
                clock.UtcNow,
                actorReference,
                cancellationToken).ConfigureAwait(false);
            await areaAssignments.ReplaceForMembershipAsync(
                organizationId,
                membershipId,
                [],
                clock.UtcNow,
                actorReference,
                cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            responseBranches = activeLookup.Values
                .Select(b => new MembershipBranchAssignmentDto(b.Id.Value, b.Name, b.Code, b.IsPrimary))
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else if (scope == BranchAccessScope.Areas)
        {
            var requestedAreaIds = (command.AreaIds ?? [])
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();
            if (requestedAreaIds.Count == 0)
            {
                return ApplicationResult<MembershipBranchAccessDto>.Failure(
                    ApplicationErrorCodes.DomainViolation,
                    "At least one area is required for area-scoped staff access.");
            }

            var orgAreas = await areas.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
            var activeAreas = orgAreas
                .Where(a => a.Status == OrganizationAreaStatus.Active)
                .ToDictionary(a => a.Id.Value);
            if (requestedAreaIds.Any(id => !activeAreas.ContainsKey(id)))
            {
                return ApplicationResult<MembershipBranchAccessDto>.Failure(
                    ApplicationErrorCodes.AreaNotFound,
                    "One or more areas are not active in this organization.");
            }

            membership.SetBranchAccessScope(BranchAccessScope.Areas, clock.UtcNow, actorReference);
            await memberships.UpdateAsync(membership, cancellationToken).ConfigureAwait(false);
            await assignments.ReplaceForMembershipAsync(
                organizationId,
                membershipId,
                [],
                clock.UtcNow,
                actorReference,
                cancellationToken).ConfigureAwait(false);
            await areaAssignments.ReplaceForMembershipAsync(
                organizationId,
                membershipId,
                requestedAreaIds.Select(OrganizationAreaId.From).ToList(),
                clock.UtcNow,
                actorReference,
                cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var grantedIds = requestedAreaIds.ToHashSet();
            responseAreas = requestedAreaIds
                .Select(id => new MembershipAreaAssignmentDto(id, activeAreas[id].Name, activeAreas[id].Code))
                .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            responseBranches = activeLookup.Values
                .Where(b => b.AreaId is not null && grantedIds.Contains(b.AreaId.Value))
                .Select(b => new MembershipBranchAssignmentDto(b.Id.Value, b.Name, b.Code, b.IsPrimary))
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        else
        {
            var requested = (command.BranchIds ?? [])
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList();
            if (requested.Any(id => !activeIds.Contains(id)))
            {
                return ApplicationResult<MembershipBranchAccessDto>.Failure(
                    ApplicationErrorCodes.BranchNotFound,
                    "One or more branches are not active in this organization.");
            }

            if (requested.Count == 0)
            {
                return ApplicationResult<MembershipBranchAccessDto>.Failure(
                    ApplicationErrorCodes.DomainViolation,
                    "At least one branch assignment is required for organization staff.");
            }

            membership.SetBranchAccessScope(BranchAccessScope.Explicit, clock.UtcNow, actorReference);
            await memberships.UpdateAsync(membership, cancellationToken).ConfigureAwait(false);
            var branchIdEntities = requested.Select(OrganizationBranchId.From).ToList();
            await assignments.ReplaceForMembershipAsync(
                organizationId,
                membershipId,
                branchIdEntities,
                clock.UtcNow,
                actorReference,
                cancellationToken).ConfigureAwait(false);
            await areaAssignments.ReplaceForMembershipAsync(
                organizationId,
                membershipId,
                [],
                clock.UtcNow,
                actorReference,
                cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            responseBranches = requested
                .Select(id =>
                {
                    var branch = activeLookup[id];
                    return new MembershipBranchAssignmentDto(id, branch.Name, branch.Code, branch.IsPrimary);
                })
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return ApplicationResult<MembershipBranchAccessDto>.Success(
            new MembershipBranchAccessDto(scope.ToString(), responseBranches, responseAreas));
    }
}

public sealed class ListBranchStaffAccess(
    IOrganizationMembershipRepository memberships,
    IOrganizationMembershipBranchAssignmentRepository assignments,
    IOrganizationBranchRepository branches,
    IOrganizationMembershipAreaAssignmentRepository areaAssignments,
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

        var branch = await branches.GetByIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        var areaGrantedMembershipIds = new HashSet<Guid>();
        if (branch?.AreaId is not null && branch.OrganizationId == organizationId)
        {
            var areaGrants = await areaAssignments
                .ListByAreaAsync(organizationId, branch.AreaId, cancellationToken)
                .ConfigureAwait(false);
            areaGrantedMembershipIds = areaGrants.Select(a => a.MembershipId.Value).ToHashSet();
        }

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
            var allActive = OrganizationBranchAccessService.HasDynamicAllActiveBranchAccess(membership);
            var areaAccess = !wide
                             && membership.BranchAccessScope == BranchAccessScope.Areas
                             && areaGrantedMembershipIds.Contains(membership.Id.Value);
            var explicitAccess = assignedMembershipIds.Contains(membership.Id.Value) || areaAccess;
            if (!wide && !allActive && !explicitAccess)
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
                explicitAccess || allActive,
                wide || allActive));
        }

        return ApplicationResult<IReadOnlyList<BranchStaffAccessItemDto>>.Success(items);
    }
}
