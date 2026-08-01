using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Api.Authorization;

internal static class OrganizationRbacEndpoints
{
    public static IEndpointRouteBuilder MapOrganizationRbacEndpoints(this IEndpointRouteBuilder app)
    {
        var org = app.MapGroup("/api/v1/platform/organizations/{organizationId:guid}");

        org.MapGet("/role-definitions", async (
            Guid organizationId,
            string? status,
            string? search,
            int? page,
            int? pageSize,
            OrganizationRoleDefinitionQueryService queries,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(PlatformAuditActions.PlatformAccessChecked, nameof(OrganizationRoleDefinition), organizationId.ToString("D"), organizationId, cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            PlatformRoleLifecycleStatus? parsedStatus = null;
            if (!string.IsNullOrWhiteSpace(status)
                && Enum.TryParse<PlatformRoleLifecycleStatus>(status, ignoreCase: true, out var value))
            {
                parsedStatus = value;
            }

            var result = await queries.ListAsync(organizationId, parsedStatus, search, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        org.MapGet("/role-definitions/{roleId:guid}", async (
            Guid organizationId,
            Guid roleId,
            OrganizationRoleDefinitionQueryService queries,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(PlatformAuditActions.PlatformAccessChecked, nameof(OrganizationRoleDefinition), organizationId.ToString("D"), organizationId, cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var item = await queries.GetByIdAsync(roleId, ct).ConfigureAwait(false);
            if (item is null || item.OrganizationId != organizationId)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.OrganizationRoleDefinitionNotFound,
                    "Organization role definition was not found.",
                    StatusCodes.Status404NotFound);
            }

            return Results.Ok(item);
        });

        org.MapPost("/role-definitions", async (
            Guid organizationId,
            CreateRoleDefinitionRequest body,
            CreateOrganizationRoleDefinition useCase,
            PlatformMembershipAuthz membershipAuthz,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(PlatformAuditActions.PlatformAccessChecked, nameof(OrganizationRoleDefinition), organizationId.ToString("D"), organizationId, cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = authz.CurrentActor;
            var result = await useCase.ExecuteAsync(
                organizationId,
                body.Code,
                body.Name,
                body.Description,
                body.Permissions ?? Array.Empty<string>(),
                actor.ActorIdentifier,
                actor.ActorType,
                actor.CorrelationId,
                ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, d => Results.Created(
                $"/api/v1/platform/organizations/{organizationId:D}/role-definitions/{d.Id.Value}",
                RbacDtoMaps.Map(d)));
        });

        org.MapPut("/role-definitions/{roleId:guid}", async (
            Guid organizationId,
            Guid roleId,
            UpdateRoleDefinitionRequest body,
            UpdateOrganizationRoleDefinition useCase,
            PlatformMembershipAuthz membershipAuthz,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(PlatformAuditActions.PlatformAccessChecked, nameof(OrganizationRoleDefinition), organizationId.ToString("D"), organizationId, cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = authz.CurrentActor;
            var result = await useCase.ExecuteAsync(
                organizationId,
                roleId,
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

        org.MapPost("/role-definitions/{roleId:guid}/activate", async (
            Guid organizationId,
            Guid roleId,
            RoleLifecycleRequest? body,
            ChangeOrganizationRoleDefinitionStatus useCase,
            PlatformMembershipAuthz membershipAuthz,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(PlatformAuditActions.PlatformAccessChecked, nameof(OrganizationRoleDefinition), organizationId.ToString("D"), organizationId, cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = authz.CurrentActor;
            var result = await useCase.ActivateAsync(
                organizationId, roleId, body?.ExpectedVersion, actor.ActorIdentifier, actor.ActorType, actor.CorrelationId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, d => Results.Ok(RbacDtoMaps.Map(d)));
        });

        org.MapPost("/role-definitions/{roleId:guid}/deactivate", async (
            Guid organizationId,
            Guid roleId,
            RoleLifecycleRequest? body,
            ChangeOrganizationRoleDefinitionStatus useCase,
            PlatformMembershipAuthz membershipAuthz,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(PlatformAuditActions.PlatformAccessChecked, nameof(OrganizationRoleDefinition), organizationId.ToString("D"), organizationId, cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = authz.CurrentActor;
            var result = await useCase.DeactivateAsync(
                organizationId, roleId, body?.ExpectedVersion, actor.ActorIdentifier, actor.ActorType, actor.CorrelationId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, d => Results.Ok(RbacDtoMaps.Map(d)));
        });

        org.MapPost("/role-definitions/{roleId:guid}/retire", async (
            Guid organizationId,
            Guid roleId,
            RoleLifecycleRequest? body,
            ChangeOrganizationRoleDefinitionStatus useCase,
            PlatformMembershipAuthz membershipAuthz,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(PlatformAuditActions.PlatformAccessChecked, nameof(OrganizationRoleDefinition), organizationId.ToString("D"), organizationId, cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = authz.CurrentActor;
            var result = await useCase.RetireAsync(
                organizationId, roleId, body?.ExpectedVersion, actor.ActorIdentifier, actor.ActorType, actor.CorrelationId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, d => Results.Ok(RbacDtoMaps.Map(d)));
        });

        org.MapGet("/role-assignments", async (
            Guid organizationId,
            Guid? platformUserId,
            Guid? roleDefinitionId,
            string? status,
            int? page,
            int? pageSize,
            ListOrganizationCustomRoleAssignments useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(PlatformAuditActions.PlatformAccessChecked, nameof(OrganizationRoleDefinition), organizationId.ToString("D"), organizationId, cancellationToken: ct).ConfigureAwait(false);
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

            var result = await useCase
                .ExecuteAsync(organizationId, platformUserId, roleDefinitionId, parsedStatus, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        org.MapPost("/role-assignments", async (
            Guid organizationId,
            AssignOrgCustomRoleRequest body,
            AssignOrganizationCustomRole useCase,
            PlatformMembershipAuthz membershipAuthz,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(PlatformAuditActions.PlatformAccessChecked, nameof(OrganizationRoleDefinition), organizationId.ToString("D"), organizationId, cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = authz.CurrentActor;
            var result = await useCase.ExecuteAsync(
                organizationId,
                body.PlatformUserId,
                body.RoleDefinitionId,
                actor.ActorIdentifier,
                actor.ActorType,
                body.Reason,
                actor.CorrelationId,
                ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, a => Results.Created(
                $"/api/v1/platform/organizations/{organizationId:D}/role-assignments/{a.Id.Value}",
                RbacDtoMaps.Map(a)));
        });

        org.MapPost("/role-assignments/{assignmentId:guid}/revoke", async (
            Guid organizationId,
            Guid assignmentId,
            RevokePlatformRoleRequest? body,
            RevokeOrganizationCustomRole useCase,
            PlatformMembershipAuthz membershipAuthz,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(PlatformAuditActions.PlatformAccessChecked, nameof(OrganizationRoleDefinition), organizationId.ToString("D"), organizationId, cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = authz.CurrentActor;
            var result = await useCase.ExecuteAsync(
                organizationId,
                assignmentId,
                actor.ActorIdentifier,
                actor.ActorType,
                body?.Reason,
                actor.CorrelationId,
                ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, a => Results.Ok(RbacDtoMaps.Map(a)));
        });

        org.MapGet("/members/{userId:guid}/effective-permissions", async (
            Guid organizationId,
            Guid userId,
            ResolveEffectiveOrganizationPermissions useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(PlatformAuditActions.PlatformAccessChecked, nameof(OrganizationRoleDefinition), organizationId.ToString("D"), organizationId, cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(organizationId, userId, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        return app;
    }
}

internal sealed record AssignOrgCustomRoleRequest(Guid PlatformUserId, Guid RoleDefinitionId, string? Reason);
