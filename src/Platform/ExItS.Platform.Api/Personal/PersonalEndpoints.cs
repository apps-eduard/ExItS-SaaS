using System.Security.Claims;
using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Api.Personal;

/// <summary>
/// Personal Scope API surface (P16-WP04).
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

        return app;
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
