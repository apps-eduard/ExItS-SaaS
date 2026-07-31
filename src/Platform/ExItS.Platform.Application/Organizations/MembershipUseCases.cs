using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Organizations;

public sealed record OrganizationMembershipDto(
    Guid Id,
    Guid OrganizationId,
    Guid UserId,
    string Role,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? SuspendedAtUtc,
    DateTimeOffset? RemovedAtUtc,
    string? Reason,
    string? ActorReference);

public sealed class MembershipQueryService
{
    private readonly IOrganizationMembershipRepository _memberships;

    public MembershipQueryService(IOrganizationMembershipRepository memberships) => _memberships = memberships;

    public async Task<OrganizationMembershipDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var membership = await _memberships.GetByIdAsync(OrganizationMembershipId.From(id), cancellationToken)
            .ConfigureAwait(false);
        return membership is null ? null : Map(membership);
    }

    public async Task<PagedResult<OrganizationMembershipDto>> ListByOrganizationAsync(
        Guid organizationId,
        MembershipStatus? status,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, total) = await _memberships
            .ListByOrganizationAsync(PlatformOrganizationId.From(organizationId), status, skip, take, cancellationToken)
            .ConfigureAwait(false);
        return new PagedResult<OrganizationMembershipDto>(
            items.Select(Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public async Task<PagedResult<OrganizationMembershipDto>> ListByUserAsync(
        Guid userId,
        MembershipStatus? status,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, total) = await _memberships
            .ListByUserAsync(PlatformUserId.From(userId), status, skip, take, cancellationToken)
            .ConfigureAwait(false);
        return new PagedResult<OrganizationMembershipDto>(
            items.Select(Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public static OrganizationMembershipDto Map(OrganizationMembership membership) =>
        new(
            membership.Id.Value,
            membership.OrganizationId.Value,
            membership.UserId.Value,
            membership.Role.ToString(),
            membership.Status.ToString(),
            membership.CreatedAtUtc,
            membership.UpdatedAtUtc,
            membership.SuspendedAtUtc,
            membership.RemovedAtUtc,
            membership.Reason,
            membership.ActorReference);
}

public sealed class ReactivateOrganizationMembership
{
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReactivateOrganizationMembership(
        IOrganizationMembershipRepository memberships,
        IPlatformUserRepository users,
        IPlatformOrganizationRepository organizations,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _memberships = memberships;
        _users = users;
        _organizations = organizations;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<OrganizationMembership>> ExecuteAsync(
        OrganizationMembershipId membershipId,
        string? actorReference = null,
        CancellationToken cancellationToken = default)
    {
        var membership = await _memberships.GetByIdAsync(membershipId, cancellationToken).ConfigureAwait(false);
        if (membership is null)
        {
            return ApplicationResult<OrganizationMembership>.Failure(
                ApplicationErrorCodes.MembershipNotFound,
                "Membership was not found.");
        }

        var user = await _users.GetByIdAsync(membership.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status != AccountStatus.Active)
        {
            return ApplicationResult<OrganizationMembership>.Failure(
                DomainErrorCodes.UserNotActive,
                "Membership reactivation requires an active Platform User.");
        }

        var organization = await _organizations.GetByIdAsync(membership.OrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (organization is null || organization.Status != OrganizationStatus.Active)
        {
            return ApplicationResult<OrganizationMembership>.Failure(
                DomainErrorCodes.OrganizationNotActive,
                "Membership reactivation requires an active Platform Organization.");
        }

        try
        {
            membership.Reactivate(_clock.UtcNow, actorReference);
            await _memberships.UpdateAsync(membership, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<OrganizationMembership>.Success(membership);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationMembership>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class RevokeOrganizationMembership
{
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IProductAccessAssignmentRepository _assignments;
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RevokeOrganizationMembership(
        IOrganizationMembershipRepository memberships,
        IProductAccessAssignmentRepository assignments,
        IPlatformAuthSessionRepository sessions,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _memberships = memberships;
        _assignments = assignments;
        _sessions = sessions;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<OrganizationMembership>> ExecuteAsync(
        OrganizationMembershipId membershipId,
        string? reason = null,
        string? actorReference = null,
        CancellationToken cancellationToken = default)
    {
        var membership = await _memberships.GetByIdAsync(membershipId, cancellationToken).ConfigureAwait(false);
        if (membership is null)
        {
            return ApplicationResult<OrganizationMembership>.Failure(
                ApplicationErrorCodes.MembershipNotFound,
                "Membership was not found.");
        }

        try
        {
            var actor = string.IsNullOrWhiteSpace(actorReference) ? "system" : actorReference;
            membership.Remove(_clock.UtcNow, reason, actor);
            await _memberships.UpdateAsync(membership, cancellationToken).ConfigureAwait(false);

            var activeAssignments = await _assignments
                .ListActiveByMembershipAsync(membership.Id, cancellationToken)
                .ConfigureAwait(false);
            foreach (var assignment in activeAssignments)
            {
                assignment.Revoke(actor, reason ?? "Membership revoked", _clock.UtcNow);
                await _assignments.UpdateAsync(assignment, cancellationToken).ConfigureAwait(false);
            }

            await _sessions
                .ClearSelectedOrganizationAsync(membership.UserId, membership.OrganizationId, cancellationToken)
                .ConfigureAwait(false);

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<OrganizationMembership>.Success(membership);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationMembership>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

