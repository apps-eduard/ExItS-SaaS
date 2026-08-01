using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using Microsoft.AspNetCore.RateLimiting;

namespace ExItS.Platform.Api.Authorization;

/// <summary>
/// Platform authorization endpoints: current-actor permission resolution, the static role/permission
/// catalog, and Platform system role assignment management. Development-stage only: the actor is
/// resolved via <see cref="ExItS.Platform.Infrastructure.Authorization.DevelopmentPlatformActorAccessor"/>
/// (not production authentication), but permission enforcement itself is real.
/// </summary>
internal static class AuthorizationEndpoints
{
    public static IEndpointRouteBuilder MapAuthorizationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/platform/authorization");

        group.MapGet("/me", async (
            Guid? organizationId,
            ResolveCurrentPermissions useCase,
            CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(organizationId, ct).ConfigureAwait(false);
            return Results.Ok(result);
        })
        .DisableRateLimiting();

        group.MapGet("/assignments", async (
            Guid? platformUserId,
            string? role,
            Guid? organizationId,
            string? status,
            int? page,
            int? pageSize,
            ListPlatformRoles useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(PlatformRoleAssignment),
                "list",
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            PlatformSystemRole? parsedRole = null;
            if (!string.IsNullOrWhiteSpace(role))
            {
                if (!Enum.TryParse<PlatformSystemRole>(role, ignoreCase: true, out var value))
                {
                    return PlatformApiResults.Problem(
                        DomainErrorCodes.InvalidPlatformSystemRole,
                        $"Unrecognized platform system role '{role}'.",
                        StatusCodes.Status400BadRequest);
                }

                parsedRole = value;
            }

            PlatformRoleAssignmentStatus? parsedStatus = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<PlatformRoleAssignmentStatus>(status, ignoreCase: true, out var value))
                {
                    return PlatformApiResults.Problem(
                        "platform.role_assignment.status.invalid",
                        $"Unrecognized role assignment status '{status}'.",
                        StatusCodes.Status400BadRequest);
                }

                parsedStatus = value;
            }

            var result = await useCase
                .ExecuteAsync(platformUserId, parsedRole, organizationId, parsedStatus, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPost("/assignments", async (
            AssignPlatformRoleRequest body,
            AssignPlatformRole useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<PlatformSystemRole>(body.Role, ignoreCase: true, out var role))
            {
                return PlatformApiResults.Problem(
                    DomainErrorCodes.InvalidPlatformSystemRole,
                    $"Unrecognized platform system role '{body.Role}'.",
                    StatusCodes.Status400BadRequest);
            }

            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformRoleAssigned,
                nameof(PlatformRoleAssignment),
                body.PlatformUserId.ToString("D"),
                body.OrganizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = authz.CurrentActor;
            var result = await useCase
                .ExecuteAsync(
                    body.PlatformUserId,
                    role,
                    body.OrganizationId,
                    actor.ActorIdentifier,
                    actor.ActorType,
                    body.Reason,
                    actor.CorrelationId,
                    ct)
                .ConfigureAwait(false);

            // AssignPlatformRole already writes the Succeeded/Failed audit record itself.
            return PlatformApiResults.FromResult(result, a => Results.Created(
                $"/api/v1/platform/authorization/assignments/{a.Id.Value}",
                ListPlatformRoles.Map(a)));
        });

        group.MapPost("/assignments/{id:guid}/revoke", async (
            Guid id,
            RevokePlatformRoleRequest? body,
            ListPlatformRoles listUseCase,
            RevokePlatformRole useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var existing = await listUseCase.GetByIdAsync(id, ct).ConfigureAwait(false);
            if (existing is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.RoleAssignmentNotFound,
                    "Platform role assignment was not found.",
                    StatusCodes.Status404NotFound);
            }

            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformRoleRevoked,
                nameof(PlatformRoleAssignment),
                id.ToString("D"),
                existing.OrganizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = authz.CurrentActor;
            var result = await useCase
                .ExecuteAsync(id, actor.ActorIdentifier, actor.ActorType, body?.Reason, actor.CorrelationId, ct)
                .ConfigureAwait(false);

            // RevokePlatformRole already writes the Succeeded/Failed audit record itself.
            return PlatformApiResults.FromResult(result, a => Results.Ok(ListPlatformRoles.Map(a)));
        });

        group.MapGet("/roles", async (PlatformAuthz authz, CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformAccessChecked,
                "PlatformSystemRole",
                "catalog",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var roles = Enum.GetValues<PlatformSystemRole>()
                .Select(role => new
                {
                    role = role.ToString(),
                    permissions = PlatformRolePermissionCatalog.GetPermissions(role)
                        .OrderBy(p => p, StringComparer.Ordinal)
                        .ToList()
                })
                .ToList();
            return Results.Ok(roles);
        });

        group.MapGet("/permissions", async (PlatformAuthz authz, CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformAccessChecked,
                "PlatformPermission",
                "catalog",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var entries = PlatformPermission.All.Select(code => new PermissionCatalogEntryDto(
                code,
                code.Replace("platform.permission.", string.Empty, StringComparison.Ordinal).Replace('_', ' '),
                "platform")).ToList();
            return Results.Ok(entries);
        });

        group.MapGet("/organization-permissions", async (PlatformAuthz authz, CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageMemberships,
                PlatformAuditActions.PlatformAccessChecked,
                "OrganizationPermission",
                "catalog",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var entries = OrganizationPermission.All.Select(code => new PermissionCatalogEntryDto(
                code,
                OrganizationPermission.Descriptions.TryGetValue(code, out var d) ? d : code,
                "organization")).ToList();
            return Results.Ok(entries);
        });

        group.MapGet("/role-definitions", async (
            string? kind,
            string? status,
            string? search,
            int? page,
            int? pageSize,
            PlatformRoleDefinitionQueryService queries,
            EnsureBuiltInPlatformRoleDefinitions seed,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(PlatformRoleDefinition),
                "list",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            await seed.ExecuteAsync(ct).ConfigureAwait(false);

            PlatformRoleKind? parsedKind = null;
            if (!string.IsNullOrWhiteSpace(kind)
                && Enum.TryParse<PlatformRoleKind>(kind, ignoreCase: true, out var kindValue))
            {
                parsedKind = kindValue;
            }

            PlatformRoleLifecycleStatus? parsedStatus = null;
            if (!string.IsNullOrWhiteSpace(status)
                && Enum.TryParse<PlatformRoleLifecycleStatus>(status, ignoreCase: true, out var statusValue))
            {
                parsedStatus = statusValue;
            }

            var result = await queries.ListAsync(parsedKind, parsedStatus, search, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapGet("/role-definitions/{id:guid}", async (
            Guid id,
            PlatformRoleDefinitionQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(PlatformRoleDefinition),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var item = await queries.GetByIdAsync(id, ct).ConfigureAwait(false);
            return item is null
                ? PlatformApiResults.Problem(ApplicationErrorCodes.RoleDefinitionNotFound, "Platform role definition was not found.", StatusCodes.Status404NotFound)
                : Results.Ok(item);
        });

        group.MapPost("/role-definitions", async (
            CreateRoleDefinitionRequest body,
            CreatePlatformRoleDefinition useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformRoleDefinitionCreated,
                nameof(PlatformRoleDefinition),
                body.Code,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = authz.CurrentActor;
            var result = await useCase.ExecuteAsync(
                body.Code,
                body.Name,
                body.Description,
                body.Permissions ?? Array.Empty<string>(),
                actor.ActorIdentifier,
                actor.ActorType,
                actor.CorrelationId,
                ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, d => Results.Created(
                $"/api/v1/platform/authorization/role-definitions/{d.Id.Value}",
                RbacDtoMaps.Map(d)));
        });

        group.MapPut("/role-definitions/{id:guid}", async (
            Guid id,
            UpdateRoleDefinitionRequest body,
            UpdatePlatformRoleDefinition useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformRoleDefinitionUpdated,
                nameof(PlatformRoleDefinition),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = authz.CurrentActor;
            var result = await useCase.ExecuteAsync(
                id,
                body.Name,
                body.Description,
                body.Permissions,
                body.ExpectedVersion,
                actor.ActorIdentifier,
                actor.ActorType,
                actor.CorrelationId,
                ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, d => Results.Ok(RbacDtoMaps.Map(d)));
        });

        group.MapPost("/role-definitions/{id:guid}/activate", async (
            Guid id,
            RoleLifecycleRequest? body,
            ChangePlatformRoleDefinitionStatus useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformRoleDefinitionActivated,
                nameof(PlatformRoleDefinition),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = authz.CurrentActor;
            var result = await useCase.ActivateAsync(id, body?.ExpectedVersion, actor.ActorIdentifier, actor.ActorType, actor.CorrelationId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, d => Results.Ok(RbacDtoMaps.Map(d)));
        });

        group.MapPost("/role-definitions/{id:guid}/deactivate", async (
            Guid id,
            RoleLifecycleRequest? body,
            ChangePlatformRoleDefinitionStatus useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformRoleDefinitionDeactivated,
                nameof(PlatformRoleDefinition),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = authz.CurrentActor;
            var result = await useCase.DeactivateAsync(id, body?.ExpectedVersion, actor.ActorIdentifier, actor.ActorType, actor.CorrelationId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, d => Results.Ok(RbacDtoMaps.Map(d)));
        });

        group.MapPost("/role-definitions/{id:guid}/retire", async (
            Guid id,
            RoleLifecycleRequest? body,
            ChangePlatformRoleDefinitionStatus useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformRoleDefinitionRetired,
                nameof(PlatformRoleDefinition),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = authz.CurrentActor;
            var result = await useCase.RetireAsync(id, body?.ExpectedVersion, actor.ActorIdentifier, actor.ActorType, actor.CorrelationId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, d => Results.Ok(RbacDtoMaps.Map(d)));
        });

        group.MapGet("/custom-assignments", async (
            Guid? platformUserId,
            Guid? roleDefinitionId,
            string? status,
            int? page,
            int? pageSize,
            ListPlatformCustomRoleAssignments useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(PlatformCustomRoleAssignment),
                "list",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            PlatformRoleAssignmentStatus? parsedStatus = null;
            if (!string.IsNullOrWhiteSpace(status)
                && Enum.TryParse<PlatformRoleAssignmentStatus>(status, ignoreCase: true, out var value))
            {
                parsedStatus = value;
            }

            var result = await useCase.ExecuteAsync(platformUserId, roleDefinitionId, parsedStatus, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapPost("/custom-assignments", async (
            AssignCustomRoleRequest body,
            AssignPlatformCustomRole useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformCustomRoleAssigned,
                nameof(PlatformCustomRoleAssignment),
                body.PlatformUserId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = authz.CurrentActor;
            var result = await useCase.ExecuteAsync(
                body.PlatformUserId,
                body.RoleDefinitionId,
                actor.ActorIdentifier,
                actor.ActorType,
                body.Reason,
                actor.CorrelationId,
                ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, a => Results.Created(
                $"/api/v1/platform/authorization/custom-assignments/{a.Id.Value}",
                RbacDtoMaps.Map(a)));
        });

        group.MapPost("/custom-assignments/{id:guid}/revoke", async (
            Guid id,
            RevokePlatformRoleRequest? body,
            RevokePlatformCustomRole useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformCustomRoleRevoked,
                nameof(PlatformCustomRoleAssignment),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = authz.CurrentActor;
            var result = await useCase.ExecuteAsync(id, actor.ActorIdentifier, actor.ActorType, body?.Reason, actor.CorrelationId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, a => Results.Ok(RbacDtoMaps.Map(a)));
        });

        group.MapGet("/users/{userId:guid}/effective-permissions", async (
            Guid userId,
            Guid? organizationId,
            ResolveEffectivePlatformPermissions useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(PlatformUser),
                userId.ToString("D"),
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(userId, organizationId, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        return app;
    }
}

internal sealed record AssignPlatformRoleRequest(Guid PlatformUserId, string Role, Guid? OrganizationId, string? Reason);
internal sealed record RevokePlatformRoleRequest(string? Reason);
internal sealed record CreateRoleDefinitionRequest(string Code, string Name, string? Description, IReadOnlyList<string>? Permissions);
internal sealed record UpdateRoleDefinitionRequest(string Name, string? Description, IReadOnlyList<string>? Permissions, int? ExpectedVersion);
internal sealed record RoleLifecycleRequest(int? ExpectedVersion, string? Reason);
internal sealed record AssignCustomRoleRequest(Guid PlatformUserId, Guid RoleDefinitionId, string? Reason);
