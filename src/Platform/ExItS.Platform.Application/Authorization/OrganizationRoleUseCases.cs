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

public sealed class OrganizationRoleDefinitionQueryService
{
    private readonly IOrganizationRoleDefinitionRepository _definitions;

    public OrganizationRoleDefinitionQueryService(IOrganizationRoleDefinitionRepository definitions) =>
        _definitions = definitions;

    public async Task<OrganizationRoleDefinitionDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _definitions.GetByIdAsync(OrganizationRoleDefinitionId.From(id), ct).ConfigureAwait(false);
        return item is null ? null : RbacDtoMaps.Map(item);
    }

    public async Task<PagedResult<OrganizationRoleDefinitionDto>> ListAsync(
        Guid organizationId,
        PlatformRoleLifecycleStatus? status,
        string? search,
        int? page,
        int? pageSize,
        CancellationToken ct = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var orgId = PlatformOrganizationId.From(organizationId);
        var (items, total) = await _definitions
            .ListByOrganizationAsync(orgId, status, search, skip, take, ct)
            .ConfigureAwait(false);
        return new PagedResult<OrganizationRoleDefinitionDto>(
            items.Select(RbacDtoMaps.Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }
}

public sealed class CreateOrganizationRoleDefinition
{
    private readonly IOrganizationRoleDefinitionRepository _definitions;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IAuditWriter _audit;
    private readonly IPlatformUnitOfWork _uow;
    private readonly IClock _clock;

    public CreateOrganizationRoleDefinition(
        IOrganizationRoleDefinitionRepository definitions,
        IPlatformOrganizationRepository organizations,
        IAuditWriter audit,
        IPlatformUnitOfWork uow,
        IClock clock)
    {
        _definitions = definitions;
        _organizations = organizations;
        _audit = audit;
        _uow = uow;
        _clock = clock;
    }

    public async Task<ApplicationResult<OrganizationRoleDefinition>> ExecuteAsync(
        Guid organizationId,
        string code,
        string name,
        string? description,
        IReadOnlyList<string> permissions,
        string actorIdentifier,
        AuditActorType actorType = AuditActorType.DevelopmentOperator,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        var orgId = PlatformOrganizationId.From(organizationId);
        if (await _organizations.GetByIdAsync(orgId, ct).ConfigureAwait(false) is null)
        {
            return ApplicationResult<OrganizationRoleDefinition>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Platform Organization was not found.");
        }

        if (await _definitions.GetByOrgAndCodeAsync(orgId, code.Trim(), ct).ConfigureAwait(false) is not null)
        {
            return ApplicationResult<OrganizationRoleDefinition>.Failure(
                ApplicationErrorCodes.OrganizationRoleDefinitionConflict,
                "An organization role with this code already exists.");
        }

        try
        {
            var definition = OrganizationRoleDefinition.Create(orgId, code, name, description, permissions, _clock.UtcNow);
            await _definitions.AddAsync(definition, ct).ConfigureAwait(false);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
            await _audit.WriteAsync(
                actorIdentifier,
                actorType,
                PlatformAuditActions.OrganizationRoleDefinitionCreated,
                nameof(OrganizationRoleDefinition),
                definition.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                orgId,
                correlationId: correlationId,
                summary: $"Created organization role {definition.Code}.",
                cancellationToken: ct).ConfigureAwait(false);
            return ApplicationResult<OrganizationRoleDefinition>.Success(definition);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationRoleDefinition>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<OrganizationRoleDefinition>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdateOrganizationRoleDefinition
{
    private readonly IOrganizationRoleDefinitionRepository _definitions;
    private readonly IAuditWriter _audit;
    private readonly IPlatformUnitOfWork _uow;
    private readonly IClock _clock;

    public UpdateOrganizationRoleDefinition(
        IOrganizationRoleDefinitionRepository definitions,
        IAuditWriter audit,
        IPlatformUnitOfWork uow,
        IClock clock)
    {
        _definitions = definitions;
        _audit = audit;
        _uow = uow;
        _clock = clock;
    }

    public async Task<ApplicationResult<OrganizationRoleDefinition>> ExecuteAsync(
        Guid organizationId,
        Guid roleId,
        string name,
        string? description,
        IReadOnlyList<string>? permissions,
        int? expectedVersion,
        string actorIdentifier,
        AuditActorType actorType = AuditActorType.DevelopmentOperator,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        var definition = await _definitions.GetByIdAsync(OrganizationRoleDefinitionId.From(roleId), ct).ConfigureAwait(false);
        if (definition is null || definition.OrganizationId.Value != organizationId)
        {
            return ApplicationResult<OrganizationRoleDefinition>.Failure(
                ApplicationErrorCodes.OrganizationRoleDefinitionNotFound,
                "Organization role definition was not found.");
        }

        try
        {
            definition.UpdateDetails(name, description, permissions, _clock.UtcNow);
            await _definitions.UpdateAsync(definition, expectedVersion, ct).ConfigureAwait(false);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
            await _audit.WriteAsync(
                actorIdentifier,
                actorType,
                PlatformAuditActions.OrganizationRoleDefinitionUpdated,
                nameof(OrganizationRoleDefinition),
                definition.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                definition.OrganizationId,
                correlationId: correlationId,
                summary: $"Updated organization role {definition.Code}.",
                cancellationToken: ct).ConfigureAwait(false);
            return ApplicationResult<OrganizationRoleDefinition>.Success(definition);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationRoleDefinition>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<OrganizationRoleDefinition>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ChangeOrganizationRoleDefinitionStatus
{
    private readonly IOrganizationRoleDefinitionRepository _definitions;
    private readonly IAuditWriter _audit;
    private readonly IPlatformUnitOfWork _uow;
    private readonly IClock _clock;

    public ChangeOrganizationRoleDefinitionStatus(
        IOrganizationRoleDefinitionRepository definitions,
        IAuditWriter audit,
        IPlatformUnitOfWork uow,
        IClock clock)
    {
        _definitions = definitions;
        _audit = audit;
        _uow = uow;
        _clock = clock;
    }

    public Task<ApplicationResult<OrganizationRoleDefinition>> ActivateAsync(
        Guid organizationId,
        Guid roleId,
        int? expectedVersion,
        string actorIdentifier,
        AuditActorType actorType = AuditActorType.DevelopmentOperator,
        string? correlationId = null,
        CancellationToken ct = default) =>
        ExecuteAsync(organizationId, roleId, expectedVersion, d => d.Activate(_clock.UtcNow), PlatformAuditActions.OrganizationRoleDefinitionActivated, actorIdentifier, actorType, correlationId, ct);

    public Task<ApplicationResult<OrganizationRoleDefinition>> DeactivateAsync(
        Guid organizationId,
        Guid roleId,
        int? expectedVersion,
        string actorIdentifier,
        AuditActorType actorType = AuditActorType.DevelopmentOperator,
        string? correlationId = null,
        CancellationToken ct = default) =>
        ExecuteAsync(organizationId, roleId, expectedVersion, d => d.Deactivate(_clock.UtcNow), PlatformAuditActions.OrganizationRoleDefinitionDeactivated, actorIdentifier, actorType, correlationId, ct);

    public Task<ApplicationResult<OrganizationRoleDefinition>> RetireAsync(
        Guid organizationId,
        Guid roleId,
        int? expectedVersion,
        string actorIdentifier,
        AuditActorType actorType = AuditActorType.DevelopmentOperator,
        string? correlationId = null,
        CancellationToken ct = default) =>
        ExecuteAsync(organizationId, roleId, expectedVersion, d => d.Retire(_clock.UtcNow), PlatformAuditActions.OrganizationRoleDefinitionRetired, actorIdentifier, actorType, correlationId, ct);

    private async Task<ApplicationResult<OrganizationRoleDefinition>> ExecuteAsync(
        Guid organizationId,
        Guid roleId,
        int? expectedVersion,
        Action<OrganizationRoleDefinition> mutate,
        string auditAction,
        string actorIdentifier,
        AuditActorType actorType,
        string? correlationId,
        CancellationToken ct)
    {
        var definition = await _definitions.GetByIdAsync(OrganizationRoleDefinitionId.From(roleId), ct).ConfigureAwait(false);
        if (definition is null || definition.OrganizationId.Value != organizationId)
        {
            return ApplicationResult<OrganizationRoleDefinition>.Failure(
                ApplicationErrorCodes.OrganizationRoleDefinitionNotFound,
                "Organization role definition was not found.");
        }

        try
        {
            mutate(definition);
            await _definitions.UpdateAsync(definition, expectedVersion, ct).ConfigureAwait(false);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
            await _audit.WriteAsync(
                actorIdentifier,
                actorType,
                auditAction,
                nameof(OrganizationRoleDefinition),
                definition.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                definition.OrganizationId,
                correlationId: correlationId,
                summary: $"Changed organization role {definition.Code} to {definition.Status}.",
                cancellationToken: ct).ConfigureAwait(false);
            return ApplicationResult<OrganizationRoleDefinition>.Success(definition);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationRoleDefinition>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<OrganizationRoleDefinition>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class AssignOrganizationCustomRole
{
    private readonly IOrganizationCustomRoleAssignmentRepository _assignments;
    private readonly IOrganizationRoleDefinitionRepository _definitions;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IAuditWriter _audit;
    private readonly IPlatformUnitOfWork _uow;
    private readonly IClock _clock;

    public AssignOrganizationCustomRole(
        IOrganizationCustomRoleAssignmentRepository assignments,
        IOrganizationRoleDefinitionRepository definitions,
        IOrganizationMembershipRepository memberships,
        IAuditWriter audit,
        IPlatformUnitOfWork uow,
        IClock clock)
    {
        _assignments = assignments;
        _definitions = definitions;
        _memberships = memberships;
        _audit = audit;
        _uow = uow;
        _clock = clock;
    }

    public async Task<ApplicationResult<OrganizationCustomRoleAssignment>> ExecuteAsync(
        Guid organizationId,
        Guid platformUserId,
        Guid roleDefinitionId,
        string actorIdentifier,
        AuditActorType actorType = AuditActorType.DevelopmentOperator,
        string? reason = null,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        var orgId = PlatformOrganizationId.From(organizationId);
        var userId = PlatformUserId.From(platformUserId);
        var definitionId = OrganizationRoleDefinitionId.From(roleDefinitionId);

        var membership = await _memberships.FindActiveByUserAndOrganizationAsync(userId, orgId, ct).ConfigureAwait(false);
        if (membership is null)
        {
            return ApplicationResult<OrganizationCustomRoleAssignment>.Failure(
                ApplicationErrorCodes.MembershipNotFound,
                "An active organization membership is required for role assignment.");
        }

        var definition = await _definitions.GetByIdAsync(definitionId, ct).ConfigureAwait(false);
        if (definition is null || definition.OrganizationId != orgId)
        {
            return ApplicationResult<OrganizationCustomRoleAssignment>.Failure(
                ApplicationErrorCodes.OrganizationRoleDefinitionNotFound,
                "Organization role definition was not found.");
        }

        if (!definition.IsAssignable)
        {
            return ApplicationResult<OrganizationCustomRoleAssignment>.Failure(
                DomainErrorCodes.RoleDefinitionNotAssignable,
                "Inactive or retired roles cannot receive new assignments.");
        }

        if (await _assignments.FindActiveAsync(orgId, userId, definitionId, ct).ConfigureAwait(false) is not null)
        {
            return ApplicationResult<OrganizationCustomRoleAssignment>.Failure(
                ApplicationErrorCodes.OrganizationCustomRoleAssignmentConflict,
                "An active organization custom role assignment already exists.");
        }

        try
        {
            var assignment = OrganizationCustomRoleAssignment.Grant(
                orgId, userId, definitionId, actorIdentifier, _clock.UtcNow, reason);
            await _assignments.AddAsync(assignment, ct).ConfigureAwait(false);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
            await _audit.WriteAsync(
                actorIdentifier,
                actorType,
                PlatformAuditActions.OrganizationCustomRoleAssigned,
                nameof(OrganizationCustomRoleAssignment),
                assignment.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                orgId,
                correlationId: correlationId,
                reason: reason,
                summary: $"Granted organization role {definition.Code} to user {userId.Value:D}.",
                cancellationToken: ct).ConfigureAwait(false);
            return ApplicationResult<OrganizationCustomRoleAssignment>.Success(assignment);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationCustomRoleAssignment>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<OrganizationCustomRoleAssignment>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class RevokeOrganizationCustomRole
{
    private readonly IOrganizationCustomRoleAssignmentRepository _assignments;
    private readonly IAuditWriter _audit;
    private readonly IPlatformUnitOfWork _uow;
    private readonly IClock _clock;

    public RevokeOrganizationCustomRole(
        IOrganizationCustomRoleAssignmentRepository assignments,
        IAuditWriter audit,
        IPlatformUnitOfWork uow,
        IClock clock)
    {
        _assignments = assignments;
        _audit = audit;
        _uow = uow;
        _clock = clock;
    }

    public async Task<ApplicationResult<OrganizationCustomRoleAssignment>> ExecuteAsync(
        Guid organizationId,
        Guid assignmentId,
        string actorIdentifier,
        AuditActorType actorType = AuditActorType.DevelopmentOperator,
        string? reason = null,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        var assignment = await _assignments
            .GetByIdAsync(OrganizationCustomRoleAssignmentId.From(assignmentId), ct)
            .ConfigureAwait(false);
        if (assignment is null || assignment.OrganizationId.Value != organizationId)
        {
            return ApplicationResult<OrganizationCustomRoleAssignment>.Failure(
                ApplicationErrorCodes.OrganizationCustomRoleAssignmentNotFound,
                "Organization custom role assignment was not found.");
        }

        try
        {
            assignment.Revoke(actorIdentifier, reason, _clock.UtcNow);
            await _assignments.UpdateAsync(assignment, ct).ConfigureAwait(false);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
            await _audit.WriteAsync(
                actorIdentifier,
                actorType,
                PlatformAuditActions.OrganizationCustomRoleRevoked,
                nameof(OrganizationCustomRoleAssignment),
                assignment.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                assignment.OrganizationId,
                correlationId: correlationId,
                reason: reason,
                summary: $"Revoked organization custom role assignment {assignment.Id.Value:D}.",
                cancellationToken: ct).ConfigureAwait(false);
            return ApplicationResult<OrganizationCustomRoleAssignment>.Success(assignment);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationCustomRoleAssignment>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<OrganizationCustomRoleAssignment>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ListOrganizationCustomRoleAssignments
{
    private readonly IOrganizationCustomRoleAssignmentRepository _assignments;

    public ListOrganizationCustomRoleAssignments(IOrganizationCustomRoleAssignmentRepository assignments) =>
        _assignments = assignments;

    public async Task<PagedResult<OrganizationCustomRoleAssignmentDto>> ExecuteAsync(
        Guid organizationId,
        Guid? platformUserId,
        Guid? roleDefinitionId,
        PlatformRoleAssignmentStatus? status,
        int? page,
        int? pageSize,
        CancellationToken ct = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var orgId = PlatformOrganizationId.From(organizationId);
        var userId = platformUserId.HasValue ? PlatformUserId.From(platformUserId.Value) : null;
        var roleId = roleDefinitionId.HasValue ? OrganizationRoleDefinitionId.From(roleDefinitionId.Value) : null;
        var (items, total) = await _assignments.ListAsync(orgId, userId, roleId, status, skip, take, ct).ConfigureAwait(false);
        return new PagedResult<OrganizationCustomRoleAssignmentDto>(
            items.Select(RbacDtoMaps.Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }
}

public sealed class ResolveEffectiveOrganizationPermissions
{
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IOrganizationCustomRoleAssignmentRepository _assignments;
    private readonly IOrganizationRoleDefinitionRepository _definitions;

    public ResolveEffectiveOrganizationPermissions(
        IOrganizationMembershipRepository memberships,
        IOrganizationCustomRoleAssignmentRepository assignments,
        IOrganizationRoleDefinitionRepository definitions)
    {
        _memberships = memberships;
        _assignments = assignments;
        _definitions = definitions;
    }

    public async Task<EffectiveOrganizationPermissionsDto> ExecuteAsync(
        Guid organizationId,
        Guid platformUserId,
        CancellationToken ct = default)
    {
        var orgId = PlatformOrganizationId.From(organizationId);
        var userId = PlatformUserId.From(platformUserId);
        var permissions = new HashSet<string>(StringComparer.Ordinal);
        string? membershipRole = null;
        string? membershipStatus = null;

        var membership = await _memberships.FindCurrentByUserAndOrganizationAsync(userId, orgId, ct).ConfigureAwait(false);
        if (membership is not null)
        {
            membershipRole = membership.Role.ToString();
            membershipStatus = membership.Status.ToString();
            if (membership.Status == MembershipStatus.Active)
            {
                foreach (var permission in OrganizationRolePermissionCatalog.GetPermissions(membership.Role))
                {
                    permissions.Add(permission);
                }
            }
        }

        var customRoles = new List<string>();
        foreach (var assignment in await _assignments.ListActiveByUserAsync(orgId, userId, ct).ConfigureAwait(false))
        {
            var definition = await _definitions.GetByIdAsync(assignment.RoleDefinitionId, ct).ConfigureAwait(false);
            if (definition is { Status: PlatformRoleLifecycleStatus.Active })
            {
                customRoles.Add(definition.Code);
                foreach (var permission in definition.Permissions)
                {
                    permissions.Add(permission);
                }
            }
        }

        return new EffectiveOrganizationPermissionsDto(
            organizationId,
            platformUserId,
            membershipRole,
            membershipStatus,
            customRoles.Distinct(StringComparer.Ordinal).OrderBy(r => r, StringComparer.Ordinal).ToList(),
            permissions.OrderBy(p => p, StringComparer.Ordinal).ToList());
    }
}
