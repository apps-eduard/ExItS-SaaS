using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

/// <summary>
/// Area governance: grouping, access, navigation, and reporting only.
/// No Area holds stock, reservations, registers, shifts, sales, or receiving authority.
/// </summary>
public sealed record OrganizationAreaDto(
    Guid Id,
    Guid OrganizationId,
    string Name,
    string? Code,
    OrganizationAreaStatus Status,
    int BranchCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record OrganizationAreaListDto(
    IReadOnlyList<OrganizationAreaDto> Areas,
    int UnassignedBranchCount,
    int ActiveAreaCount,
    int MaxAreas);

public sealed record CreateOrganizationAreaCommand(string Name, string? Code = null);

public sealed record UpdateOrganizationAreaCommand(string Name, string? Code = null);

public sealed class ListOrganizationAreas(
    IOrganizationAreaRepository areas,
    IOrganizationBranchRepository branches,
    ISubscriptionRepository subscriptions,
    IPlanRepository plans)
{
    public async Task<ApplicationResult<OrganizationAreaListDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var orgAreas = await areas.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
        var orgBranches = await branches.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
        var liveBranches = orgBranches
            .Where(b => b.Status != OrganizationBranchStatus.Archived)
            .ToList();
        var branchCountsByArea = liveBranches
            .Where(b => b.AreaId is not null)
            .GroupBy(b => b.AreaId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var items = orgAreas
            .OrderBy(a => a.Status)
            .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Select(a => AreaMapper.ToDto(a, branchCountsByArea.GetValueOrDefault(a.Id.Value, 0)))
            .ToList();

        var maxAreas = 0;
        var limit = await PosOrganizationPlanLimits
            .ResolveAsync(organizationId, subscriptions, plans, cancellationToken)
            .ConfigureAwait(false);
        if (limit.IsSuccess && limit.Value is not null)
        {
            maxAreas = limit.Value.MaxAreas;
        }

        return ApplicationResult<OrganizationAreaListDto>.Success(new OrganizationAreaListDto(
            items,
            liveBranches.Count(b => b.AreaId is null),
            orgAreas.Count(a => a.Status == OrganizationAreaStatus.Active),
            maxAreas));
    }
}

public sealed class CreateOrganizationArea(
    IOrganizationAreaRepository areas,
    ISubscriptionRepository subscriptions,
    IPlanRepository plans,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ApplicationResult<OrganizationAreaDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CreateOrganizationAreaCommand command,
        CancellationToken cancellationToken = default)
    {
        var limit = await PosOrganizationPlanLimits
            .ResolveAsync(organizationId, subscriptions, plans, cancellationToken)
            .ConfigureAwait(false);
        if (!limit.IsSuccess || limit.Value is null)
        {
            return ApplicationResult<OrganizationAreaDto>.Failure(limit.ErrorCode!, limit.ErrorMessage!);
        }

        var activeCount = await areas.CountActiveAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (activeCount >= limit.Value.MaxAreas)
        {
            return ApplicationResult<OrganizationAreaDto>.Failure(
                ApplicationErrorCodes.AreaCapacityExceeded,
                "The active POS plan area limit has been reached.");
        }

        OrganizationArea area;
        try
        {
            area = OrganizationArea.Create(organizationId, command.Name, clock.UtcNow, command.Code);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationAreaDto>.Failure(ex.ErrorCode, ex.Message);
        }

        var existing = await areas.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (existing.Any(x => x.Status == OrganizationAreaStatus.Active
                              && string.Equals(x.Name, area.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return ApplicationResult<OrganizationAreaDto>.Failure(
                ApplicationErrorCodes.AreaNameConflict,
                "An active area with this name already exists.");
        }

        await areas.AddAsync(area, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<OrganizationAreaDto>.Success(AreaMapper.ToDto(area, 0));
    }
}

public sealed class UpdateOrganizationArea(
    IOrganizationAreaRepository areas,
    IOrganizationBranchRepository branches,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ApplicationResult<OrganizationAreaDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationAreaId areaId,
        UpdateOrganizationAreaCommand command,
        CancellationToken cancellationToken = default)
    {
        var area = await areas.GetByIdAsync(areaId, cancellationToken).ConfigureAwait(false);
        if (area is null || area.OrganizationId != organizationId)
        {
            return ApplicationResult<OrganizationAreaDto>.Failure(
                ApplicationErrorCodes.AreaNotFound,
                "Area was not found.");
        }

        try
        {
            area.Update(command.Name, command.Code, clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationAreaDto>.Failure(ex.ErrorCode, ex.Message);
        }

        var existing = await areas.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (existing.Any(x => x.Id != area.Id
                              && x.Status == OrganizationAreaStatus.Active
                              && string.Equals(x.Name, area.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return ApplicationResult<OrganizationAreaDto>.Failure(
                ApplicationErrorCodes.AreaNameConflict,
                "An active area with this name already exists.");
        }

        await areas.UpdateAsync(area, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        var count = await CountAssignedBranchesAsync(branches, organizationId, areaId, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<OrganizationAreaDto>.Success(AreaMapper.ToDto(area, count));
    }

    internal static async Task<int> CountAssignedBranchesAsync(
        IOrganizationBranchRepository branches,
        PlatformOrganizationId organizationId,
        OrganizationAreaId areaId,
        CancellationToken cancellationToken)
    {
        var orgBranches = await branches.ListByOrganizationAsync(organizationId, cancellationToken).ConfigureAwait(false);
        return orgBranches.Count(b => b.Status != OrganizationBranchStatus.Archived && b.AreaId == areaId);
    }
}

/// <summary>
/// Archives an Area only when nothing still depends on it. Never cascades to branches,
/// never deletes branches, and never moves stock.
/// </summary>
public sealed class ArchiveOrganizationArea(
    IOrganizationAreaRepository areas,
    IOrganizationBranchRepository branches,
    IOrganizationMembershipAreaAssignmentRepository areaAssignments,
    IOrganizationMembershipRepository memberships,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ApplicationResult<OrganizationAreaDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationAreaId areaId,
        CancellationToken cancellationToken = default)
    {
        var area = await areas.GetByIdAsync(areaId, cancellationToken).ConfigureAwait(false);
        if (area is null || area.OrganizationId != organizationId)
        {
            return ApplicationResult<OrganizationAreaDto>.Failure(
                ApplicationErrorCodes.AreaNotFound,
                "Area was not found.");
        }

        var assignedBranches = await UpdateOrganizationArea
            .CountAssignedBranchesAsync(branches, organizationId, areaId, cancellationToken)
            .ConfigureAwait(false);
        if (assignedBranches > 0)
        {
            return ApplicationResult<OrganizationAreaDto>.Failure(
                ApplicationErrorCodes.AreaArchiveBlocked,
                "Move or unassign every branch in this area before archiving it.");
        }

        var grants = await areaAssignments.ListByAreaAsync(organizationId, areaId, cancellationToken).ConfigureAwait(false);
        if (grants.Count > 0)
        {
            var membershipPage = await memberships
                .ListByOrganizationAsync(organizationId, MembershipStatus.Active, skip: 0, take: 500, cancellationToken)
                .ConfigureAwait(false);
            var activeMembershipIds = membershipPage.Items.Select(m => m.Id.Value).ToHashSet();
            if (grants.Any(g => activeMembershipIds.Contains(g.MembershipId.Value)))
            {
                return ApplicationResult<OrganizationAreaDto>.Failure(
                    ApplicationErrorCodes.AreaArchiveBlocked,
                    "Change staff access away from this area before archiving it.");
            }
        }

        try
        {
            area.Archive(clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationAreaDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await areas.UpdateAsync(area, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<OrganizationAreaDto>.Success(AreaMapper.ToDto(area, 0));
    }
}

public sealed record BranchAreaAssignmentDto(
    Guid BranchId,
    string Code,
    string Name,
    Guid? AreaId,
    string? AreaName);

/// <summary>
/// Places a branch in an Area, or moves it between Areas. Grouping only — a branch belongs
/// to at most one Area, and moving it changes no inventory, register, or document ownership.
/// </summary>
public sealed class SetBranchArea(
    IOrganizationBranchRepository branches,
    IOrganizationAreaRepository areas,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<ApplicationResult<BranchAreaAssignmentDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        OrganizationAreaId? areaId,
        CancellationToken cancellationToken = default)
    {
        var branch = await branches.GetByIdAsync(branchId, cancellationToken).ConfigureAwait(false);
        if (branch is null || branch.OrganizationId != organizationId)
        {
            return ApplicationResult<BranchAreaAssignmentDto>.Failure(
                ApplicationErrorCodes.BranchNotFound,
                "Branch was not found.");
        }

        OrganizationArea? area = null;
        if (areaId is not null)
        {
            area = await areas.GetByIdAsync(areaId, cancellationToken).ConfigureAwait(false);
            if (area is null || area.OrganizationId != organizationId)
            {
                return ApplicationResult<BranchAreaAssignmentDto>.Failure(
                    ApplicationErrorCodes.AreaNotFound,
                    "Area was not found.");
            }

            if (area.Status != OrganizationAreaStatus.Active)
            {
                return ApplicationResult<BranchAreaAssignmentDto>.Failure(
                    DomainErrorCodes.OrganizationAreaNotActive,
                    "Branches can only be placed in an active area.");
            }
        }

        try
        {
            if (area is null)
            {
                branch.UnassignArea(clock.UtcNow);
            }
            else
            {
                branch.AssignArea(area.Id, clock.UtcNow);
            }
        }
        catch (DomainException ex)
        {
            return ApplicationResult<BranchAreaAssignmentDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await branches.UpdateAsync(branch, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<BranchAreaAssignmentDto>.Success(new BranchAreaAssignmentDto(
            branch.Id.Value,
            branch.Code,
            branch.Name,
            branch.AreaId?.Value,
            area?.Name));
    }
}

internal static class AreaMapper
{
    public static OrganizationAreaDto ToDto(OrganizationArea area, int branchCount) =>
        new(
            area.Id.Value,
            area.OrganizationId.Value,
            area.Name,
            area.Code,
            area.Status,
            branchCount,
            area.CreatedAtUtc,
            area.UpdatedAtUtc);
}
