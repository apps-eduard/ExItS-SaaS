using System.Security.Claims;
using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Organizations;

/// <summary>
/// Organization Staff Invitation lifecycle (<see cref="InvitationKinds.OrganizationStaffInvitation"/>).
/// Tokens are returned once on create/resend for delivery channels; list/get DTOs never include token hashes.
/// Accept (token + password) creates an org-scoped staff identity, membership, and optional product role —
/// never Business Customer or Customer Link.
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
            CreateOrganizationInvitationForPersonal nativeInvite,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
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

            ApplicationResult<OrganizationInvitationDto> result;
            var publicTarget = body.PublicUserIdOrQrPayload?.Trim();
            if (!string.IsNullOrWhiteSpace(publicTarget))
            {
                if (authority.ActorMembershipRole != OrganizationRole.OrganizationOwner)
                {
                    return PlatformApiResults.Problem(
                        DomainErrorCodes.AuthorizationDenied,
                        "Only the Organization Owner can invite staff.",
                        StatusCodes.Status403Forbidden);
                }

                result = await nativeInvite
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        publicTarget,
                        membershipAuthz.Inner.CurrentActor.PlatformUserId,
                        body.ProductRole,
                        body.Branch,
                        ct)
                    .ConfigureAwait(false);
            }
            else
            {
                if (!TryParseRole(body.Role, out var role, out var error))
                {
                    return error!;
                }

                result = await useCase
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
                        body.RequireEmailVerification ?? true,
                        ct)
                    .ConfigureAwait(false);
            }
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

        app.MapPost("/api/v1/platform/organizations/{organizationId:guid}/invitations/resolve-target", async (
            Guid organizationId,
            ResolveStaffInviteTargetRequest body,
            ResolveStaffInviteTarget useCase,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.PlatformAccessChecked,
                nameof(OrganizationInvitation),
                organizationId.ToString("D"),
                organizationId,
                summary: "Resolve staff invite target.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var authority = await membershipAuthz
                .ResolveActorMembershipAuthorityAsync(organizationId, ct)
                .ConfigureAwait(false);
            if (authority.ActorMembershipRole != OrganizationRole.OrganizationOwner)
            {
                return PlatformApiResults.Problem(
                    DomainErrorCodes.AuthorizationDenied,
                    "Only the Organization Owner can invite staff.",
                    StatusCodes.Status403Forbidden);
            }

            var result = await useCase.ExecuteAsync(body.Input ?? string.Empty, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        app.MapGet("/api/v1/platform/invitations/my-pending", async (
            ListPendingOrganizationInvitationsForPersonalUser useCase,
            HttpContext http,
            CancellationToken ct) =>
        {
            if (!TryGetAuthenticatedUserId(http, out var userId))
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.SessionInvalid,
                    "Authentication is required.",
                    StatusCodes.Status401Unauthorized);
            }

            var result = await useCase
                .ExecuteAsync(PlatformUserId.From(userId), ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        app.MapPost("/api/v1/platform/invitations/{invitationId:guid}/decline", async (
            Guid invitationId,
            DeclineOrganizationInvitationForPersonal useCase,
            HttpContext http,
            CancellationToken ct) =>
        {
            if (!TryGetAuthenticatedUserId(http, out var userId))
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.SessionInvalid,
                    "Authentication is required.",
                    StatusCodes.Status401Unauthorized);
            }

            var result = await useCase
                .ExecuteAsync(OrganizationInvitationId.From(invitationId), PlatformUserId.From(userId), ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto with { AcceptToken = null }));
        });

        app.MapPost("/api/v1/platform/invitations/{invitationId:guid}/accept-as-personal", async (
            Guid invitationId,
            AcceptInvitationByIdRequest body,
            AcceptOrganizationInvitation useCase,
            HttpContext http,
            CancellationToken ct) =>
        {
            if (!TryGetAuthenticatedUserId(http, out var userId))
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.SessionInvalid,
                    "Authentication is required.",
                    StatusCodes.Status401Unauthorized);
            }

            var result = await useCase
                .ExecuteAcceptByIdForPersonalAsync(
                    PlatformUserId.From(userId),
                    OrganizationInvitationId.From(invitationId),
                    body.Password ?? string.Empty,
                    body.DisplayName,
                    body.FirstName,
                    body.LastName,
                    ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
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
            CancellationToken ct) =>
        {
            var result = await useCase
                .ExecuteAsync(
                    body.Token ?? string.Empty,
                    body.Password ?? string.Empty,
                    body.DisplayName,
                    body.FirstName,
                    body.LastName,
                    ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        })
        .AllowAnonymous();

        app.MapPost("/api/v1/platform/invitations/accept-as-personal", async (
            AcceptInvitationRequest body,
            AcceptOrganizationInvitation useCase,
            HttpContext http,
            CancellationToken ct) =>
        {
            if (!TryGetAuthenticatedUserId(http, out var userId))
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.SessionInvalid,
                    "Authentication is required.",
                    StatusCodes.Status401Unauthorized);
            }

            var result = await useCase
                .ExecuteForAuthenticatedPersonalAsync(
                    PlatformUserId.From(userId),
                    body.Token ?? string.Empty,
                    body.Password ?? string.Empty,
                    body.DisplayName,
                    body.FirstName,
                    body.LastName,
                    ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
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

    private static bool TryGetAuthenticatedUserId(HttpContext http, out Guid userId)
    {
        userId = Guid.Empty;
        if (http.User.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var raw = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId) && userId != Guid.Empty;
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
    bool? RequireEmailVerification = null,
    string? PublicUserIdOrQrPayload = null);
internal sealed record ResolveStaffInviteTargetRequest(string? Input);
internal sealed record AcceptInvitationRequest(
    string? Token,
    string? Password,
    string? DisplayName = null,
    string? FirstName = null,
    string? LastName = null);
internal sealed record AcceptInvitationByIdRequest(
    string? Password,
    string? DisplayName = null,
    string? FirstName = null,
    string? LastName = null);
