using System.Security.Claims;
using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Personal;

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

        personal.MapGet("/linked-merchants", async (
            HttpContext http,
            int? page,
            int? pageSize,
            ListLinkedMerchantsForPersonalUser listMerchants,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await listMerchants
                .ExecuteAsync(PlatformUserId.From(userId), page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        personal.MapGet("/linked-merchants/{organizationId:guid}/ordering-capability", async (
            HttpContext http,
            Guid organizationId,
            GetLinkedMerchantOrderingCapability capability,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await capability
                .ExecuteAsync(PlatformUserId.From(userId), organizationId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        personal.MapGet("/linked-merchants/authorization", async (
            HttpContext http,
            Guid? organizationId,
            Guid? businessCustomerId,
            AuthorizeLinkedCustomerAccess authorize,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out var accountClassRaw, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            if (organizationId is not Guid orgId || orgId == Guid.Empty
                || businessCustomerId is not Guid customerId || customerId == Guid.Empty)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.LinkedCustomerAppUserNotFound,
                    "Linked customer was not found.",
                    StatusCodes.Status404NotFound);
            }

            var accountClass = Enum.TryParse<AccountClass>(accountClassRaw, ignoreCase: true, out var parsed)
                ? parsed
                : AccountClass.Platform;

            var result = await authorize
                .ExecuteAsync(
                    PlatformUserId.From(userId),
                    accountClass,
                    PlatformOrganizationId.From(orgId),
                    BusinessCustomerId.From(customerId),
                    ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        personal.MapPost("/linked-merchants/{linkedCustomerId:guid}/unlink", async (
            HttpContext http,
            Guid linkedCustomerId,
            UnlinkAcceptedCustomerLink unlink,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await unlink
                .ExecuteForOwnerAsync(
                    LinkedCustomerAppUserId.From(linkedCustomerId),
                    PlatformUserId.From(userId),
                    ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        personal.MapGet("/customer-link-requests", async (
            HttpContext http,
            ListPendingCustomerLinkRequestsForPersonalUser listRequests,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out var accountClassRaw, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            if (!TryRequirePersonalAccountClass(accountClassRaw, out var accountClass, out var scopeDenied))
            {
                return scopeDenied!;
            }

            var result = await listRequests
                .ExecuteAsync(PlatformUserId.From(userId), accountClass, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        personal.MapPost("/customer-link-requests/{requestId:guid}/accept", async (
            HttpContext http,
            Guid requestId,
            AcceptCustomerLinkRequestById acceptById,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out var accountClassRaw, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            if (!TryRequirePersonalAccountClass(accountClassRaw, out var accountClass, out var scopeDenied))
            {
                return scopeDenied!;
            }

            var result = await acceptById
                .ExecuteAsync(
                    CustomerLinkRequestId.From(requestId),
                    PlatformUserId.From(userId),
                    accountClass,
                    ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        personal.MapPost("/customer-link-requests/{requestId:guid}/decline", async (
            HttpContext http,
            Guid requestId,
            DeclineCustomerLinkRequestById declineById,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out var accountClassRaw, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            if (!TryRequirePersonalAccountClass(accountClassRaw, out var accountClass, out var scopeDenied))
            {
                return scopeDenied!;
            }

            var result = await declineById
                .ExecuteAsync(
                    CustomerLinkRequestId.From(requestId),
                    PlatformUserId.From(userId),
                    accountClass,
                    ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(
                result,
                dto => Results.Ok(dto with { AcceptToken = null }));
        });

        personal.MapPost("/customer-link-requests/{requestId:guid}/block-business", async (
            HttpContext http,
            Guid requestId,
            BlockBusinessFromCustomerLinkRequest blockBusiness,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out var accountClassRaw, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            if (!TryRequirePersonalAccountClass(accountClassRaw, out _, out var scopeDenied))
            {
                return scopeDenied!;
            }

            var result = await blockBusiness
                .ExecuteAsync(CustomerLinkRequestId.From(requestId), PlatformUserId.From(userId), ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        personal.MapGet("/customer-link-requests/history", async (
            HttpContext http,
            ListResolvedCustomerLinkRequestsForPersonalUser listHistory,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out var accountClassRaw, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            if (!TryRequirePersonalAccountClass(accountClassRaw, out var accountClass, out var scopeDenied))
            {
                return scopeDenied!;
            }

            var result = await listHistory
                .ExecuteAsync(PlatformUserId.From(userId), accountClass, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        personal.MapGet("/blocked-businesses", async (
            HttpContext http,
            ListPersonalBlockedBusinesses listBlocked,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out var accountClassRaw, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            if (!TryRequirePersonalAccountClass(accountClassRaw, out _, out var scopeDenied))
            {
                return scopeDenied!;
            }

            var list = await listBlocked
                .ExecuteAsync(PlatformUserId.From(userId), ct)
                .ConfigureAwait(false);
            return Results.Ok(list);
        });

        personal.MapPost("/blocked-businesses/{organizationId:guid}/unblock", async (
            HttpContext http,
            Guid organizationId,
            UnblockPersonalOrganizationConnection unblock,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out var accountClassRaw, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            if (!TryRequirePersonalAccountClass(accountClassRaw, out _, out var scopeDenied))
            {
                return scopeDenied!;
            }

            var result = await unblock
                .ExecuteAsync(
                    PlatformUserId.From(userId),
                    PlatformOrganizationId.From(organizationId),
                    ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        personal.MapPost("/linked-merchants/by-organization/{organizationId:guid}/disconnect", async (
            HttpContext http,
            Guid organizationId,
            DisconnectPersonalLinkedMerchant disconnect,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out var accountClassRaw, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            if (!TryRequirePersonalAccountClass(accountClassRaw, out _, out var scopeDenied))
            {
                return scopeDenied!;
            }

            var result = await disconnect
                .ExecuteAsync(
                    PlatformUserId.From(userId),
                    PlatformOrganizationId.From(organizationId),
                    ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        personal.MapPost("/linked-merchants/by-organization/{organizationId:guid}/disconnect-and-block", async (
            HttpContext http,
            Guid organizationId,
            DisconnectPersonalLinkedMerchant disconnect,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out var accountClassRaw, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            if (!TryRequirePersonalAccountClass(accountClassRaw, out _, out var scopeDenied))
            {
                return scopeDenied!;
            }

            var result = await disconnect
                .ExecuteAndBlockAsync(
                    PlatformUserId.From(userId),
                    PlatformOrganizationId.From(organizationId),
                    ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        // WP06: Personal feature entitlement check for POS history APIs (session-bound; not self-grant).
        personal.MapGet("/features/{featureCode}/active", async (
            HttpContext http,
            string featureCode,
            GetPersonalFeatureActiveStatus getActive,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await getActive
                .ExecuteAsync(PlatformUserId.From(userId), featureCode, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        // WP07: redeem eligible personal features with reward points (session PersonalUserId only).
        personal.MapPost("/features/{featureCode}/redeem", async (
            HttpContext http,
            string featureCode,
            RedeemPersonalFeatureWithRewardPoints redeem,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await redeem
                .ExecuteAsync(PlatformUserId.From(userId), featureCode, organizationId: null, cancellationToken: ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        personal.MapGet("/reward-points/balance", async (
            HttpContext http,
            GetPersonalRewardPointsBalance getBalance,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await getBalance
                .ExecuteAsync(PlatformUserId.From(userId), ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        personal.MapGet("/reward-points/activity", async (
            HttpContext http,
            int? page,
            int? pageSize,
            ListPersonalRewardPointsActivity listActivity,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await listActivity
                .ExecuteAsync(PlatformUserId.From(userId), page, pageSize, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        // WP09: server-side ads eligibility (Ad-Free authoritative; no fake playback).
        personal.MapGet("/ads/eligibility", async (
            HttpContext http,
            GetPersonalAdEligibility getEligibility,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await getEligibility
                .ExecuteAsync(PlatformUserId.From(userId), organizationId: null, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        // WP08: idempotent AdReward claim (server-side points; null provider does not fabricate success).
        personal.MapPost("/reward-points/ad-claims", async (
            HttpContext http,
            ClaimPersonalAdRewardRequest body,
            ClaimPersonalAdReward claimAdReward,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await claimAdReward
                .ExecuteAsync(PlatformUserId.From(userId), body.ClaimKey, organizationId: null, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
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

        personal.MapPut("/profile", async (
            HttpContext http,
            UpdatePersonalProfileRequest body,
            UpdatePersonalProfile updateProfile,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out var accountProfileId, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await updateProfile.ExecuteAsync(
                PlatformUserId.From(userId),
                AccountProfileId.From(accountProfileId),
                body,
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
        MapPersonalConnectionEndpoints(personal);
        MapPersonalNotificationEndpoints(personal);
        MapPersonalTodoEndpoints(personal);

        return app;
    }

    private static void MapPersonalTodoEndpoints(RouteGroupBuilder personal)
    {
        var todos = personal.MapGroup("/todos");

        todos.MapGet("/", async (
            HttpContext http,
            string? status,
            ListPersonalTodos listTodos,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out var accountClassRaw, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            if (!TryRequirePersonalAccountClass(accountClassRaw, out _, out var scopeDenied))
            {
                return scopeDenied!;
            }

            if (!TryParsePersonalTodoListStatus(status, out var statusFilter, out var invalidStatus))
            {
                return invalidStatus!;
            }

            var result = await listTodos
                .ExecuteAsync(PlatformUserId.From(userId), statusFilter, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        todos.MapGet("/{todoId:guid}", async (
            HttpContext http,
            Guid todoId,
            GetPersonalTodo getTodo,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out var accountClassRaw, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            if (!TryRequirePersonalAccountClass(accountClassRaw, out _, out var scopeDenied))
            {
                return scopeDenied!;
            }

            var result = await getTodo.ExecuteAsync(PlatformUserId.From(userId), todoId, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        todos.MapPost("/", async (
            HttpContext http,
            CreatePersonalTodoRequest body,
            CreatePersonalTodo createTodo,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out var accountClassRaw, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            if (!TryRequirePersonalAccountClass(accountClassRaw, out _, out var scopeDenied))
            {
                return scopeDenied!;
            }

            var result = await createTodo.ExecuteAsync(PlatformUserId.From(userId), body, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(
                result,
                dto => Results.Created($"/api/v1/personal/todos/{dto.Id}", dto));
        });

        todos.MapPut("/{todoId:guid}", async (
            HttpContext http,
            Guid todoId,
            UpdatePersonalTodoRequest body,
            UpdatePersonalTodo updateTodo,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out var accountClassRaw, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            if (!TryRequirePersonalAccountClass(accountClassRaw, out _, out var scopeDenied))
            {
                return scopeDenied!;
            }

            var result = await updateTodo.ExecuteAsync(PlatformUserId.From(userId), todoId, body, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        todos.MapMethods("/{todoId:guid}", new[] { "PATCH" }, async (
            HttpContext http,
            Guid todoId,
            UpdatePersonalTodoRequest body,
            UpdatePersonalTodo updateTodo,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out var accountClassRaw, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            if (!TryRequirePersonalAccountClass(accountClassRaw, out _, out var scopeDenied))
            {
                return scopeDenied!;
            }

            var result = await updateTodo.ExecuteAsync(PlatformUserId.From(userId), todoId, body, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        todos.MapPost("/{todoId:guid}/complete", async (
            HttpContext http,
            Guid todoId,
            PersonalTodoVersionRequest? body,
            CompletePersonalTodo completeTodo,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out var accountClassRaw, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            if (!TryRequirePersonalAccountClass(accountClassRaw, out _, out var scopeDenied))
            {
                return scopeDenied!;
            }

            var result = await completeTodo
                .ExecuteAsync(PlatformUserId.From(userId), todoId, body?.ExpectedVersion, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        todos.MapPost("/{todoId:guid}/reopen", async (
            HttpContext http,
            Guid todoId,
            PersonalTodoVersionRequest? body,
            ReopenPersonalTodo reopenTodo,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out var accountClassRaw, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            if (!TryRequirePersonalAccountClass(accountClassRaw, out _, out var scopeDenied))
            {
                return scopeDenied!;
            }

            var result = await reopenTodo
                .ExecuteAsync(PlatformUserId.From(userId), todoId, body?.ExpectedVersion, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        todos.MapPost("/{todoId:guid}/cancel", async (
            HttpContext http,
            Guid todoId,
            PersonalTodoVersionRequest? body,
            CancelPersonalTodo cancelTodo,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out var accountClassRaw, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            if (!TryRequirePersonalAccountClass(accountClassRaw, out _, out var scopeDenied))
            {
                return scopeDenied!;
            }

            var result = await cancelTodo
                .ExecuteAsync(PlatformUserId.From(userId), todoId, body?.ExpectedVersion, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });
    }

    private static void MapPersonalConnectionEndpoints(RouteGroupBuilder personal)
    {
        personal.MapGet("/connections", async (
            HttpContext http,
            ListPersonalConnectionRequests listRequests,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var list = await listRequests.ExecuteAsync(PlatformUserId.From(userId), ct).ConfigureAwait(false);
            return Results.Ok(list);
        });

        personal.MapPost("/people/{contactId:guid}/connection-request", async (
            HttpContext http,
            Guid contactId,
            RequestPersonalConnection requestConnection,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await requestConnection
                .ExecuteAsync(PlatformUserId.From(userId), contactId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(
                result,
                dto => Results.Created($"/api/v1/personal/connections/{dto.Id}", dto));
        });

        personal.MapPost("/connections/{requestId:guid}/accept", async (
            HttpContext http,
            Guid requestId,
            AcceptPersonalConnectionRequest accept,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await accept.ExecuteAsync(PlatformUserId.From(userId), requestId, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        personal.MapPost("/connections/{requestId:guid}/decline", async (
            HttpContext http,
            Guid requestId,
            DeclinePersonalConnectionRequest decline,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await decline.ExecuteAsync(PlatformUserId.From(userId), requestId, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        personal.MapPost("/connections/{requestId:guid}/revoke", async (
            HttpContext http,
            Guid requestId,
            RevokePersonalConnectionRequest revoke,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await revoke.ExecuteAsync(PlatformUserId.From(userId), requestId, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        personal.MapPost("/people/{contactId:guid}/unlink", async (
            HttpContext http,
            Guid contactId,
            UnlinkPersonalContact unlink,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await unlink.ExecuteAsync(PlatformUserId.From(userId), contactId, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        personal.MapPost("/people/{contactId:guid}/block", async (
            HttpContext http,
            Guid contactId,
            BlockPersonalContact block,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await block.ExecuteAsync(PlatformUserId.From(userId), contactId, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        personal.MapPost("/people/{contactId:guid}/unblock", async (
            HttpContext http,
            Guid contactId,
            UnblockPersonalContact unblock,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await unblock.ExecuteAsync(PlatformUserId.From(userId), contactId, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });
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
        })
        .AddEndpointFilter<IdentifiedPersonalContactRateLimitFilter>();

        utang.MapPost("/contacts/{contactId:guid}/link", async (
            HttpContext http,
            Guid contactId,
            LinkPersonalContactRequest body,
            LinkPersonalContact linkContact,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await linkContact
                .ExecuteAsync(PlatformUserId.From(userId), contactId, body, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        utang.MapPut("/contacts/{contactId:guid}", async (
            HttpContext http,
            Guid contactId,
            UpdatePersonalContactRequest body,
            UpdatePersonalContact updateContact,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await updateContact
                .ExecuteAsync(PlatformUserId.From(userId), contactId, body, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
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

        utang.MapGet("/contacts/{contactId:guid}", async (
            HttpContext http,
            Guid contactId,
            GetPersonalContact getContact,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await getContact.ExecuteAsync(PlatformUserId.From(userId), contactId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
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

        utang.MapGet("/entries/{entryId:guid}", async (
            HttpContext http,
            Guid entryId,
            GetPersonalUtangEntry getEntry,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await getEntry.ExecuteAsync(PlatformUserId.From(userId), entryId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
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

        utang.MapPost("/relationships/{relationshipId:guid}/entries/{entryId:guid}/confirm", async (
            HttpContext http,
            Guid relationshipId,
            Guid entryId,
            ConfirmPersonalUtangEntryRequest body,
            ConfirmPersonalUtangEntry confirmEntry,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await confirmEntry
                .ExecuteAsync(PlatformUserId.From(userId), relationshipId, entryId, body, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        utang.MapPost("/relationships/{relationshipId:guid}/entries/{entryId:guid}/dispute", async (
            HttpContext http,
            Guid relationshipId,
            Guid entryId,
            DisputePersonalUtangEntryRequest body,
            DisputePersonalUtangEntry disputeEntry,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await disputeEntry
                .ExecuteAsync(PlatformUserId.From(userId), relationshipId, entryId, body, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        utang.MapPost("/relationships/{relationshipId:guid}/entries/{entryId:guid}/cancel", async (
            HttpContext http,
            Guid relationshipId,
            Guid entryId,
            CancelPersonalUtangEntryRequest body,
            CancelPersonalUtangEntry cancelEntry,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await cancelEntry
                .ExecuteAsync(PlatformUserId.From(userId), relationshipId, entryId, body, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
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

        utang.MapPost("/invitations/accept-by-id", async (
            HttpContext http,
            AcceptPersonalUtangInvitationByIdRequest body,
            AcceptPersonalUtangInvitationById acceptById,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await acceptById.ExecuteAsync(PlatformUserId.From(userId), body, ct)
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

        utang.MapPost("/invitations/decline-by-id", async (
            HttpContext http,
            DeclinePersonalUtangInvitationByIdRequest body,
            DeclinePersonalUtangInvitationById declineById,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var result = await declineById.ExecuteAsync(PlatformUserId.From(userId), body, ct)
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
            string? scope,
            int? page,
            int? pageSize,
            bool? unreadOnly,
            ListPersonalInAppNotifications listNotifications,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            if (!ListPersonalInAppNotifications.TryNormalizeScope(scope, out var normalizedScope, out var scopeError))
            {
                return PlatformApiResults.Problem(
                    DomainErrorCodes.InvalidPersonalNotificationId,
                    scopeError ?? "Notification scope must be 'recent' or 'archived'.",
                    StatusCodes.Status400BadRequest);
            }

            var user = PlatformUserId.From(userId);
            if (normalizedScope == PersonalNotificationArchiveRules.ScopeArchived)
            {
                var paged = await listNotifications
                    .ExecutePagedAsync(user, normalizedScope, page, pageSize, unreadOnly ?? false, ct)
                    .ConfigureAwait(false);
                return Results.Ok(paged);
            }

            // Recent inbox stays a plain array for existing MAUI / Blazor / Admin clients.
            if (unreadOnly == true || page is not null || pageSize is not null)
            {
                var pagedRecent = await listNotifications
                    .ExecutePagedAsync(user, normalizedScope, page, pageSize, unreadOnly ?? false, ct)
                    .ConfigureAwait(false);
                return Results.Ok(pagedRecent);
            }

            var list = await listNotifications.ExecuteAsync(user, ct).ConfigureAwait(false);
            return Results.Ok(list);
        });

        personal.MapGet("/notifications/unread-count", async (
            HttpContext http,
            CountPersonalInAppNotificationUnread countUnread,
            CancellationToken ct) =>
        {
            if (!TryGetPersonalContext(http, out var userId, out _, out _, out _, out var unauthorized))
            {
                return unauthorized!;
            }

            var dto = await countUnread.ExecuteAsync(PlatformUserId.From(userId), ct).ConfigureAwait(false);
            return Results.Ok(dto);
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

    private static bool TryParsePersonalTodoListStatus(
        string? statusRaw,
        out PersonalTodoStatus? statusFilter,
        out IResult? invalid)
    {
        statusFilter = null;
        invalid = null;

        if (string.IsNullOrWhiteSpace(statusRaw))
        {
            return true;
        }

        var normalized = statusRaw.Trim();
        if (normalized.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (normalized.Equals("active", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("open", StringComparison.OrdinalIgnoreCase))
        {
            statusFilter = PersonalTodoStatus.Open;
            return true;
        }

        if (Enum.TryParse<PersonalTodoStatus>(normalized, ignoreCase: true, out var parsed)
            && Enum.IsDefined(parsed))
        {
            statusFilter = parsed;
            return true;
        }

        invalid = PlatformApiResults.Problem(
            DomainErrorCodes.InvalidPersonalTodo,
            "To-do list status filter is invalid.",
            StatusCodes.Status400BadRequest);
        return false;
    }

    private static bool TryRequirePersonalAccountClass(
        string? accountClassRaw,
        out AccountClass accountClass,
        out IResult? denied)
    {
        accountClass = AccountClass.Platform;
        denied = null;
        if (!Enum.TryParse(accountClassRaw, ignoreCase: true, out accountClass)
            || accountClass != AccountClass.Personal)
        {
            denied = PlatformApiResults.Problem(
                ApplicationErrorCodes.AccountScopeDenied,
                "This action requires a Personal session.",
                StatusCodes.Status403Forbidden);
            return false;
        }

        return true;
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

    internal sealed record ClaimPersonalAdRewardRequest(string ClaimKey);
}
