using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Authorization;

public sealed record PlatformRoleDefinitionDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    string Kind,
    string Status,
    IReadOnlyList<string> Permissions,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int Version);

public sealed record PlatformCustomRoleAssignmentDto(
    Guid Id,
    Guid PlatformUserId,
    Guid RoleDefinitionId,
    string Status,
    string GrantedByActor,
    DateTimeOffset GrantedAtUtc,
    string? Reason,
    string? RevokedByActor,
    DateTimeOffset? RevokedAtUtc,
    string? RevokeReason);

public sealed record OrganizationRoleDefinitionDto(
    Guid Id,
    Guid OrganizationId,
    string Code,
    string Name,
    string? Description,
    string Status,
    IReadOnlyList<string> Permissions,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    int Version);

public sealed record OrganizationCustomRoleAssignmentDto(
    Guid Id,
    Guid OrganizationId,
    Guid PlatformUserId,
    Guid RoleDefinitionId,
    string Status,
    string GrantedByActor,
    DateTimeOffset GrantedAtUtc,
    string? Reason,
    string? RevokedByActor,
    DateTimeOffset? RevokedAtUtc,
    string? RevokeReason);

public sealed record EffectivePlatformPermissionsDto(
    Guid PlatformUserId,
    IReadOnlyList<string> SystemRoles,
    IReadOnlyList<string> CustomRoles,
    IReadOnlyList<string> Permissions);

public sealed record EffectiveOrganizationPermissionsDto(
    Guid OrganizationId,
    Guid PlatformUserId,
    string? MembershipRole,
    string? MembershipStatus,
    IReadOnlyList<string> CustomRoles,
    IReadOnlyList<string> Permissions);

public sealed record PermissionCatalogEntryDto(string Code, string Description, string Area);

public static class RbacDtoMaps
{
    public static PlatformRoleDefinitionDto Map(PlatformRoleDefinition d) =>
        new(
            d.Id.Value,
            d.Code,
            d.Name,
            d.Description,
            d.Kind.ToString(),
            d.Status.ToString(),
            d.Permissions.OrderBy(p => p, StringComparer.Ordinal).ToList(),
            d.CreatedAtUtc,
            d.UpdatedAtUtc,
            d.Version);

    public static PlatformCustomRoleAssignmentDto Map(PlatformCustomRoleAssignment a) =>
        new(
            a.Id.Value,
            a.PlatformUserId.Value,
            a.RoleDefinitionId.Value,
            a.Status.ToString(),
            a.GrantedByActor,
            a.GrantedAtUtc,
            a.Reason,
            a.RevokedByActor,
            a.RevokedAtUtc,
            a.RevokeReason);

    public static OrganizationRoleDefinitionDto Map(OrganizationRoleDefinition d) =>
        new(
            d.Id.Value,
            d.OrganizationId.Value,
            d.Code,
            d.Name,
            d.Description,
            d.Status.ToString(),
            d.Permissions.OrderBy(p => p, StringComparer.Ordinal).ToList(),
            d.CreatedAtUtc,
            d.UpdatedAtUtc,
            d.Version);

    public static OrganizationCustomRoleAssignmentDto Map(OrganizationCustomRoleAssignment a) =>
        new(
            a.Id.Value,
            a.OrganizationId.Value,
            a.PlatformUserId.Value,
            a.RoleDefinitionId.Value,
            a.Status.ToString(),
            a.GrantedByActor,
            a.GrantedAtUtc,
            a.Reason,
            a.RevokedByActor,
            a.RevokedAtUtc,
            a.RevokeReason);
}

public sealed class PlatformRoleDefinitionQueryService
{
    private readonly IPlatformRoleDefinitionRepository _definitions;

    public PlatformRoleDefinitionQueryService(IPlatformRoleDefinitionRepository definitions) =>
        _definitions = definitions;

    public async Task<PlatformRoleDefinitionDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _definitions.GetByIdAsync(PlatformRoleDefinitionId.From(id), ct).ConfigureAwait(false);
        return item is null ? null : RbacDtoMaps.Map(item);
    }

    public async Task<PagedResult<PlatformRoleDefinitionDto>> ListAsync(
        PlatformRoleKind? kind,
        PlatformRoleLifecycleStatus? status,
        string? search,
        int? page,
        int? pageSize,
        CancellationToken ct = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, total) = await _definitions.ListAsync(kind, status, search, skip, take, ct).ConfigureAwait(false);
        return new PagedResult<PlatformRoleDefinitionDto>(
            items.Select(RbacDtoMaps.Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }
}

public sealed class CreatePlatformRoleDefinition
{
    private readonly IPlatformRoleDefinitionRepository _definitions;
    private readonly IAuditWriter _audit;
    private readonly IPlatformUnitOfWork _uow;
    private readonly IClock _clock;

    public CreatePlatformRoleDefinition(
        IPlatformRoleDefinitionRepository definitions,
        IAuditWriter audit,
        IPlatformUnitOfWork uow,
        IClock clock)
    {
        _definitions = definitions;
        _audit = audit;
        _uow = uow;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformRoleDefinition>> ExecuteAsync(
        string code,
        string name,
        string? description,
        IReadOnlyList<string> permissions,
        string actorIdentifier,
        AuditActorType actorType = AuditActorType.DevelopmentOperator,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        try
        {
            var existing = await _definitions.GetByCodeAsync(code.Trim(), ct).ConfigureAwait(false);
            if (existing is not null)
            {
                return ApplicationResult<PlatformRoleDefinition>.Failure(
                    ApplicationErrorCodes.RoleDefinitionConflict,
                    "A platform role definition with this code already exists.");
            }

            var definition = PlatformRoleDefinition.CreateCustom(code, name, description, permissions, _clock.UtcNow);
            await _definitions.AddAsync(definition, ct).ConfigureAwait(false);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
            await _audit.WriteAsync(
                actorIdentifier,
                actorType,
                PlatformAuditActions.PlatformRoleDefinitionCreated,
                nameof(PlatformRoleDefinition),
                definition.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                null,
                correlationId: correlationId,
                summary: $"Created custom platform role {definition.Code}.",
                cancellationToken: ct).ConfigureAwait(false);
            return ApplicationResult<PlatformRoleDefinition>.Success(definition);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformRoleDefinition>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PlatformRoleDefinition>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdatePlatformRoleDefinition
{
    private readonly IPlatformRoleDefinitionRepository _definitions;
    private readonly IAuditWriter _audit;
    private readonly IPlatformUnitOfWork _uow;
    private readonly IClock _clock;

    public UpdatePlatformRoleDefinition(
        IPlatformRoleDefinitionRepository definitions,
        IAuditWriter audit,
        IPlatformUnitOfWork uow,
        IClock clock)
    {
        _definitions = definitions;
        _audit = audit;
        _uow = uow;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformRoleDefinition>> ExecuteAsync(
        Guid id,
        string name,
        string? description,
        IReadOnlyList<string>? permissions,
        int? expectedVersion,
        string actorIdentifier,
        AuditActorType actorType = AuditActorType.DevelopmentOperator,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        var definition = await _definitions.GetByIdAsync(PlatformRoleDefinitionId.From(id), ct).ConfigureAwait(false);
        if (definition is null)
        {
            return ApplicationResult<PlatformRoleDefinition>.Failure(
                ApplicationErrorCodes.RoleDefinitionNotFound,
                "Platform role definition was not found.");
        }

        try
        {
            definition.UpdateDetails(name, description, permissions, _clock.UtcNow);
            await _definitions.UpdateAsync(definition, expectedVersion, ct).ConfigureAwait(false);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
            await _audit.WriteAsync(
                actorIdentifier,
                actorType,
                PlatformAuditActions.PlatformRoleDefinitionUpdated,
                nameof(PlatformRoleDefinition),
                definition.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                null,
                correlationId: correlationId,
                summary: $"Updated platform role {definition.Code}.",
                cancellationToken: ct).ConfigureAwait(false);
            return ApplicationResult<PlatformRoleDefinition>.Success(definition);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformRoleDefinition>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PlatformRoleDefinition>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ChangePlatformRoleDefinitionStatus
{
    private readonly IPlatformRoleDefinitionRepository _definitions;
    private readonly IAuditWriter _audit;
    private readonly IPlatformUnitOfWork _uow;
    private readonly IClock _clock;

    public ChangePlatformRoleDefinitionStatus(
        IPlatformRoleDefinitionRepository definitions,
        IAuditWriter audit,
        IPlatformUnitOfWork uow,
        IClock clock)
    {
        _definitions = definitions;
        _audit = audit;
        _uow = uow;
        _clock = clock;
    }

    public Task<ApplicationResult<PlatformRoleDefinition>> ActivateAsync(
        Guid id,
        int? expectedVersion,
        string actorIdentifier,
        AuditActorType actorType = AuditActorType.DevelopmentOperator,
        string? correlationId = null,
        CancellationToken ct = default) =>
        ExecuteAsync(id, expectedVersion, d => d.Activate(_clock.UtcNow), PlatformAuditActions.PlatformRoleDefinitionActivated, actorIdentifier, actorType, correlationId, ct);

    public Task<ApplicationResult<PlatformRoleDefinition>> DeactivateAsync(
        Guid id,
        int? expectedVersion,
        string actorIdentifier,
        AuditActorType actorType = AuditActorType.DevelopmentOperator,
        string? correlationId = null,
        CancellationToken ct = default) =>
        ExecuteAsync(id, expectedVersion, d => d.Deactivate(_clock.UtcNow), PlatformAuditActions.PlatformRoleDefinitionDeactivated, actorIdentifier, actorType, correlationId, ct);

    public Task<ApplicationResult<PlatformRoleDefinition>> RetireAsync(
        Guid id,
        int? expectedVersion,
        string actorIdentifier,
        AuditActorType actorType = AuditActorType.DevelopmentOperator,
        string? correlationId = null,
        CancellationToken ct = default) =>
        ExecuteAsync(id, expectedVersion, d => d.Retire(_clock.UtcNow), PlatformAuditActions.PlatformRoleDefinitionRetired, actorIdentifier, actorType, correlationId, ct);

    private async Task<ApplicationResult<PlatformRoleDefinition>> ExecuteAsync(
        Guid id,
        int? expectedVersion,
        Action<PlatformRoleDefinition> mutate,
        string auditAction,
        string actorIdentifier,
        AuditActorType actorType,
        string? correlationId,
        CancellationToken ct)
    {
        var definition = await _definitions.GetByIdAsync(PlatformRoleDefinitionId.From(id), ct).ConfigureAwait(false);
        if (definition is null)
        {
            return ApplicationResult<PlatformRoleDefinition>.Failure(
                ApplicationErrorCodes.RoleDefinitionNotFound,
                "Platform role definition was not found.");
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
                nameof(PlatformRoleDefinition),
                definition.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                null,
                correlationId: correlationId,
                summary: $"Changed platform role {definition.Code} to {definition.Status}.",
                cancellationToken: ct).ConfigureAwait(false);
            return ApplicationResult<PlatformRoleDefinition>.Success(definition);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformRoleDefinition>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PlatformRoleDefinition>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class EnsureBuiltInPlatformRoleDefinitions
{
    private readonly IPlatformRoleDefinitionRepository _definitions;
    private readonly IPlatformUnitOfWork _uow;
    private readonly IClock _clock;

    public EnsureBuiltInPlatformRoleDefinitions(
        IPlatformRoleDefinitionRepository definitions,
        IPlatformUnitOfWork uow,
        IClock clock)
    {
        _definitions = definitions;
        _uow = uow;
        _clock = clock;
    }

    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var added = false;
        foreach (var builtIn in BuiltInPlatformRoleDefinitions.All)
        {
            var id = PlatformRoleDefinitionId.From(builtIn.Id);
            if (await _definitions.GetByIdAsync(id, ct).ConfigureAwait(false) is not null)
            {
                continue;
            }

            var definition = PlatformRoleDefinition.CreateBuiltIn(
                id,
                builtIn.Code,
                builtIn.Name,
                builtIn.Description,
                PlatformRolePermissionCatalog.GetPermissions(builtIn.SystemRole),
                _clock.UtcNow);
            await _definitions.AddAsync(definition, ct).ConfigureAwait(false);
            added = true;
        }

        if (added)
        {
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }
}

public sealed class AssignPlatformCustomRole
{
    private readonly IPlatformCustomRoleAssignmentRepository _assignments;
    private readonly IPlatformRoleDefinitionRepository _definitions;
    private readonly IPlatformUserRepository _users;
    private readonly IAuditWriter _audit;
    private readonly IPlatformUnitOfWork _uow;
    private readonly IClock _clock;

    public AssignPlatformCustomRole(
        IPlatformCustomRoleAssignmentRepository assignments,
        IPlatformRoleDefinitionRepository definitions,
        IPlatformUserRepository users,
        IAuditWriter audit,
        IPlatformUnitOfWork uow,
        IClock clock)
    {
        _assignments = assignments;
        _definitions = definitions;
        _users = users;
        _audit = audit;
        _uow = uow;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformCustomRoleAssignment>> ExecuteAsync(
        Guid platformUserId,
        Guid roleDefinitionId,
        string actorIdentifier,
        AuditActorType actorType = AuditActorType.DevelopmentOperator,
        string? reason = null,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        var userId = PlatformUserId.From(platformUserId);
        var definitionId = PlatformRoleDefinitionId.From(roleDefinitionId);

        if (await _users.GetByIdAsync(userId, ct).ConfigureAwait(false) is null)
        {
            return ApplicationResult<PlatformCustomRoleAssignment>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "Platform User was not found.");
        }

        var definition = await _definitions.GetByIdAsync(definitionId, ct).ConfigureAwait(false);
        if (definition is null)
        {
            return ApplicationResult<PlatformCustomRoleAssignment>.Failure(
                ApplicationErrorCodes.RoleDefinitionNotFound,
                "Platform role definition was not found.");
        }

        if (definition.Kind != PlatformRoleKind.Custom)
        {
            return ApplicationResult<PlatformCustomRoleAssignment>.Failure(
                DomainErrorCodes.BuiltInRoleProtected,
                "Built-in platform roles must be assigned through system role assignments.");
        }

        if (!definition.IsAssignable)
        {
            return ApplicationResult<PlatformCustomRoleAssignment>.Failure(
                DomainErrorCodes.RoleDefinitionNotAssignable,
                "Inactive or retired roles cannot receive new assignments.");
        }

        if (await _assignments.FindActiveAsync(userId, definitionId, ct).ConfigureAwait(false) is not null)
        {
            return ApplicationResult<PlatformCustomRoleAssignment>.Failure(
                ApplicationErrorCodes.CustomRoleAssignmentConflict,
                "An active custom role assignment already exists for this user and role.");
        }

        try
        {
            var assignment = PlatformCustomRoleAssignment.Grant(userId, definitionId, actorIdentifier, _clock.UtcNow, reason);
            await _assignments.AddAsync(assignment, ct).ConfigureAwait(false);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
            await _audit.WriteAsync(
                actorIdentifier,
                actorType,
                PlatformAuditActions.PlatformCustomRoleAssigned,
                nameof(PlatformCustomRoleAssignment),
                assignment.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                null,
                correlationId: correlationId,
                reason: reason,
                summary: $"Granted custom role {definition.Code} to Platform User {userId.Value:D}.",
                cancellationToken: ct).ConfigureAwait(false);
            return ApplicationResult<PlatformCustomRoleAssignment>.Success(assignment);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformCustomRoleAssignment>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PlatformCustomRoleAssignment>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class RevokePlatformCustomRole
{
    private readonly IPlatformCustomRoleAssignmentRepository _assignments;
    private readonly IAuditWriter _audit;
    private readonly IPlatformUnitOfWork _uow;
    private readonly IClock _clock;

    public RevokePlatformCustomRole(
        IPlatformCustomRoleAssignmentRepository assignments,
        IAuditWriter audit,
        IPlatformUnitOfWork uow,
        IClock clock)
    {
        _assignments = assignments;
        _audit = audit;
        _uow = uow;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformCustomRoleAssignment>> ExecuteAsync(
        Guid assignmentId,
        string actorIdentifier,
        AuditActorType actorType = AuditActorType.DevelopmentOperator,
        string? reason = null,
        string? correlationId = null,
        CancellationToken ct = default)
    {
        var assignment = await _assignments
            .GetByIdAsync(PlatformCustomRoleAssignmentId.From(assignmentId), ct)
            .ConfigureAwait(false);
        if (assignment is null)
        {
            return ApplicationResult<PlatformCustomRoleAssignment>.Failure(
                ApplicationErrorCodes.CustomRoleAssignmentNotFound,
                "Platform custom role assignment was not found.");
        }

        try
        {
            assignment.Revoke(actorIdentifier, reason, _clock.UtcNow);
            await _assignments.UpdateAsync(assignment, ct).ConfigureAwait(false);
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
            await _audit.WriteAsync(
                actorIdentifier,
                actorType,
                PlatformAuditActions.PlatformCustomRoleRevoked,
                nameof(PlatformCustomRoleAssignment),
                assignment.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                null,
                correlationId: correlationId,
                reason: reason,
                summary: $"Revoked custom role assignment {assignment.Id.Value:D}.",
                cancellationToken: ct).ConfigureAwait(false);
            return ApplicationResult<PlatformCustomRoleAssignment>.Success(assignment);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformCustomRoleAssignment>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (PersistenceConflictException ex)
        {
            return ApplicationResult<PlatformCustomRoleAssignment>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ListPlatformCustomRoleAssignments
{
    private readonly IPlatformCustomRoleAssignmentRepository _assignments;

    public ListPlatformCustomRoleAssignments(IPlatformCustomRoleAssignmentRepository assignments) =>
        _assignments = assignments;

    public async Task<PagedResult<PlatformCustomRoleAssignmentDto>> ExecuteAsync(
        Guid? platformUserId,
        Guid? roleDefinitionId,
        PlatformRoleAssignmentStatus? status,
        int? page,
        int? pageSize,
        CancellationToken ct = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var userId = platformUserId.HasValue ? PlatformUserId.From(platformUserId.Value) : null;
        var roleId = roleDefinitionId.HasValue ? PlatformRoleDefinitionId.From(roleDefinitionId.Value) : null;
        var (items, total) = await _assignments.ListAsync(userId, roleId, status, skip, take, ct).ConfigureAwait(false);
        return new PagedResult<PlatformCustomRoleAssignmentDto>(
            items.Select(RbacDtoMaps.Map).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }
}

public sealed class ResolveEffectivePlatformPermissions
{
    private readonly IPlatformAuthorizationService _authorization;
    private readonly IPlatformRoleAssignmentRepository _systemAssignments;
    private readonly IPlatformCustomRoleAssignmentRepository _customAssignments;
    private readonly IPlatformRoleDefinitionRepository _definitions;

    public ResolveEffectivePlatformPermissions(
        IPlatformAuthorizationService authorization,
        IPlatformRoleAssignmentRepository systemAssignments,
        IPlatformCustomRoleAssignmentRepository customAssignments,
        IPlatformRoleDefinitionRepository definitions)
    {
        _authorization = authorization;
        _systemAssignments = systemAssignments;
        _customAssignments = customAssignments;
        _definitions = definitions;
    }

    public async Task<EffectivePlatformPermissionsDto> ExecuteAsync(
        Guid platformUserId,
        Guid? organizationId = null,
        CancellationToken ct = default)
    {
        var userId = PlatformUserId.From(platformUserId);
        var orgId = organizationId.HasValue ? PlatformOrganizationId.From(organizationId.Value) : null;
        var permissions = await _authorization.ResolvePermissionsAsync(userId, orgId, ct).ConfigureAwait(false);

        var systemRoles = (await _systemAssignments.ListActiveByUserAsync(userId, ct).ConfigureAwait(false))
            .Where(a => a.OrganizationId is null || (orgId is not null && a.OrganizationId == orgId))
            .Select(a => a.Role.ToString())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(r => r, StringComparer.Ordinal)
            .ToList();

        var customRoles = new List<string>();
        foreach (var assignment in await _customAssignments.ListActiveByUserAsync(userId, ct).ConfigureAwait(false))
        {
            var definition = await _definitions.GetByIdAsync(assignment.RoleDefinitionId, ct).ConfigureAwait(false);
            if (definition is { Status: PlatformRoleLifecycleStatus.Active })
            {
                customRoles.Add(definition.Code);
            }
        }

        return new EffectivePlatformPermissionsDto(
            platformUserId,
            systemRoles,
            customRoles.Distinct(StringComparer.Ordinal).OrderBy(r => r, StringComparer.Ordinal).ToList(),
            permissions.OrderBy(p => p, StringComparer.Ordinal).ToList());
    }
}
