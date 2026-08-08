using System.Security.Claims;
using ExItS.Platform.Api.Authentication;
using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Api.Identity;

internal static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/platform/auth/login", async (
            LoginRequest body,
            HttpContext http,
            LoginPlatformUser useCase,
            IOptions<PlatformSessionOptions> sessionOptions,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(
                body.UsernameOrEmail,
                body.Password,
                http.Connection.RemoteIpAddress?.ToString(),
                http.Request.Headers.UserAgent.ToString(),
                ct).ConfigureAwait(false);

            if (!result.IsSuccess || result.Value is null)
            {
                return PlatformApiResults.FromResult(result, _ => Results.Ok());
            }

            AppendSessionCookie(http, result.Value.SessionToken, result.Value.ExpiresAtUtc, sessionOptions.Value, env);
            return Results.Ok(result.Value);
        })
        .RequireRateLimiting(PlatformSecurityPipeline.AuthLoginRateLimitPolicy)
        .AllowAnonymous();

        app.MapPost("/api/v1/platform/auth/logout", async (
            HttpContext http,
            LogoutPlatformSession useCase,
            IOptions<PlatformSessionOptions> sessionOptions,
            CancellationToken ct) =>
        {
            var token = ExtractSessionToken(http, sessionOptions.Value);
            await useCase.ExecuteAsync(token, ct).ConfigureAwait(false);
            DeleteSessionCookie(http, sessionOptions.Value);
            return Results.NoContent();
        })
        .AllowAnonymous();

        app.MapGet("/api/v1/platform/auth/me", async (
            HttpContext http,
            ValidateAndRenewPlatformSession useCase,
            IOptions<PlatformSessionOptions> sessionOptions,
            CancellationToken ct) =>
        {
            var token = ExtractSessionToken(http, sessionOptions.Value);
            var result = await useCase.ExecuteAsync(token, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        })
        .AllowAnonymous()
        .DisableRateLimiting();

        app.MapGet("/api/v1/platform/auth/credentials", async (
            HttpContext http,
            GetPlatformCredentialStatus useCase,
            CancellationToken ct) =>
        {
            if (!TryGetAuthenticatedUserId(http, out var userId))
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.SessionInvalid,
                    "Authentication is required.",
                    StatusCodes.Status401Unauthorized);
            }

            var result = await useCase.ExecuteAsync(userId, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        app.MapGet("/api/v1/platform/auth/organizations", async (
            HttpContext http,
            ListEligibleOrganizationsForSession useCase,
            IOptions<PlatformSessionOptions> sessionOptions,
            CancellationToken ct) =>
        {
            var token = ExtractSessionToken(http, sessionOptions.Value);
            var result = await useCase.ExecuteAsync(token, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        })
        .AllowAnonymous()
        .DisableRateLimiting();

        app.MapPut("/api/v1/platform/auth/organization-context", async (
            SetOrganizationContextRequest body,
            HttpContext http,
            SetSessionOrganizationContext useCase,
            IOptions<PlatformSessionOptions> sessionOptions,
            CancellationToken ct) =>
        {
            var token = ExtractSessionToken(http, sessionOptions.Value);
            var result = await useCase.ExecuteAsync(token, body.OrganizationId, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        })
        .AllowAnonymous();

        app.MapPost("/api/v1/platform/auth/change-password", async (
            ChangePasswordRequest body,
            HttpContext http,
            ChangePlatformUserPassword useCase,
            IOptions<PlatformSessionOptions> sessionOptions,
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
                .ExecuteAsync(userId, body.CurrentPassword, body.NewPassword, ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                DeleteSessionCookie(http, sessionOptions.Value);
            }

            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        app.MapPost("/api/v1/platform/auth/forgot-password", async (
            ForgotPasswordRequest body,
            RequestPasswordReset useCase,
            CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(body.UsernameOrEmail, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        })
        .RequireRateLimiting(PlatformSecurityPipeline.AuthPasswordResetRateLimitPolicy)
        .AllowAnonymous();

        app.MapPost("/api/v1/platform/auth/reset-password", async (
            ResetPasswordRequest body,
            ResetPasswordWithToken useCase,
            CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(body.Token, body.NewPassword, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        })
        .RequireRateLimiting(PlatformSecurityPipeline.AuthPasswordResetRateLimitPolicy)
        .AllowAnonymous();

        app.MapPost("/api/v1/platform/auth/register", async (
            RegisterPersonalAccountRequest body,
            RegisterPersonalAccount useCase,
            CancellationToken ct) =>
        {
            var result = await useCase
                .ExecuteAsync(
                    body.DisplayName ?? string.Empty,
                    body.Email ?? string.Empty,
                    ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        })
        .RequireRateLimiting(PlatformSecurityPipeline.AuthPasswordResetRateLimitPolicy)
        .AllowAnonymous();

        app.MapPost("/api/v1/platform/auth/activate-account", async (
            ActivatePersonalAccountRequest body,
            ActivatePersonalAccountRegistration useCase,
            CancellationToken ct) =>
        {
            var result = await useCase
                .ExecuteAsync(body.Token, body.Password, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        })
        .RequireRateLimiting(PlatformSecurityPipeline.AuthPasswordResetRateLimitPolicy)
        .AllowAnonymous();

        app.MapPost("/api/v1/platform/auth/email-verification/request", async (
            HttpContext http,
            RequestEmailVerification useCase,
            CancellationToken ct) =>
        {
            if (!TryGetAuthenticatedUserId(http, out var userId))
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.SessionInvalid,
                    "Authentication is required.",
                    StatusCodes.Status401Unauthorized);
            }

            var result = await useCase.ExecuteAsync(userId, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        })
        .RequireRateLimiting(PlatformSecurityPipeline.AuthPasswordResetRateLimitPolicy);

        app.MapPost("/api/v1/platform/auth/email-verification/confirm", async (
            ConfirmEmailVerificationRequest body,
            ConfirmEmailVerification useCase,
            CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(body.Token, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        })
        .RequireRateLimiting(PlatformSecurityPipeline.AuthPasswordResetRateLimitPolicy)
        .AllowAnonymous();

        app.MapPost("/api/v1/platform/auth/recovery-email/request", async (
            RequestRecoveryEmailRequest body,
            HttpContext http,
            RequestRecoveryEmailChange useCase,
            CancellationToken ct) =>
        {
            if (!TryGetAuthenticatedUserId(http, out var userId))
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.SessionInvalid,
                    "Authentication is required.",
                    StatusCodes.Status401Unauthorized);
            }

            var result = await useCase.ExecuteAsync(userId, body.RecoveryEmail, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        })
        .RequireRateLimiting(PlatformSecurityPipeline.AuthPasswordResetRateLimitPolicy);

        app.MapPost("/api/v1/platform/auth/recovery-email/confirm", async (
            ConfirmRecoveryEmailRequest body,
            ConfirmRecoveryEmailChange useCase,
            CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(body.Token, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        })
        .RequireRateLimiting(PlatformSecurityPipeline.AuthPasswordResetRateLimitPolicy)
        .AllowAnonymous();

        app.MapPost("/api/v1/platform/auth/recovery-email/skip", async (
            HttpContext http,
            SkipRecoveryEmailPrompt useCase,
            CancellationToken ct) =>
        {
            if (!TryGetAuthenticatedUserId(http, out var userId))
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.SessionInvalid,
                    "Authentication is required.",
                    StatusCodes.Status401Unauthorized);
            }

            var result = await useCase.ExecuteAsync(userId, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        })
        .RequireRateLimiting(PlatformSecurityPipeline.AuthPasswordResetRateLimitPolicy);

        app.MapPost("/api/v1/platform/auth/recovery-email/clear", async (
            HttpContext http,
            ClearRecoveryEmail useCase,
            CancellationToken ct) =>
        {
            if (!TryGetAuthenticatedUserId(http, out var userId))
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.SessionInvalid,
                    "Authentication is required.",
                    StatusCodes.Status401Unauthorized);
            }

            var result = await useCase.ExecuteAsync(userId, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        })
        .RequireRateLimiting(PlatformSecurityPipeline.AuthPasswordResetRateLimitPolicy);

        app.MapPost("/api/v1/platform/auth/token", async (
            IssueAccessTokenRequest body,
            HttpContext http,
            IssuePlatformAccessToken useCase,
            IOptions<PlatformSessionOptions> sessionOptions,
            CancellationToken ct) =>
        {
            var grant = (body.GrantType ?? "password").Trim();
            ApplicationResult<PlatformAccessTokenIssueDto> result;
            if (string.Equals(grant, "session", StringComparison.OrdinalIgnoreCase))
            {
                var sessionToken = ExtractSessionToken(http, sessionOptions.Value);
                result = await useCase
                    .ExecuteSessionGrantAsync(sessionToken, body.OrganizationId, body.ProductCode, ct)
                    .ConfigureAwait(false);
            }
            else
            {
                result = await useCase
                    .ExecutePasswordGrantAsync(body.UsernameOrEmail, body.Password, body.OrganizationId, body.ProductCode, ct)
                    .ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        })
        .RequireRateLimiting(PlatformSecurityPipeline.AuthLoginRateLimitPolicy)
        .AllowAnonymous();

        app.MapPost("/api/v1/platform/auth/token/bind", async (
            BindAccessTokenRequest body,
            HttpContext http,
            BindPlatformAccessTokenProductContext useCase,
            CancellationToken ct) =>
        {
            var token = ExtractBearerToken(http) ?? body.AccessToken;
            var result = await useCase
                .ExecuteAsync(token, body.OrganizationId, body.ProductCode ?? string.Empty, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        })
        .RequireRateLimiting(PlatformSecurityPipeline.AuthTokenOpsRateLimitPolicy)
        .AllowAnonymous();

        app.MapPost("/api/v1/platform/auth/introspect", async (
            IntrospectAccessTokenRequest body,
            HttpContext http,
            IntrospectPlatformAccessToken useCase,
            CancellationToken ct) =>
        {
            var token = ExtractBearerToken(http) ?? body.Token;
            var dto = await useCase.ExecuteAsync(token, ct).ConfigureAwait(false);
            return Results.Ok(dto);
        })
        .RequireRateLimiting(PlatformSecurityPipeline.AuthTokenOpsRateLimitPolicy)
        .AllowAnonymous();

        app.MapPost("/api/v1/platform/auth/token/revoke", async (
            HttpContext http,
            RevokePlatformAccessToken useCase,
            CancellationToken ct) =>
        {
            var token = ExtractBearerToken(http);
            var result = await useCase.ExecuteAsync(token, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, _ => Results.NoContent());
        })
        .RequireRateLimiting(PlatformSecurityPipeline.AuthTokenOpsRateLimitPolicy)
        .AllowAnonymous();

        app.MapGet("/api/v1/platform/auth/account-profiles", async (
            HttpContext http,
            ListAccountProfilesForUser useCase,
            CancellationToken ct) =>
        {
            if (!TryGetAuthenticatedUserId(http, out var userId))
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.SessionInvalid,
                    "Authentication is required.",
                    StatusCodes.Status401Unauthorized);
            }

            var profiles = await useCase.ExecuteAsync(PlatformUserId.From(userId), ct)
                .ConfigureAwait(false);
            return Results.Ok(profiles);
        });

        app.MapPost("/api/v1/platform/auth/account-profiles/select", async (
            SelectAccountProfileRequest body,
            HttpContext http,
            SelectAccountProfileSession useCase,
            IOptions<PlatformSessionOptions> sessionOptions,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            if (!TryGetAuthenticatedUserId(http, out var userId))
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.SessionInvalid,
                    "Authentication is required.",
                    StatusCodes.Status401Unauthorized);
            }

            var sessionIdRaw = http.User.FindFirstValue(PlatformSessionDefaults.SessionIdClaimType);
            if (!Guid.TryParse(sessionIdRaw, out var sessionId) || sessionId == Guid.Empty)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.SessionInvalid,
                    "Session is invalid.",
                    StatusCodes.Status401Unauthorized);
            }

            var result = await useCase.ExecuteAsync(
                PlatformUserId.From(userId),
                PlatformAuthSessionId.From(sessionId),
                body.AccountProfileId,
                http.Connection.RemoteIpAddress?.ToString(),
                http.Request.Headers.UserAgent.ToString(),
                ct).ConfigureAwait(false);

            if (!result.IsSuccess || result.Value is null)
            {
                return PlatformApiResults.FromResult(result, _ => Results.Ok());
            }

            AppendSessionCookie(http, result.Value.SessionToken, result.Value.ExpiresAtUtc, sessionOptions.Value, env);
            return Results.Ok(result.Value);
        });

        // Identity-bound organization staff invitation surfaces for Personal Mobile MVP.
        app.MapGet("/api/v1/platform/auth/organization-invitations/pending", async (
            HttpContext http,
            ListPendingOrganizationInvitationsForUser useCase,
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
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        app.MapPost("/api/v1/platform/auth/organization-invitations/accept", async (
            AcceptInvitationTokenRequest body,
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

        app.MapPost("/api/v1/platform/auth/organization-invitations/{invitationId:guid}/accept", async (
            Guid invitationId,
            AcceptOrganizationInvitationByIdForInvitee useCase,
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
                .ExecuteAsync(invitationId, actor.PlatformUserId, ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await membershipAuthz.Inner.AuditSucceededAsync(
                    PlatformAuditActions.InvitationAccepted,
                    nameof(OrganizationInvitation),
                    invitationId.ToString("D"),
                    result.Value!.OrganizationId.Value,
                    summary: "Accepted organization invitation by id (auth).",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, m => Results.Ok(MembershipQueryService.Map(m)));
        });

        return app;
    }

    private sealed record SelectAccountProfileRequest(Guid AccountProfileId);

    private sealed record AcceptInvitationTokenRequest(
        string? Token,
        string? Password,
        string? DisplayName = null,
        string? FirstName = null,
        string? LastName = null);

    private static string? ExtractBearerToken(HttpContext http)
    {
        var authorization = http.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization)
            && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authorization["Bearer ".Length..].Trim();
        }

        return null;
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

    internal static string? ExtractSessionToken(HttpContext http, PlatformSessionOptions options)
    {
        if (http.Items.TryGetValue(PlatformSessionClaimTypes.RequestTokenItemKey, out var cached)
            && cached is string cachedToken
            && !string.IsNullOrWhiteSpace(cachedToken))
        {
            return cachedToken;
        }

        if (http.Request.Cookies.TryGetValue(options.CookieName, out var cookieToken)
            && !string.IsNullOrWhiteSpace(cookieToken))
        {
            return cookieToken;
        }

        if (http.Request.Headers.TryGetValue(options.SessionTokenHeaderName, out var headerValues))
        {
            var headerToken = headerValues.ToString();
            if (!string.IsNullOrWhiteSpace(headerToken))
            {
                return headerToken;
            }
        }

        var authorization = http.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(authorization)
            && authorization.StartsWith(PlatformSessionDefaults.AuthorizationScheme + " ", StringComparison.OrdinalIgnoreCase))
        {
            return authorization[(PlatformSessionDefaults.AuthorizationScheme.Length + 1)..].Trim();
        }

        return null;
    }

    internal static void AppendSessionCookie(
        HttpContext http,
        string sessionToken,
        DateTimeOffset expiresAtUtc,
        PlatformSessionOptions options,
        IHostEnvironment env)
    {
        var secure = !(env.IsDevelopment() || env.IsEnvironment("Testing"));
        http.Response.Cookies.Append(
            options.CookieName,
            sessionToken,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = secure,
                SameSite = SameSiteMode.Lax,
                Expires = expiresAtUtc,
                IsEssential = true,
                Path = "/"
            });
    }

    internal static void DeleteSessionCookie(HttpContext http, PlatformSessionOptions options)
    {
        http.Response.Cookies.Delete(options.CookieName, new CookieOptions { Path = "/" });
    }

    internal sealed record LoginRequest(string? UsernameOrEmail, string? Password);
    internal sealed record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);
    internal sealed record ForgotPasswordRequest(string? UsernameOrEmail);
    internal sealed record ResetPasswordRequest(string? Token, string? NewPassword);
    internal sealed record RegisterPersonalAccountRequest(string? DisplayName, string? Email);
    internal sealed record ActivatePersonalAccountRequest(string? Token, string? Password);
    internal sealed record ConfirmEmailVerificationRequest(string? Token);
    internal sealed record RequestRecoveryEmailRequest(string? RecoveryEmail);
    internal sealed record ConfirmRecoveryEmailRequest(string? Token);
    internal sealed record SetOrganizationContextRequest(Guid? OrganizationId);
    internal sealed record IssueAccessTokenRequest(
        string? GrantType,
        string? UsernameOrEmail,
        string? Password,
        Guid? OrganizationId,
        string? ProductCode);
    internal sealed record BindAccessTokenRequest(string? AccessToken, Guid OrganizationId, string? ProductCode);
    internal sealed record IntrospectAccessTokenRequest(string? Token);
}
