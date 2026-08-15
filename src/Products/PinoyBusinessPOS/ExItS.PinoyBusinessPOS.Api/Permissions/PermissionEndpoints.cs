using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Domain.Permissions;

namespace ExItS.PinoyBusinessPOS.Api.Permissions;

internal static class PermissionEndpoints
{
    public static IEndpointRouteBuilder MapPermissionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/permissions");

        group.MapGet("/roles", (
            HttpRequest request,
            IPosCommercialAccessAccessor access) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewPermissions, out _, out var problem))
            {
                return problem!;
            }

            return Results.Ok(PosRoleAssignmentQueryService.ListRoles());
        });

        group.MapGet("/assignments", async (
            HttpRequest request,
            string? status,
            Guid? actorId,
            string? role,
            int? page,
            int? pageSize,
            PosRoleAssignmentQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewPermissions, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await queries.ListAsync(organizationId, status, actorId, role, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapGet("/assignments/{assignmentId:guid}", async (
            HttpRequest request,
            Guid assignmentId,
            PosRoleAssignmentQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewPermissions, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await queries.GetByIdAsync(organizationId, assignmentId, ct).ConfigureAwait(false);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapPost("/assignments", async (
            HttpRequest request,
            AssignPosRoleRequest body,
            AssignPosRole useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManagePermissions, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    OfflineOperationTypes.PosRoleAssign,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(
                        organizationId,
                        body.ActorId,
                        body.Role,
                        actorId,
                        body.AssignmentId,
                        ct2),
                    PosRoleAssignmentMapping.Map,
                    dto => Results.Created($"/api/v1/pos/permissions/assignments/{dto.AssignmentId:D}", dto),
                    ct)
                .ConfigureAwait(false);
        });

        group.MapPost("/assignments/{assignmentId:guid}/revoke", async (
            HttpRequest request,
            Guid assignmentId,
            RevokePosRoleRequest? body,
            RevokePosRole useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManagePermissions, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    OfflineOperationTypes.PosRoleRevoke,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(
                        organizationId,
                        assignmentId,
                        actorId,
                        body?.Reason,
                        ct2),
                    PosRoleAssignmentMapping.Map,
                    Results.Ok,
                    ct)
                .ConfigureAwait(false);
        });

        group.MapGet("/effective", async (
            HttpRequest request,
            PosRoleAssignmentQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewPermissions, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await queries.GetEffectiveAsync(organizationId, actorId, ct).ConfigureAwait(false);
            return Results.Ok(ApplyRequestRoleFallback(result, actorId));
        });

        group.MapGet("/actors/{actorId:guid}/effective", async (
            HttpRequest request,
            Guid actorId,
            PosRoleAssignmentQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewPermissions, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await queries.GetEffectiveAsync(organizationId, actorId, ct).ConfigureAwait(false);
            // Only apply Dev/request role fallback for the caller's own effective view.
            if (PosOrganizationScope.TryGetActorId(request, out var callerId, out _) && callerId == actorId)
            {
                result = ApplyRequestRoleFallback(result, actorId);
            }

            return Results.Ok(result);
        });

        return app;
    }

    /// <summary>
    /// Role resolution middleware may set an in-memory Dev Owner (or just-synced mapped role)
    /// without a DB row yet. Effective permissions must reflect that role so Mobile home routing works.
    /// </summary>
    private static PosEffectivePermissionsDto ApplyRequestRoleFallback(
        PosEffectivePermissionsDto result,
        Guid actorId)
    {
        if (!string.IsNullOrWhiteSpace(result.Role)
            && string.Equals(result.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            return result;
        }

        if (PosRoleRequestContext.CurrentRole is not { } role)
        {
            if (PosRoleRequestContext.OrganizationManagementAuthority)
            {
                var caps = PosRoleMatrix.OrganizationManagementCapabilities(
                    PosRoleRequestContext.OrganizationManagementIsExactOwner);
                return new PosEffectivePermissionsDto(
                    result.OrganizationId,
                    actorId,
                    Role: PosRoleRequestContext.OrganizationManagementIsExactOwner
                        ? "OrganizationOwner"
                        : "OrganizationAdministrator",
                    RoleDisplayName: PosRoleRequestContext.OrganizationManagementIsExactOwner
                        ? "Organization Owner"
                        : "Organization Administrator",
                    Status: "Active",
                    AllowedCapabilities: caps.Select(c => c.ToString()).ToArray(),
                    AllowedFeatureCodes: [],
                    CanManageAssignments: PosRoleRequestContext.OrganizationManagementIsExactOwner
                        || result.IsBootstrapEligible,
                    IsBootstrapEligible: result.IsBootstrapEligible);
            }

            return result;
        }

        return new PosEffectivePermissionsDto(
            result.OrganizationId,
            actorId,
            PosRoleCodes.ToCode(role),
            PosRoleCodes.ToDisplayName(role),
            "Active",
            PosRoleMatrix.CapabilitiesFor(role).Select(c => c.ToString()).ToArray(),
            PosRoleAssignmentMapping.FeatureCodesForRole(role),
            PosRoleMatrix.CanManageAssignments(role) || result.IsBootstrapEligible,
            result.IsBootstrapEligible);
    }

    private static bool TryAuthorize(
        HttpRequest request,
        IPosCommercialAccessAccessor access,
        UtangCapability capability,
        out Guid organizationId,
        out IResult? problem)
    {
        problem = null;
        organizationId = Guid.Empty;

        if (!PosOrganizationScope.TryGetOrganizationId(request, out organizationId, out problem))
        {
            return false;
        }

        return PosCommercialScope.TryAuthorize(access, capability, out problem);
    }
}
