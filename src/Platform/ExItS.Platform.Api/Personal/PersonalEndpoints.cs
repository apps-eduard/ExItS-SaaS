using System.Security.Claims;
using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Api.Personal;

/// <summary>
/// Personal Scope API surface (P16-WP04/WP05/WP06).
/// </summary>
internal static class PersonalEndpoints
{
    public static IEndpointRouteBuilder MapPersonalEndpoints(this IEndpointRouteBuilder app)
    {
        var personal = app.MapGroup("/api/v1/personal");

        personal.MapGet("/me", (HttpContext http) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out var accountProfileId, out var accountClass, out var allowedScope, out var unauthorized))
            {
                return unauthorized!;
            }

            return Results.Ok(new
            {
                userIdentityId = userId,
                accountProfileId,
                accountClass,
                allowedScope,
                scope = "Personal"
            });
        });

        personal.MapGet("/health", () => Results.Ok(new { status = "Healthy", scope = "Personal" }));

        personal.MapGet("/dashboard", async (
            HttpContext http,
            GetPersonalDashboard getDashboard,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out var accountProfileId, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await getDashboard.ExecuteAsync(
                PlatformUserId.From(userId),
                AccountProfileId.From(accountProfileId),
                ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        personal.MapGet("/profile", async (
            HttpContext http,
            GetPersonalProfile getProfile,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out var accountProfileId, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await getProfile.ExecuteAsync(
                PlatformUserId.From(userId),
                AccountProfileId.From(accountProfileId),
                ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        personal.MapGet("/settings", async (
            HttpContext http,
            GetPersonalAccountSettings getSettings,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await getSettings.ExecuteAsync(PlatformUserId.From(userId), ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        personal.MapPut("/settings", async (
            HttpContext http,
            UpdatePersonalAccountSettingsRequest body,
            UpdatePersonalAccountSettings updateSettings,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await updateSettings.ExecuteAsync(PlatformUserId.From(userId), body, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        personal.MapPost("/start-business", async (
            HttpContext http,
            StartBusinessRequest body,
            StartBusinessForPersonalUser startBusiness,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var sessionIdRaw = http.User.FindFirstValue(PlatformSessionClaimTypes.SessionId);
            if (!Guid.TryParse(sessionIdRaw, out var sessionId) || sessionId == Guid.Empty)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.SessionInvalid,
                    "Session is invalid.",
                    StatusCodes.Status401Unauthorized);
            }

            var result = await startBusiness.ExecuteAsync(
                PlatformUserId.From(userId),
                PlatformAuthSessionId.From(sessionId),
                body,
                http.Connection.RemoteIpAddress?.ToString(),
                http.Request.Headers.UserAgent.ToString(),
                ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Created($"/api/v1/organizations/{dto.OrganizationId}", dto));
        });

        // Active Platform Business Types for Start Business (Personal scope; no org entitlement filter).
        personal.MapGet("/onboarding/business-types", async (
            HttpContext http,
            BusinessTypeQueryService queries,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out _, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var items = await queries.ListActiveForMerchantsAsync(ct).ConfigureAwait(false);
            return Results.Ok(items);
        });

        MapPersonalUtangEndpoints(personal);
        MapPersonalNotificationEndpoints(personal);

        return app;
    }

    private static void MapPersonalUtangEndpoints(RouteGroupBuilder personal)
    {
        var utang = personal.MapGroup("/utang");

        utang.MapPost("/contacts", async (
            HttpContext http,
            CreatePersonalContactRequest body,
            CreatePersonalContact createContact,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await createContact.ExecuteAsync(PlatformUserId.From(userId), body, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Created($"/api/v1/personal/utang/contacts/{dto.Id}", dto));
        });

        utang.MapGet("/contacts", async (
            HttpContext http,
            ListPersonalContacts listContacts,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var list = await listContacts.ExecuteAsync(PlatformUserId.From(userId), ct).ConfigureAwait(false);
            return Results.Ok(list);
        });

        utang.MapPost("/relationships", async (
            HttpContext http,
            CreatePersonalDebtRelationshipRequest body,
            CreatePersonalDebtRelationship createRelationship,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await createRelationship.ExecuteAsync(PlatformUserId.From(userId), body, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(
                result,
                dto => Results.Created($"/api/v1/personal/utang/relationships/{dto.Id}", dto));
        });

        utang.MapGet("/relationships/lent", async (
            HttpContext http,
            ListPersonalUtangRelationships listRelationships,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var list = await listRelationships.ExecuteAsync(PlatformUserId.From(userId), "lent", ct)
                .ConfigureAwait(false);
            return Results.Ok(list);
        });

        utang.MapGet("/relationships/borrowed", async (
            HttpContext http,
            ListPersonalUtangRelationships listRelationships,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var list = await listRelationships.ExecuteAsync(PlatformUserId.From(userId), "borrowed", ct)
                .ConfigureAwait(false);
            return Results.Ok(list);
        });

        utang.MapGet("/relationships/{relationshipId:guid}", async (
            HttpContext http,
            Guid relationshipId,
            GetPersonalUtangRelationship getRelationship,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await getRelationship.ExecuteAsync(PlatformUserId.From(userId), relationshipId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        utang.MapGet("/relationships/{relationshipId:guid}/balance", async (
            HttpContext http,
            Guid relationshipId,
            GetPersonalUtangBalance getBalance,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await getBalance.ExecuteAsync(PlatformUserId.From(userId), relationshipId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        utang.MapGet("/relationships/{relationshipId:guid}/history", async (
            HttpContext http,
            Guid relationshipId,
            ListPersonalUtangHistory listHistory,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await listHistory.ExecuteAsync(PlatformUserId.From(userId), relationshipId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        utang.MapPost("/relationships/{relationshipId:guid}/entries", async (
            HttpContext http,
            Guid relationshipId,
            RecordPersonalUtangEntryRequest body,
            RecordPersonalUtangEntry recordEntry,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await recordEntry.ExecuteAsync(PlatformUserId.From(userId), relationshipId, body, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(
                result,
                dto => Results.Created(
                    $"/api/v1/personal/utang/relationships/{relationshipId}/history",
                    dto));
        });

        utang.MapPost("/relationships/{relationshipId:guid}/invitations", async (
            HttpContext http,
            Guid relationshipId,
            CreatePersonalUtangInvitationRequest body,
            CreatePersonalUtangInvitation createInvitation,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await createInvitation.ExecuteAsync(PlatformUserId.From(userId), relationshipId, body, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(
                result,
                dto => Results.Created($"/api/v1/personal/utang/invitations/{dto.Id}", dto));
        });

        utang.MapGet("/invitations", async (
            HttpContext http,
            ListPersonalUtangInvitations listInvitations,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var list = await listInvitations.ExecuteAsync(PlatformUserId.From(userId), ct).ConfigureAwait(false);
            return Results.Ok(list);
        });

        utang.MapPost("/invitations/accept", async (
            HttpContext http,
            AcceptPersonalUtangInvitationRequest body,
            AcceptPersonalUtangInvitation acceptInvitation,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await acceptInvitation.ExecuteAsync(PlatformUserId.From(userId), body, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        utang.MapPost("/invitations/decline", async (
            HttpContext http,
            AcceptPersonalUtangInvitationRequest body,
            DeclinePersonalUtangInvitation declineInvitation,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await declineInvitation.ExecuteAsync(PlatformUserId.From(userId), body.Token, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        utang.MapPost("/invitations/{invitationId:guid}/resend", async (
            HttpContext http,
            Guid invitationId,
            ResendPersonalUtangInvitation resendInvitation,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await resendInvitation.ExecuteAsync(PlatformUserId.From(userId), invitationId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        utang.MapPost("/invitations/{invitationId:guid}/revoke", async (
            HttpContext http,
            Guid invitationId,
            RevokePersonalUtangInvitation revokeInvitation,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await revokeInvitation.ExecuteAsync(PlatformUserId.From(userId), invitationId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        utang.MapPost("/relationships/{relationshipId:guid}/reminders", async (
            HttpContext http,
            Guid relationshipId,
            CreatePersonalReminderRequest body,
            CreatePersonalReminder createReminder,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await createReminder.ExecuteAsync(PlatformUserId.From(userId), relationshipId, body, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(
                result,
                dto => Results.Created($"/api/v1/personal/utang/reminders/{dto.Id}", dto));
        });

        utang.MapGet("/relationships/{relationshipId:guid}/reminders", async (
            HttpContext http,
            Guid relationshipId,
            ListPersonalReminders listReminders,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await listReminders.ExecuteAsync(PlatformUserId.From(userId), relationshipId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        utang.MapGet("/reminders/due", async (
            HttpContext http,
            ListDuePersonalReminders listDue,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out _, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var list = await listDue.ExecuteAsync(cancellationToken: ct).ConfigureAwait(false);
            return Results.Ok(list);
        });

        utang.MapPost("/reminders/{reminderId:guid}/deliver", async (
            HttpContext http,
            Guid reminderId,
            DeliverPersonalReminder deliverReminder,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await deliverReminder.ExecuteAsync(PlatformUserId.From(userId), reminderId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        utang.MapPost("/reminders/{reminderId:guid}/cancel", async (
            HttpContext http,
            Guid reminderId,
            CancelPersonalReminder cancelReminder,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await cancelReminder.ExecuteAsync(PlatformUserId.From(userId), reminderId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        utang.MapGet("/delivery-audit", async (
            HttpContext http,
            Guid? reminderId,
            ListPersonalNotificationDeliveries listDeliveries,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await listDeliveries.ExecuteAsync(PlatformUserId.From(userId), reminderId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });
    }

    private static void MapPersonalNotificationEndpoints(RouteGroupBuilder personal)
    {
        personal.MapGet("/notifications", async (
            HttpContext http,
            ListPersonalInAppNotifications listNotifications,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var list = await listNotifications.ExecuteAsync(PlatformUserId.From(userId), ct).ConfigureAwait(false);
            return Results.Ok(list);
        });

        personal.MapPost("/notifications/{notificationId:guid}/read", async (
            HttpContext http,
            Guid notificationId,
            MarkPersonalInAppNotificationRead markRead,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await markRead.ExecuteAsync(PlatformUserId.From(userId), notificationId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });
    }

    private static bool TryGetPersonalContext(
        HttpContext http,
        out Guid userId,
        out Guid accountProfileId,
        out string? accountClass,
        out string? allowedScope,
        out IResult? unauthorized)
    {
        userId = Guid.Empty;
        accountProfileId = Guid.Empty;
        accountClass = null;
        allowedScope = null;
        unauthorized = null;

        if (!TryGetUserId(http, out userId))
        {
            unauthorized = PlatformApiResults.Problem(
                ApplicationErrorCodes.SessionInvalid,
                "Authentication is required.",
                StatusCodes.Status401Unauthorized);
            return false;
        }

        accountClass = http.User.FindFirstValue(PlatformSessionClaimTypes.AccountClass);
        accountProfileId = Guid.TryParse(
            http.User.FindFirstValue(PlatformSessionClaimTypes.AccountProfileId),
            out var profileId)
            ? profileId
            : Guid.Empty;
        allowedScope = http.User.FindFirstValue(PlatformSessionClaimTypes.AllowedScope);

        if (accountProfileId == Guid.Empty)
        {
            unauthorized = PlatformApiResults.Problem(
                ApplicationErrorCodes.SessionInvalid,
                "Personal account profile is required.",
                StatusCodes.Status401Unauthorized);
            return false;
        }

        return true;
    }

    private static bool TryGetUserId(HttpContext http, out Guid userId)
    {
        userId = Guid.Empty;
        var raw = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId) && userId != Guid.Empty;
    }
}
