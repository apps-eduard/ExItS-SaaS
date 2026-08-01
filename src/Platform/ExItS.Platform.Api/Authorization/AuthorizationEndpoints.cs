using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
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

        group.MapGet("/roles", () =>
        {
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

        return app;
    }
}

internal sealed record AssignPlatformRoleRequest(Guid PlatformUserId, string Role, Guid? OrganizationId, string? Reason);
internal sealed record RevokePlatformRoleRequest(string? Reason);
