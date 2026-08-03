using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Organizations;

/// <summary>
/// Organization Staff Invitation lifecycle (<see cref="InvitationKinds.OrganizationStaffInvitation"/>).
/// Tokens are returned once on create/resend for delivery channels; list/get DTOs never include token hashes.
/// Accept creates Organization membership + staff role only — never Business Customer or Customer Link.
/// </summary>
internal static class InvitationEndpoints
{
    public static IEndpointRouteBuilder MapInvitationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/platform/organizations/{organizationId:guid}/invitations", async (
            Guid organizationId,
            string? status,
            int? page,
            int? pageSize,
            OrganizationInvitationQueryService queries,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.PlatformAccessChecked,
                nameof(OrganizationInvitation),
                organizationId.ToString("D"),
                organizationId,
                summary: "List organization invitations.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            if (!TryParseInvitationStatus(status, out var parsed, out var error))
            {
                return error!;
            }

            var result = await queries
                .ListByOrganizationAsync(organizationId, parsed, page, pageSize, ct)
                .ConfigureAwait(false);
            // Never return accept tokens on list.
            var sanitized = result with
            {
                Items = result.Items.Select(i => i with { AcceptToken = null }).ToList()
            };
            return Results.Ok(sanitized);
        });

        app.MapPost("/api/v1/platform/organizations/{organizationId:guid}/invitations", async (
            Guid organizationId,
            CreateInvitationRequest body,
            CreateOrganizationInvitation useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            if (!TryParseRole(body.Role, out var role, out var error))
            {
                return error!;
            }

            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.InvitationCreated,
                nameof(OrganizationInvitation),
                organizationId.ToString("D"),
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var authority = await membershipAuthz
                .ResolveActorMembershipAuthorityAsync(organizationId, ct)
                .ConfigureAwait(false);

            var result = await useCase
                .ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    body.Email ?? string.Empty,
                    role,
                    membershipAuthz.Inner.CurrentActor.PlatformUserId,
                    authority.ActorMembershipRole,
                    authority.HasPlatformManageMemberships,
                    body.DisplayName,
                    body.FirstName,
                    body.LastName,
                    body.Phone,
                    body.EmployeeCode,
                    body.Branch,
                    body.ProductRole,
                    ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await membershipAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.InvitationCreated,
                    nameof(OrganizationInvitation),
                    result.Value!.Id.ToString("D"),
                    organizationId,
                    summary: "Created organization invitation.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(
                result,
                dto => Results.Created(
                    $"/api/v1/platform/organizations/{organizationId}/invitations/{dto.Id}",
                    dto));
        });

        app.MapPost("/api/v1/platform/invitations/{invitationId:guid}/resend", async (
            Guid invitationId,
            ResendOrganizationInvitation useCase,
            OrganizationInvitationQueryService queries,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var existing = await queries.GetByIdAsync(invitationId, ct).ConfigureAwait(false);
            if (existing is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.InvitationNotFound,
                    "Invitation was not found.",
                    StatusCodes.Status404NotFound);
            }

            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.InvitationResent,
                nameof(OrganizationInvitation),
                invitationId.ToString("D"),
                existing.OrganizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(OrganizationInvitationId.From(invitationId), ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await membershipAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.InvitationResent,
                    nameof(OrganizationInvitation),
                    invitationId.ToString("D"),
                    existing.OrganizationId,
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        app.MapPost("/api/v1/platform/invitations/{invitationId:guid}/revoke", async (
            Guid invitationId,
            RevokeOrganizationInvitation useCase,
            OrganizationInvitationQueryService queries,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var existing = await queries.GetByIdAsync(invitationId, ct).ConfigureAwait(false);
            if (existing is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.InvitationNotFound,
                    "Invitation was not found.",
                    StatusCodes.Status404NotFound);
            }

            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.InvitationRevoked,
                nameof(OrganizationInvitation),
                invitationId.ToString("D"),
                existing.OrganizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(OrganizationInvitationId.From(invitationId), ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await membershipAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.InvitationRevoked,
                    nameof(OrganizationInvitation),
                    invitationId.ToString("D"),
                    existing.OrganizationId,
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto with { AcceptToken = null }));
        });

        app.MapPost("/api/v1/platform/invitations/accept", async (
            AcceptInvitationRequest body,
            AcceptOrganizationInvitation useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var actor = membershipAuthz.Inner.CurrentActor;
            if (actor.PlatformUserId is null)
            {
                return PlatformApiResults.Problem(
                    DomainErrorCodes.AuthorizationDenied,
                    "Accepting an invitation requires an authenticated Platform User.",
                    StatusCodes.Status401Unauthorized);
            }

            var result = await useCase
                .ExecuteAsync(body.Token ?? string.Empty, actor.PlatformUserId, ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await membershipAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.InvitationAccepted,
                    nameof(OrganizationInvitation),
                    result.Value!.Id.Value.ToString("D"),
                    result.Value.OrganizationId.Value,
                    summary: "Accepted organization invitation.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, m => Results.Ok(MembershipQueryService.Map(m)));
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

    private static bool TryParseInvitationStatus(string? status, out InvitationStatus? parsed, out IResult? error)
    {
        parsed = null;
        error = null;
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        if (!Enum.TryParse<InvitationStatus>(status, ignoreCase: true, out var value))
        {
            error = PlatformApiResults.Problem(
                DomainErrorCodes.InvalidInvitationStatusTransition,
                $"Unrecognized invitation status '{status}'.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = value;
        return true;
    }
}

internal sealed record CreateInvitationRequest(
    string? Email,
    string? Role,
    string? FirstName = null,
    string? LastName = null,
    string? DisplayName = null,
    string? Phone = null,
    string? EmployeeCode = null,
    string? Branch = null,
    string? ProductRole = null,
    bool? RequireEmailVerification = null);
internal sealed record AcceptInvitationRequest(string? Token);
