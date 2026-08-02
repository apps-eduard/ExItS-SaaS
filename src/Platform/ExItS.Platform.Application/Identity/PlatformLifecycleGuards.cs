using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using Microsoft.Extensions.Hosting;

namespace ExItS.Platform.Application.Identity;

/// <summary>
/// Blocks suspend/deactivate when the target is the last login-capable Platform Administrator.
/// Role assignments are retained; this protects operational access, not role rows.
/// </summary>
public static class PlatformAdministratorLifecycleGuard
{
    public static async Task<ApplicationResult?> EnsureCanBlockLoginAsync(
        IPlatformUserRepository users,
        IPlatformRoleAssignmentRepository roles,
        PlatformUserId targetUserId,
        CancellationToken cancellationToken = default)
    {
        var targetRoles = await roles.ListActiveByUserAsync(targetUserId, cancellationToken).ConfigureAwait(false);
        if (!targetRoles.Any(r =>
                r.Role == PlatformSystemRole.PlatformAdministrator && r.OrganizationId is null))
        {
            return null;
        }

        var (assignments, _) = await roles
            .ListAsync(
                userId: null,
                role: PlatformSystemRole.PlatformAdministrator,
                organizationId: null,
                status: PlatformRoleAssignmentStatus.Active,
                skip: 0,
                take: 500,
                cancellationToken)
            .ConfigureAwait(false);

        var loginCapableAdmins = 0;
        foreach (var assignment in assignments.Where(a => a.OrganizationId is null))
        {
            var user = await users.GetByIdAsync(assignment.PlatformUserId, cancellationToken)
                .ConfigureAwait(false);
            if (user is not null && user.Status == AccountStatus.Active)
            {
                loginCapableAdmins++;
            }
        }

        if (loginCapableAdmins <= 1)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.LastPlatformAdministratorProtected,
                "Cannot suspend or deactivate the final active Platform Administrator.");
        }

        return null;
    }
}

/// <summary>Step-up verification for high-risk Platform User reactivation from Deactivated.</summary>
public sealed class PlatformLifecycleStepUp
{
    private readonly VerifyPlatformUserPassword _verifyPassword;
    private readonly IPlatformMfaReadinessService _mfa;
    private readonly IHostEnvironment _environment;

    public PlatformLifecycleStepUp(
        VerifyPlatformUserPassword verifyPassword,
        IPlatformMfaReadinessService mfa,
        IHostEnvironment environment)
    {
        _verifyPassword = verifyPassword;
        _mfa = mfa;
        _environment = environment;
    }

    public async Task<ApplicationResult?> VerifyAsync(
        PlatformUserId? actingUserId,
        string? actorPassword,
        string? mfaCode,
        CancellationToken cancellationToken = default)
    {
        // DevelopmentOperator fixtures (no PlatformUserId) skip password/MFA outside Production.
        if (actingUserId is null)
        {
            if (_environment.IsProduction())
            {
                return ApplicationResult.Failure(
                    ApplicationErrorCodes.StepUpRequired,
                    "Acting administrator identity is required for this account-status change.");
            }

            return null;
        }

        if (string.IsNullOrWhiteSpace(actorPassword))
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.StepUpRequired,
                "Current administrator password is required for this account-status change.");
        }

        var password = await _verifyPassword
            .ExecuteAsync(actingUserId.Value, actorPassword, cancellationToken)
            .ConfigureAwait(false);
        if (!password.IsSuccess)
        {
            return ApplicationResult.Failure(
                password.ErrorCode ?? ApplicationErrorCodes.PasswordInvalid,
                password.ErrorMessage ?? "Administrator password verification failed.");
        }

        var readiness = await _mfa.GetForUserAsync(actingUserId, cancellationToken).ConfigureAwait(false);
        if (readiness.MfaEnabled)
        {
            if (string.IsNullOrWhiteSpace(mfaCode))
            {
                return ApplicationResult.Failure(
                    ApplicationErrorCodes.MfaStepUpRequired,
                    "MFA challenge is required because MFA is enabled for the acting administrator.");
            }

            // Full MFA challenge verification is deferred until MFA enrollment WP.
            // Contractual gate: non-empty code must be supplied when MFA factors exist.
        }

        return null;
    }
}
