using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Authorization;

public sealed record PlatformRoleAssignmentDto(
    Guid Id,
    Guid PlatformUserId,
    string Role,
    Guid? OrganizationId,
    string Status,
    string GrantedByActor,
    DateTimeOffset GrantedAtUtc,
    string? Reason,
    string? RevokedByActor,
    DateTimeOffset? RevokedAtUtc,
    string? RevokeReason);

public sealed record ResolvedPermissionsDto(
    string ActorIdentifier,
    string ActorType,
    Guid? PlatformUserId,
    Guid? OrganizationId,
    IReadOnlyList<string> Permissions);

public sealed class ListPlatformRoles
{
    private readonly IPlatformRoleAssignmentRepository _assignments;

    public ListPlatformRoles(IPlatformRoleAssignmentRepository assignments) => _assignments = assignments;

    public async Task<PlatformRoleAssignmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var assignment = await _assignments
            .GetByIdAsync(PlatformRoleAssignmentId.From(id), cancellationToken)
            .ConfigureAwait(false);
        return assignment is null ? null : Map(assignment);
    }

    public async Task<PagedResult<PlatformRoleAssignmentDto>> ExecuteAsync(
        Guid? platformUserId,
        PlatformSystemRole? role,
        Guid? organizationId,
        PlatformRoleAssignmentStatus? status,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var userId = platformUserId.HasValue ? PlatformUserId.From(platformUserId.Value) : null;
        var orgId = organizationId.HasValue ? PlatformOrganizationId.From(organizationId.Value) : null;

        var (items, total) = await _assignments
            .ListAsync(userId, role, orgId, status, skip, take, cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<PlatformRoleAssignmentDto>(
            items.Select(Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public static PlatformRoleAssignmentDto Map(PlatformRoleAssignment assignment) =>
        new(
            assignment.Id.Value,
            assignment.PlatformUserId.Value,
            assignment.Role.ToString(),
            assignment.OrganizationId?.Value,
            assignment.Status.ToString(),
            assignment.GrantedByActor,
            assignment.GrantedAtUtc,
            assignment.Reason,
            assignment.RevokedByActor,
            assignment.RevokedAtUtc,
            assignment.RevokeReason);
}

public sealed class AssignPlatformRole
{
    private readonly IPlatformRoleAssignmentRepository _assignments;
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly EnsureAccountProfilesForUser _ensureProfiles;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AssignPlatformRole(
        IPlatformRoleAssignmentRepository assignments,
        IPlatformUserRepository users,
        IPlatformOrganizationRepository organizations,
        EnsureAccountProfilesForUser ensureProfiles,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _assignments = assignments;
        _users = users;
        _organizations = organizations;
        _ensureProfiles = ensureProfiles;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformRoleAssignment>> ExecuteAsync(
        Guid platformUserId,
        PlatformSystemRole role,
        Guid? organizationId,
        string actorIdentifier,
        AuditActorType actorType = AuditActorType.DevelopmentOperator,
        string? reason = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var userId = PlatformUserId.From(platformUserId);
        var user = await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PlatformRoleAssignment>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "Platform User was not found.");
        }

        PlatformOrganizationId? orgId = null;
        if (organizationId is not null)
        {
            orgId = PlatformOrganizationId.From(organizationId.Value);
            var organization = await _organizations.GetByIdAsync(orgId, cancellationToken).ConfigureAwait(false);
            if (organization is null)
            {
                return ApplicationResult<PlatformRoleAssignment>.Failure(
                    ApplicationErrorCodes.OrganizationNotFound,
                    "Platform Organization was not found.");
            }
        }

        var existing = await _assignments
            .FindActiveAsync(userId, role, orgId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return ApplicationResult<PlatformRoleAssignment>.Failure(
                ApplicationErrorCodes.RoleAssignmentConflict,
                "An active assignment for this Platform User, role, and organization scope already exists.");
        }

        try
        {
            var assignment = PlatformRoleAssignment.Grant(userId, role, orgId, actorIdentifier, _clock.UtcNow, reason);
            await _assignments.AddAsync(assignment, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            if (orgId is null)
            {
                await _ensureProfiles
                    .ExecuteAsync(userId, AccountClass.Platform, exclusivePreferredClass: false, cancellationToken)
                    .ConfigureAwait(false);
            }

            await _auditWriter.WriteAsync(
                actorIdentifier,
                actorType,
                PlatformAuditActions.PlatformRoleAssigned,
                nameof(PlatformRoleAssignment),
                assignment.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                orgId,
                correlationId: correlationId,
                reason: reason,
                summary: $"Granted {role} to Platform User {userId.Value:D}" +
                    (orgId is null ? " (platform-wide)." : $" scoped to organization {orgId.Value:D}."),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PlatformRoleAssignment>.Success(assignment);
        }
        catch (DomainException ex)
        {
            await _auditWriter.WriteAsync(
                actorIdentifier,
                actorType,
                PlatformAuditActions.PlatformRoleAssigned,
                nameof(PlatformRoleAssignment),
                platformUserId.ToString("D"),
                AuditOutcome.Failed,
                orgId,
                correlationId: correlationId,
                reason: ex.Message,
                cancellationToken: cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PlatformRoleAssignment>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PlatformRoleAssignment>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class RevokePlatformRole
{
    private readonly IPlatformRoleAssignmentRepository _assignments;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RevokePlatformRole(
        IPlatformRoleAssignmentRepository assignments,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _assignments = assignments;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformRoleAssignment>> ExecuteAsync(
        Guid roleAssignmentId,
        string actorIdentifier,
        AuditActorType actorType = AuditActorType.DevelopmentOperator,
        string? reason = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        var id = PlatformRoleAssignmentId.From(roleAssignmentId);
        var assignment = await _assignments.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (assignment is null)
        {
            return ApplicationResult<PlatformRoleAssignment>.Failure(
                ApplicationErrorCodes.RoleAssignmentNotFound,
                "Platform role assignment was not found.");
        }

        if (assignment.Status == PlatformRoleAssignmentStatus.Active
            && assignment.Role == PlatformSystemRole.PlatformAdministrator
            && assignment.OrganizationId is null)
        {
            var remaining = await _assignments.CountActivePlatformAdministratorsAsync(cancellationToken).ConfigureAwait(false);
            if (remaining <= 1)
            {
                return ApplicationResult<PlatformRoleAssignment>.Failure(
                    ApplicationErrorCodes.LastPlatformAdministratorProtected,
                    "Cannot revoke the final active Platform Administrator assignment.");
            }
        }

        try
        {
            assignment.Revoke(actorIdentifier, reason, _clock.UtcNow);
            await _assignments.UpdateAsync(assignment, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                actorIdentifier,
                actorType,
                PlatformAuditActions.PlatformRoleRevoked,
                nameof(PlatformRoleAssignment),
                assignment.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                assignment.OrganizationId,
                correlationId: correlationId,
                reason: reason,
                summary: $"Revoked {assignment.Role} from Platform User {assignment.PlatformUserId.Value:D}.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PlatformRoleAssignment>.Success(assignment);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformRoleAssignment>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PlatformRoleAssignment>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ResolveCurrentPermissions
{
    private readonly IPlatformActorAccessor _actorAccessor;
    private readonly IPlatformAuthorizationService _authorizationService;

    public ResolveCurrentPermissions(
        IPlatformActorAccessor actorAccessor,
        IPlatformAuthorizationService authorizationService)
    {
        _actorAccessor = actorAccessor;
        _authorizationService = authorizationService;
    }

    public async Task<ResolvedPermissionsDto> ExecuteAsync(
        Guid? organizationId = null,
        CancellationToken cancellationToken = default)
    {
        var actor = _actorAccessor.GetCurrent();
        var orgId = organizationId.HasValue
            ? PlatformOrganizationId.From(organizationId.Value)
            : actor.OrganizationId;

        // Platform permission codes never apply to Organization or Personal sessions.
        if (actor.AccountClass is AccountClass.Organization or AccountClass.Personal)
        {
            return new ResolvedPermissionsDto(
                actor.ActorIdentifier,
                actor.ActorType.ToString(),
                actor.PlatformUserId?.Value,
                orgId?.Value,
                Array.Empty<string>());
        }

        var permissions = await _authorizationService
            .ResolvePermissionsForActorAsync(actor, orgId, cancellationToken)
            .ConfigureAwait(false);

        return new ResolvedPermissionsDto(
            actor.ActorIdentifier,
            actor.ActorType.ToString(),
            actor.PlatformUserId?.Value,
            orgId?.Value,
            permissions.OrderBy(p => p, StringComparer.Ordinal).ToList());
    }
}
