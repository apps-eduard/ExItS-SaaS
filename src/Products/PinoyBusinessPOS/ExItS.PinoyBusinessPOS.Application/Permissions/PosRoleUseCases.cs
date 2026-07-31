using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.Application.Permissions;

public sealed class PosRoleAssignmentQueryService(IPosRoleAssignmentRepository repository)
{
    public async Task<PosRoleAssignmentDto?> GetByIdAsync(
        Guid organizationId,
        Guid assignmentId,
        CancellationToken ct = default)
    {
        var org = PosOrganizationId.From(organizationId);
        var entity = await repository.GetByIdAsync(org, PosRoleAssignmentId.From(assignmentId), ct).ConfigureAwait(false);
        return entity is null ? null : PosRoleAssignmentMapping.Map(entity);
    }

    public async Task<PosRoleAssignmentListDto> ListAsync(
        Guid organizationId,
        string? status,
        Guid? actorId,
        string? role,
        int? page,
        int? pageSize,
        CancellationToken ct = default)
    {
        var org = PosOrganizationId.From(organizationId);
        PosRoleAssignmentStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!PosRoleAssignmentStatusCodes.TryParse(status, out var parsedStatus))
            {
                throw new DomainException(DomainErrorCodes.InvalidPosRole, "Invalid role assignment status filter.");
            }

            statusFilter = parsedStatus;
        }

        PosRole? roleFilter = null;
        if (!string.IsNullOrWhiteSpace(role))
        {
            if (!PosRoleCodes.TryParse(role, out var parsedRole))
            {
                throw new DomainException(DomainErrorCodes.InvalidPosRole, "Invalid role filter.");
            }

            roleFilter = parsedRole;
        }

        var p = page is null or < 1 ? 1 : page.Value;
        var ps = pageSize is null or < 1 ? 50 : Math.Min(pageSize.Value, 200);
        var (items, total) = await repository.ListAsync(org, statusFilter, actorId, roleFilter, p, ps, ct)
            .ConfigureAwait(false);
        return new PosRoleAssignmentListDto(items.Select(PosRoleAssignmentMapping.Map).ToList(), p, ps, total);
    }

    public async Task<PosEffectivePermissionsDto> GetEffectiveAsync(
        Guid organizationId,
        Guid actorId,
        CancellationToken ct = default)
    {
        var org = PosOrganizationId.From(organizationId);
        var active = await repository.GetActiveForActorAsync(org, actorId, ct).ConfigureAwait(false);
        var ownerCount = await repository.CountActiveOwnersAsync(org, ct).ConfigureAwait(false);
        var bootstrapEligible = ownerCount == 0;

        if (active is null)
        {
            return new PosEffectivePermissionsDto(
                organizationId,
                actorId,
                Role: null,
                RoleDisplayName: null,
                Status: "None",
                AllowedCapabilities: [],
                AllowedFeatureCodes: [],
                CanManageAssignments: bootstrapEligible,
                IsBootstrapEligible: bootstrapEligible);
        }

        return new PosEffectivePermissionsDto(
            organizationId,
            actorId,
            PosRoleCodes.ToCode(active.Role),
            PosRoleCodes.ToDisplayName(active.Role),
            PosRoleAssignmentStatusCodes.ToCode(active.Status),
            PosRoleMatrix.CapabilitiesFor(active.Role).Select(c => c.ToString()).ToArray(),
            PosRoleAssignmentMapping.FeatureCodesForRole(active.Role),
            PosRoleMatrix.CanManageAssignments(active.Role) || bootstrapEligible,
            bootstrapEligible);
    }

    public static IReadOnlyList<PosRoleDto> ListRoles() =>
    [
        new(PosRoleCodes.Owner, PosRoleCodes.ToDisplayName(PosRole.Owner)),
        new(PosRoleCodes.Admin, PosRoleCodes.ToDisplayName(PosRole.Admin)),
        new(PosRoleCodes.StoreManager, PosRoleCodes.ToDisplayName(PosRole.StoreManager)),
        new(PosRoleCodes.Cashier, PosRoleCodes.ToDisplayName(PosRole.Cashier)),
        new(PosRoleCodes.InventoryStaff, PosRoleCodes.ToDisplayName(PosRole.InventoryStaff)),
        new(PosRoleCodes.ReportingUser, PosRoleCodes.ToDisplayName(PosRole.ReportingUser))
    ];
}

public sealed class AssignPosRole(
    IPosRoleAssignmentRepository repository,
    IPosUnitOfWork unitOfWork,
    IClock clock)
{
    public Task<ApplicationResult<PosRoleAssignment>> ExecuteAsync(
        Guid organizationId,
        Guid actorId,
        string roleCode,
        Guid assignedBy,
        Guid? assignmentId = null,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteInSerializableTransactionAsync(
            ct2 => ExecuteCoreAsync(organizationId, actorId, roleCode, assignedBy, assignmentId, ct2),
            ct);

    private async Task<ApplicationResult<PosRoleAssignment>> ExecuteCoreAsync(
        Guid organizationId,
        Guid actorId,
        string roleCode,
        Guid assignedBy,
        Guid? assignmentId,
        CancellationToken ct)
    {
        if (!PosRoleCodes.TryParse(roleCode, out var role))
        {
            return ApplicationResult<PosRoleAssignment>.Failure(
                DomainErrorCodes.InvalidPosRole,
                "Unknown POS role.");
        }

        var org = PosOrganizationId.From(organizationId);
        await repository.AcquireOrganizationLockAsync(org, ct).ConfigureAwait(false);

        if (assignmentId is Guid requestedId)
        {
            var existingById = await repository.GetByIdAsync(org, PosRoleAssignmentId.From(requestedId), ct)
                .ConfigureAwait(false);
            if (existingById is not null)
            {
                if (existingById.ActorId == actorId
                    && existingById.Role == role
                    && existingById.Status == PosRoleAssignmentStatus.Active)
                {
                    return ApplicationResult<PosRoleAssignment>.Success(existingById);
                }

                return ApplicationResult<PosRoleAssignment>.Failure(
                    DomainErrorCodes.PosRoleAssignmentConflict,
                    "Idempotency key conflicts with an existing assignment payload.");
            }
        }

        var ownerCount = await repository.CountActiveOwnersAsync(org, ct).ConfigureAwait(false);
        var assignerActive = await repository.GetActiveForActorAsync(org, assignedBy, ct).ConfigureAwait(false);
        var targetActive = await repository.GetActiveForActorAsync(org, actorId, ct).ConfigureAwait(false);

        if (ownerCount == 0)
        {
            if (role != PosRole.Owner || actorId != assignedBy)
            {
                return ApplicationResult<PosRoleAssignment>.Failure(
                    DomainErrorCodes.PosRoleBootstrapRequired,
                    "The first assignment must bootstrap Owner for the trusted actor.");
            }
        }
        else
        {
            if (assignerActive is null)
            {
                return ApplicationResult<PosRoleAssignment>.Failure(
                    DomainErrorCodes.PosRoleRequired,
                    "An active POS role is required to assign roles.");
            }

            if (!PosRoleMatrix.CanAssignRole(assignerActive.Role, role))
            {
                return ApplicationResult<PosRoleAssignment>.Failure(
                    DomainErrorCodes.PosRoleAssignForbidden,
                    "The assigner role cannot assign the requested role.");
            }

            if (role == PosRole.Owner && assignerActive.Role != PosRole.Owner)
            {
                return ApplicationResult<PosRoleAssignment>.Failure(
                    DomainErrorCodes.PosRoleAssignForbidden,
                    "Only an Owner may assign Owner.");
            }

            if (role == PosRole.Admin && assignerActive.Role is not (PosRole.Owner or PosRole.Admin))
            {
                return ApplicationResult<PosRoleAssignment>.Failure(
                    DomainErrorCodes.PosRoleAssignForbidden,
                    "Only an Owner or Admin may assign Admin.");
            }
        }

        if (targetActive is not null)
        {
            if (targetActive.Role == role)
            {
                return ApplicationResult<PosRoleAssignment>.Success(targetActive);
            }

            if (targetActive.Role == PosRole.Owner && ownerCount <= 1)
            {
                return ApplicationResult<PosRoleAssignment>.Failure(
                    DomainErrorCodes.PosRoleLastOwnerProtected,
                    "Cannot replace the last active Owner.");
            }

            targetActive.Revoke(assignedBy, clock.UtcNow, "Replaced by new role assignment");
            await repository.UpdateAsync(targetActive, ct).ConfigureAwait(false);
        }

        var created = PosRoleAssignment.Assign(
            org,
            actorId,
            role,
            assignedBy,
            clock.UtcNow,
            assignmentId is Guid id ? PosRoleAssignmentId.From(id) : null);

        await repository.AddAsync(created, ct).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return ApplicationResult<PosRoleAssignment>.Success(created);
    }
}

public sealed class RevokePosRole(
    IPosRoleAssignmentRepository repository,
    IPosUnitOfWork unitOfWork,
    IClock clock)
{
    public Task<ApplicationResult<PosRoleAssignment>> ExecuteAsync(
        Guid organizationId,
        Guid assignmentId,
        Guid revokedBy,
        string? reason = null,
        CancellationToken ct = default) =>
        unitOfWork.ExecuteInSerializableTransactionAsync(
            ct2 => ExecuteCoreAsync(organizationId, assignmentId, revokedBy, reason, ct2),
            ct);

    private async Task<ApplicationResult<PosRoleAssignment>> ExecuteCoreAsync(
        Guid organizationId,
        Guid assignmentId,
        Guid revokedBy,
        string? reason,
        CancellationToken ct)
    {
        var org = PosOrganizationId.From(organizationId);
        await repository.AcquireOrganizationLockAsync(org, ct).ConfigureAwait(false);

        var assignment = await repository.GetByIdAsync(org, PosRoleAssignmentId.From(assignmentId), ct)
            .ConfigureAwait(false);
        if (assignment is null)
        {
            return ApplicationResult<PosRoleAssignment>.Failure(
                DomainErrorCodes.InvalidPosRoleAssignmentId,
                "Role assignment was not found.");
        }

        if (assignment.Status == PosRoleAssignmentStatus.Revoked)
        {
            return ApplicationResult<PosRoleAssignment>.Success(assignment);
        }

        var revoker = await repository.GetActiveForActorAsync(org, revokedBy, ct).ConfigureAwait(false);
        if (revoker is null)
        {
            return ApplicationResult<PosRoleAssignment>.Failure(
                DomainErrorCodes.PosRoleRequired,
                "An active POS role is required to revoke roles.");
        }

        if (assignment.Role == PosRole.Owner && revoker.Role != PosRole.Owner)
        {
            return ApplicationResult<PosRoleAssignment>.Failure(
                DomainErrorCodes.PosRoleAssignForbidden,
                "Only an Owner may revoke Owner.");
        }

        if (assignment.Role == PosRole.Admin && revoker.Role is not (PosRole.Owner or PosRole.Admin))
        {
            return ApplicationResult<PosRoleAssignment>.Failure(
                DomainErrorCodes.PosRoleAssignForbidden,
                "Only an Owner or Admin may revoke Admin.");
        }

        if (!PosRoleMatrix.CanAssignRole(revoker.Role, assignment.Role))
        {
            return ApplicationResult<PosRoleAssignment>.Failure(
                DomainErrorCodes.PosRoleAssignForbidden,
                "The revoker role cannot revoke the target assignment.");
        }

        if (assignment.Role == PosRole.Owner)
        {
            var owners = await repository.CountActiveOwnersAsync(org, ct).ConfigureAwait(false);
            if (owners <= 1)
            {
                return ApplicationResult<PosRoleAssignment>.Failure(
                    DomainErrorCodes.PosRoleLastOwnerProtected,
                    "Cannot revoke the last active Owner.");
            }

            if (assignment.ActorId == revokedBy && owners <= 1)
            {
                return ApplicationResult<PosRoleAssignment>.Failure(
                    DomainErrorCodes.PosRoleLastOwnerProtected,
                    "Cannot revoke your own Owner assignment when you are the last Owner.");
            }
        }

        assignment.Revoke(revokedBy, clock.UtcNow, reason);
        await repository.UpdateAsync(assignment, ct).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        return ApplicationResult<PosRoleAssignment>.Success(assignment);
    }
}
