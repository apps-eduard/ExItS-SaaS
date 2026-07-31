using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Identity;
using Microsoft.AspNetCore.RateLimiting;

namespace ExItS.Platform.Api.Identity;

/// <summary>
/// Credential status and password-set endpoints. No login cookies/tokens.
/// Bootstrap requires Enabled + SharedSecret header, is rate-limited, refused in Production,
/// and fails when an administrator already exists.
/// </summary>
internal static class CredentialEndpoints
{
    public static IEndpointRouteBuilder MapCredentialEndpoints(this IEndpointRouteBuilder app)
    {
        var users = app.MapGroup("/api/v1/platform/users");

        users.MapGet("/{userId:guid}/credentials", async (
            Guid userId,
            GetPlatformCredentialStatus useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(PlatformUserCredential),
                userId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(userId, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        users.MapPut("/{userId:guid}/credentials/password", async (
            Guid userId,
            SetPasswordRequest body,
            SetPlatformUserPassword useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformUserPasswordSet,
                nameof(PlatformUserCredential),
                userId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(userId, body.Password, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.PlatformUserPasswordSet,
                    nameof(PlatformUserCredential),
                    userId.ToString("D"),
                    summary: "Platform User password set or replaced (hash only; password not recorded).",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        users.MapPost("/{userId:guid}/credentials/unlock", async (
            Guid userId,
            UnlockPlatformUserCredential useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformUserCredentialUnlocked,
                nameof(PlatformUserCredential),
                userId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(userId, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.PlatformUserCredentialUnlocked,
                    nameof(PlatformUserCredential),
                    userId.ToString("D"),
                    summary: "Platform User credential unlocked.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        users.MapPost("/{userId:guid}/credentials/email-verified", async (
            Guid userId,
            MarkPlatformUserEmailVerified useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManagePlatformUsers,
                PlatformAuditActions.PlatformUserEmailVerified,
                nameof(PlatformUserCredential),
                userId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(userId, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.PlatformUserEmailVerified,
                    nameof(PlatformUserCredential),
                    userId.ToString("D"),
                    summary: "Platform User email marked verified (no email delivery in this WP).",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, dto => Results.Ok(dto));
        });

        app.MapPost("/api/v1/platform/auth/bootstrap", async (
            HttpRequest request,
            BootstrapFirstPlatformAdministrator useCase,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            request.Headers.TryGetValue(PlatformAuthBootstrapOptions.SharedSecretHeaderName, out var secretValues);
            var providedSecret = secretValues.Count > 0 ? secretValues.ToString() : null;
            var result = await useCase
                .ExecuteAsync(providedSecret, env.IsProduction(), ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(
                result,
                dto => Results.Created($"/api/v1/platform/users/{dto.Id}", dto));
        })
        .RequireRateLimiting(PlatformSecurityPipeline.AuthBootstrapRateLimitPolicy);

        return app;
    }
}

internal sealed record SetPasswordRequest(string Password);
