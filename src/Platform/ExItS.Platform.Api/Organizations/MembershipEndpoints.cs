using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Governance;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Governance;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Organizations;

/// <summary>
/// Organization membership endpoints. Platform organization roles only — never product-local roles.
/// Mutations and reads require ManageMemberships (Platform emergency override) or an Organization Owner
/// in trusted organization context.
/// </summary>
internal static class MembershipEndpoints
{
    public static IEndpointRouteBuilder MapMembershipEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/platform/organizations/{organizationId:guid}/members", async (
            Guid organizationId,
            string? status,
            int? page,
            int? pageSize,
            MembershipQueryService queries,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.PlatformAccessChecked,
                nameof(OrganizationMembership),
                organizationId.ToString("D"),
                organizationId,
                summary: "List organization members.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            if (!TryParseMembershipStatus(status, out var parsed, out var error))
            {
                return error!;
            }

            var result = await queries
                .ListByOrganizationAsync(organizationId, parsed, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        // Org-scoped operational actor names for detail/history UI.
        // Any active org member may resolve — not ManageMemberships (cashiers view sales).
        app.MapPost("/api/v1/platform/organizations/{organizationId:guid}/actor-display-names", async (
            Guid organizationId,
            ResolveOrganizationActorDisplayNamesRequest? body,
            ResolveOrganizationActorDisplayNames useCase,
            PlatformOrganizationAuthz organizationAuthz,
            CancellationToken ct) =>
        {
            var denied = await organizationAuthz
                .EnsureCanViewOrganizationAsync(organizationId, ct)
                .ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var request = body ?? new ResolveOrganizationActorDisplayNamesRequest(Array.Empty<Guid>());
            return PlatformApiResults.FromResult(
                await useCase.ExecuteAsync(organizationId, request, ct).ConfigureAwait(false),
                items => Results.Ok(new { items }));
        });

        app.MapPost("/api/v1/platform/organizations/{organizationId:guid}/members", async (
            Guid organizationId,
            AddMemberRequest body,
            AddOrganizationMembership useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            if (!TryParseRole(body.Role, out var role, out var error))
            {
                return error!;
            }

            var authority = await membershipAuthz
                .ResolveActorMembershipAuthorityAsync(organizationId, ct)
                .ConfigureAwait(false);
            // Raw GUID membership linking is a Platform support override — not Owner onboarding.
            if (!authority.HasPlatformManageMemberships)
            {
                return PlatformApiResults.Problem(
                    DomainErrorCodes.AuthorizationDenied,
                    "Linking an existing identity by User ID is restricted to Platform support. Use Invite Staff instead.",
                    StatusCodes.Status403Forbidden);
            }

            if (!string.IsNullOrWhiteSpace(body.Reason) && body.Reason.Trim().Length < 8)
            {
                return PlatformApiResults.Problem(
                    DomainErrorCodes.InvalidAuditReason,
                    "A support reason must be at least 8 characters when provided.",
                    StatusCodes.Status400BadRequest);
            }

            var linkReason = string.IsNullOrWhiteSpace(body.Reason)
                ? "Platform support identity link"
                : body.Reason.Trim();

            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.MembershipAdded,
                nameof(OrganizationMembership),
                body.UserId.ToString("D"),
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            // Platform support GUID link: Owner (Personal identity) or Staff (org-scoped staff identity).
            // OrganizationAdministrator remains test/legacy-only.
            if (role is not OrganizationRole.OrganizationOwner
                and not OrganizationRole.OrganizationMember
                and not OrganizationRole.OrganizationAdministrator)
            {
                return PlatformApiResults.Problem(
                    DomainErrorCodes.InvalidOrganizationRole,
                    "Organization membership roles are Owner and Staff only.",
                    StatusCodes.Status400BadRequest);
            }

            try
            {
                var result = await useCase
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        PlatformUserId.From(body.UserId),
                        role,
                        ct)
                    .ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await membershipAuthz.Inner.AuditSucceededAsync(
                        PlatformAuditActions.MembershipAdded,
                        nameof(OrganizationMembership),
                        result.Value!.Id.Value.ToString("D"),
                        organizationId,
                        summary: $"Advanced identity link. Reason: {linkReason}",
                        cancellationToken: ct).ConfigureAwait(false);
                }

                return PlatformApiResults.FromResult(result, m => Results.Created(
                    $"/api/v1/platform/memberships/{m.Id.Value}",
                    MembershipQueryService.Map(m)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        app.MapGet("/api/v1/platform/users/{userId:guid}/memberships", async (
            Guid userId,
            string? status,
            int? page,
            int? pageSize,
            MembershipQueryService queries,
            PlatformAuthz authz,
            PlatformMembershipAuthz membershipAuthz,
            ExItS.Platform.Application.Authorization.IPlatformAuthorizationService authorization,
            CancellationToken ct) =>
        {
            var actor = authz.CurrentActor;
            var canManageUsers = actor.PlatformUserId is not null
                && await authorization
                    .HasPermissionAsync(actor.PlatformUserId, PlatformPermission.ManagePlatformUsers, null, ct)
                    .ConfigureAwait(false);
            if (!canManageUsers
                && actor.ActorType == AuditActorType.DevelopmentOperator)
            {
                var perms = await authorization
                    .ResolvePermissionsForActorAsync(actor, null, ct)
                    .ConfigureAwait(false);
                canManageUsers = perms.Contains(PlatformPermission.ManagePlatformUsers);
            }

            if (!canManageUsers)
            {
                if (actor.OrganizationId is null)
                {
                    return await authz.EnsureAsync(
                        PlatformPermission.ManagePlatformUsers,
                        PlatformAuditActions.MembershipAdded,
                        nameof(OrganizationMembership),
                        userId.ToString("D"),
                        summary: "List user memberships.",
                        cancellationToken: ct).ConfigureAwait(false)!;
                }

                var orgDenied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                    PlatformAuditActions.MembershipAdded,
                    nameof(OrganizationMembership),
                    userId.ToString("D"),
                    actor.OrganizationId.Value,
                    summary: "List user memberships (org scope).",
                    cancellationToken: ct).ConfigureAwait(false);
                if (orgDenied is not null)
                {
                    return orgDenied;
                }
            }

            if (!TryParseMembershipStatus(status, out var parsed, out var error))
            {
                return error!;
            }

            var result = await queries.ListByUserAsync(userId, parsed, page, pageSize, ct).ConfigureAwait(false);
            if (!canManageUsers && actor.OrganizationId is not null)
            {
                var filtered = result.Items.Where(m => m.OrganizationId == actor.OrganizationId.Value).ToList();
                result = result with { Items = filtered, TotalCount = filtered.Count };
            }

            return Results.Ok(result);
        });

        app.MapPut("/api/v1/platform/memberships/{membershipId:guid}/role", async (
            Guid membershipId,
            ChangeRoleRequest body,
            ChangeOrganizationRole useCase,
            MembershipQueryService queries,
            PlatformMembershipAuthz membershipAuthz,
            ConsumeGovernanceStepUpGrant stepUp,
            PlatformAuthz platformAuthz,
            CancellationToken ct) =>
        {
            if (!TryParseRole(body.Role, out var role, out var error))
            {
                return error!;
            }

            var existing = await queries.GetByIdAsync(membershipId, ct).ConfigureAwait(false);
            if (existing is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.MembershipNotFound,
                    "Membership was not found.",
                    StatusCodes.Status404NotFound);
            }

            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.MembershipRoleChanged,
                nameof(OrganizationMembership),
                membershipId.ToString("D"),
                existing.OrganizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = platformAuthz.CurrentActor.PlatformUserId;
            var stepUpDenied = await GovernanceStepUpHelper.EnsureConsumedAsync(
                stepUp,
                actor,
                PlatformOrganizationId.From(existing.OrganizationId),
                GovernanceCriticalActionCodes.MembershipRoleChange,
                GovernanceStepUpTargetTypes.OrganizationMembership,
                membershipId,
                body.StepUpToken,
                ct).ConfigureAwait(false);
            if (stepUpDenied is not null)
            {
                return stepUpDenied;
            }

            var authority = await membershipAuthz
                .ResolveActorMembershipAuthorityAsync(existing.OrganizationId, ct)
                .ConfigureAwait(false);

            try
            {
                var result = await useCase
                    .ExecuteAsync(
                        OrganizationMembershipId.From(membershipId),
                        role,
                        body.ActorReference,
                        authority.ActorMembershipRole,
                        authority.HasPlatformManageMemberships,
                        ct)
                    .ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await membershipAuthz.Inner.AuditSucceededAsync(
                        PlatformAuditActions.MembershipRoleChanged,
                        nameof(OrganizationMembership),
                        membershipId.ToString("D"),
                        result.Value!.OrganizationId.Value,
                        summary: $"Changed membership role to {role}. Auth strength: PasswordStepUp.",
                        cancellationToken: ct).ConfigureAwait(false);
                }

                return PlatformApiResults.FromResult(result, m => Results.Ok(MembershipQueryService.Map(m)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        app.MapPost("/api/v1/platform/memberships/{membershipId:guid}/suspend", async (
            Guid membershipId,
            MembershipLifecycleRequest? body,
            SuspendOrganizationMembership useCase,
            MembershipQueryService queries,
            PlatformMembershipAuthz membershipAuthz,
            ConsumeGovernanceStepUpGrant stepUp,
            PlatformAuthz platformAuthz,
            CancellationToken ct) =>
        {
            var existing = await queries.GetByIdAsync(membershipId, ct).ConfigureAwait(false);
            if (existing is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.MembershipNotFound,
                    "Membership was not found.",
                    StatusCodes.Status404NotFound);
            }

            var reasonError = GovernanceCriticalActionReason.ValidateRequired(body?.Reason);
            if (reasonError is not null)
            {
                return PlatformApiResults.Problem(reasonError.ErrorCode!, reasonError.ErrorMessage!, StatusCodes.Status400BadRequest);
            }

            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.MembershipSuspended,
                nameof(OrganizationMembership),
                membershipId.ToString("D"),
                existing.OrganizationId,
                reason: body?.Reason,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = platformAuthz.CurrentActor.PlatformUserId;
            var stepUpDenied = await GovernanceStepUpHelper.EnsureConsumedAsync(
                stepUp,
                actor,
                PlatformOrganizationId.From(existing.OrganizationId),
                GovernanceCriticalActionCodes.MembershipSuspend,
                GovernanceStepUpTargetTypes.OrganizationMembership,
                membershipId,
                body?.StepUpToken,
                ct).ConfigureAwait(false);
            if (stepUpDenied is not null)
            {
                return stepUpDenied;
            }

            try
            {
                var result = await useCase
                    .ExecuteAsync(OrganizationMembershipId.From(membershipId), body?.Reason, body?.ActorReference, ct)
                    .ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await membershipAuthz.Inner.AuditSucceededAsync(
                        PlatformAuditActions.MembershipSuspended,
                        nameof(OrganizationMembership),
                        membershipId.ToString("D"),
                        result.Value!.OrganizationId.Value,
                        reason: body?.Reason,
                        summary: "Auth strength: PasswordStepUp.",
                        cancellationToken: ct).ConfigureAwait(false);
                }

                return PlatformApiResults.FromResult(result, m => Results.Ok(MembershipQueryService.Map(m)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        app.MapPost("/api/v1/platform/memberships/{membershipId:guid}/reactivate", async (
            Guid membershipId,
            MembershipLifecycleRequest? body,
            ReactivateOrganizationMembership useCase,
            MembershipQueryService queries,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var existing = await queries.GetByIdAsync(membershipId, ct).ConfigureAwait(false);
            if (existing is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.MembershipNotFound,
                    "Membership was not found.",
                    StatusCodes.Status404NotFound);
            }

            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.MembershipReactivated,
                nameof(OrganizationMembership),
                membershipId.ToString("D"),
                existing.OrganizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                var result = await useCase
                    .ExecuteAsync(OrganizationMembershipId.From(membershipId), body?.ActorReference, ct)
                    .ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await membershipAuthz.Inner.AuditSucceededAsync(
                        PlatformAuditActions.MembershipReactivated,
                        nameof(OrganizationMembership),
                        membershipId.ToString("D"),
                        result.Value!.OrganizationId.Value,
                        cancellationToken: ct).ConfigureAwait(false);
                }

                return PlatformApiResults.FromResult(result, m => Results.Ok(MembershipQueryService.Map(m)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        app.MapPost("/api/v1/platform/memberships/{membershipId:guid}/revoke", async (
            Guid membershipId,
            MembershipLifecycleRequest? body,
            RevokeOrganizationMembership useCase,
            MembershipQueryService queries,
            PlatformMembershipAuthz membershipAuthz,
            ConsumeGovernanceStepUpGrant stepUp,
            PlatformAuthz platformAuthz,
            CancellationToken ct) =>
        {
            var existing = await queries.GetByIdAsync(membershipId, ct).ConfigureAwait(false);
            if (existing is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.MembershipNotFound,
                    "Membership was not found.",
                    StatusCodes.Status404NotFound);
            }

            var reasonError = GovernanceCriticalActionReason.ValidateRequired(body?.Reason);
            if (reasonError is not null)
            {
                return PlatformApiResults.Problem(reasonError.ErrorCode!, reasonError.ErrorMessage!, StatusCodes.Status400BadRequest);
            }

            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.MembershipRevoked,
                nameof(OrganizationMembership),
                membershipId.ToString("D"),
                existing.OrganizationId,
                reason: body?.Reason,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = platformAuthz.CurrentActor.PlatformUserId;
            var stepUpDenied = await GovernanceStepUpHelper.EnsureConsumedAsync(
                stepUp,
                actor,
                PlatformOrganizationId.From(existing.OrganizationId),
                GovernanceCriticalActionCodes.MembershipRevoke,
                GovernanceStepUpTargetTypes.OrganizationMembership,
                membershipId,
                body?.StepUpToken,
                ct).ConfigureAwait(false);
            if (stepUpDenied is not null)
            {
                return stepUpDenied;
            }

            try
            {
                var result = await useCase
                    .ExecuteAsync(OrganizationMembershipId.From(membershipId), body!.Reason!, body?.ActorReference, ct)
                    .ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await membershipAuthz.Inner.AuditSucceededAsync(
                        PlatformAuditActions.MembershipRevoked,
                        nameof(OrganizationMembership),
                        membershipId.ToString("D"),
                        result.Value!.OrganizationId.Value,
                        reason: body?.Reason,
                        summary: "Auth strength: PasswordStepUp.",
                        cancellationToken: ct).ConfigureAwait(false);
                }

                return PlatformApiResults.FromResult(result, m => Results.Ok(MembershipQueryService.Map(m)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        return app.MapMembershipBranchAssignmentEndpoints();
    }

    private static IEndpointRouteBuilder MapMembershipBranchAssignmentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/platform/organizations/{organizationId:guid}/members/{membershipId:guid}/branch-assignments", async (
            Guid organizationId,
            Guid membershipId,
            ListMembershipBranchAssignments useCase,
            PlatformMembershipAuthz membershipAuthz,
            PlatformAuthz platformAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.PlatformAccessChecked,
                nameof(OrganizationMembership),
                membershipId.ToString("D"),
                organizationId,
                summary: "List staff branch assignments.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = platformAuthz.CurrentActor.PlatformUserId;
            if (actor is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.SessionInvalid,
                    "Authenticated Platform user is required.",
                    StatusCodes.Status401Unauthorized);
            }

            return PlatformApiResults.FromResult(
                await useCase.ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    OrganizationMembershipId.From(membershipId),
                    actor,
                    ct).ConfigureAwait(false),
                dto => Results.Ok(dto));
        });

        app.MapPut("/api/v1/platform/organizations/{organizationId:guid}/members/{membershipId:guid}/branch-assignments", async (
            Guid organizationId,
            Guid membershipId,
            SetMembershipBranchAssignmentsRequest body,
            SetMembershipBranchAssignments useCase,
            PlatformMembershipAuthz membershipAuthz,
            PlatformAuthz platformAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.MembershipBranchAssignmentsUpdated,
                nameof(OrganizationMembership),
                membershipId.ToString("D"),
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = platformAuthz.CurrentActor;
            var result = await useCase.ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    OrganizationMembershipId.From(membershipId),
                    new SetMembershipBranchAssignmentsCommand(body.Scope ?? string.Empty, body.BranchIds, body.AreaIds),
                    actor.PlatformUserId?.Value.ToString("D"),
                    ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                var branchCount = body.BranchIds?.Count ?? 0;
                await membershipAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.MembershipBranchAssignmentsUpdated,
                    nameof(OrganizationMembership),
                    membershipId.ToString("D"),
                    organizationId,
                    summary: $"Updated branch access (scope={body.Scope}, branches={branchCount}).",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(
                result,
                dto => Results.Ok(dto));
        });

        return app;
    }

    private static bool TryParseRole(string? role, out OrganizationRole parsed, out IResult? error)
    {
        parsed = default;
        error = null;
        if (string.IsNullOrWhiteSpace(role) || !Enum.TryParse(role, ignoreCase: true, out parsed))
        {
            error = PlatformApiResults.Problem(
                DomainErrorCodes.InvalidOrganizationRole,
                "Role must be OrganizationOwner (Owner) or OrganizationMember (Staff). OrganizationAdministrator is legacy-only.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        return true;
    }

    private static bool TryParseMembershipStatus(string? status, out MembershipStatus? parsed, out IResult? error)
    {
        parsed = null;
        error = null;
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        if (!Enum.TryParse<MembershipStatus>(status, ignoreCase: true, out var value))
        {
            error = PlatformApiResults.Problem(
                DomainErrorCodes.InvalidMembershipStatusTransition,
                $"Unrecognized membership status '{status}'.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = value;
        return true;
    }
}

internal sealed record AddMemberRequest(Guid UserId, string Role, string? Reason = null);
internal sealed record ChangeRoleRequest(string Role, string? ActorReference, string? StepUpToken);
internal sealed record MembershipLifecycleRequest(string? Reason, string? ActorReference, string? StepUpToken);
internal sealed record SetMembershipBranchAssignmentsRequest(
    string? Scope,
    IReadOnlyList<Guid>? BranchIds,
    IReadOnlyList<Guid>? AreaIds = null);
