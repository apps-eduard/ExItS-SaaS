using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Application.Identity;

/// <summary>
/// Completes Google/Facebook (or testing) external authentication by creating or linking a Platform User
/// and issuing a browser session. Never grants Platform roles, memberships, entitlements, or product roles.
/// </summary>
public sealed class CompleteExternalLogin
{
    private static readonly Regex NonUsernameChars = new("[^a-z0-9._-]+", RegexOptions.Compiled);

    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformExternalLoginRepository _externalLogins;
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IOrganizationContextPreferenceRepository _orgPreferences;
    private readonly EnsureAccountProfilesForUser _ensureProfiles;
    private readonly IPlatformSessionTokenService _tokens;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PlatformSessionOptions _sessionOptions;
    private readonly IPlatformMfaReadinessService _mfa;
    private readonly IPersonalUserProfileRepository? _personalProfiles;

    public CompleteExternalLogin(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformExternalLoginRepository externalLogins,
        IPlatformAuthSessionRepository sessions,
        IOrganizationMembershipRepository memberships,
        IPlatformOrganizationRepository organizations,
        IOrganizationContextPreferenceRepository orgPreferences,
        EnsureAccountProfilesForUser ensureProfiles,
        IPlatformSessionTokenService tokens,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IOptions<PlatformSessionOptions> sessionOptions,
        IPlatformMfaReadinessService mfa,
        IPersonalUserProfileRepository? personalProfiles = null)
    {
        _users = users;
        _credentials = credentials;
        _externalLogins = externalLogins;
        _sessions = sessions;
        _memberships = memberships;
        _organizations = organizations;
        _orgPreferences = orgPreferences;
        _ensureProfiles = ensureProfiles;
        _tokens = tokens;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _sessionOptions = sessionOptions.Value;
        _mfa = mfa;
        _personalProfiles = personalProfiles;
    }

    public async Task<ApplicationResult<PlatformLoginResultDto>> ExecuteAsync(
        ExternalLoginIdentity identity,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(identity);

        string provider;
        string subject;
        string email;
        try
        {
            provider = PlatformExternalLogin.NormalizeProvider(identity.Provider);
            subject = identity.ProviderSubject.Trim();
            email = PlatformUser.NormalizeEmail(identity.Email);
        }
        catch (DomainException ex)
        {
            await WriteFailedAsync(null, ApplicationErrorCodes.ExternalAuthFailed, ex.Message, cancellationToken)
                .ConfigureAwait(false);
            return ApplicationResult<PlatformLoginResultDto>.Failure(
                ApplicationErrorCodes.ExternalAuthFailed,
                "External authentication failed.");
        }

        if (string.IsNullOrWhiteSpace(subject))
        {
            await WriteFailedAsync(null, ApplicationErrorCodes.ExternalAuthFailed, "Missing subject.", cancellationToken)
                .ConfigureAwait(false);
            return ApplicationResult<PlatformLoginResultDto>.Failure(
                ApplicationErrorCodes.ExternalAuthFailed,
                "External authentication failed.");
        }

        if (!identity.EmailVerified)
        {
            await WriteFailedAsync(null, ApplicationErrorCodes.ExternalAuthEmailUnverified, "Email not verified.", cancellationToken)
                .ConfigureAwait(false);
            return ApplicationResult<PlatformLoginResultDto>.Failure(
                ApplicationErrorCodes.ExternalAuthEmailUnverified,
                "A verified email from the identity provider is required.");
        }

        var utcNow = _clock.UtcNow;
        var existingLink = await _externalLogins
            .FindByProviderSubjectAsync(provider, subject, cancellationToken)
            .ConfigureAwait(false);

        PlatformUser user;
        var linkedExisting = false;
        if (existingLink is not null)
        {
            var linkedUser = await _users.GetByIdAsync(existingLink.UserId, cancellationToken).ConfigureAwait(false);
            if (linkedUser is null)
            {
                await WriteFailedAsync(existingLink.UserId, ApplicationErrorCodes.ExternalAuthFailed, "Linked user missing.", cancellationToken)
                    .ConfigureAwait(false);
                return ApplicationResult<PlatformLoginResultDto>.Failure(
                    ApplicationErrorCodes.ExternalAuthFailed,
                    "External authentication failed.");
            }

            user = linkedUser;
            existingLink.TouchProviderEmail(email, utcNow);
            await _externalLogins.UpdateAsync(existingLink, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var byEmail = await _users.GetByNormalizedEmailAsync(email, cancellationToken).ConfigureAwait(false);
            if (byEmail is not null)
            {
                user = byEmail;
                linkedExisting = true;
                var link = PlatformExternalLogin.Create(user.Id, provider, subject, email, utcNow);
                await _externalLogins.AddAsync(link, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                try
                {
                    var username = await AllocateUsernameAsync(email, cancellationToken).ConfigureAwait(false);
                    var displayName = ResolveDisplayName(identity.DisplayName, email);
                    user = PlatformUser.Create(username, displayName, email, utcNow);
                    await _users.AddAsync(user, cancellationToken).ConfigureAwait(false);
                    var link = PlatformExternalLogin.Create(user.Id, provider, subject, email, utcNow);
                    await _externalLogins.AddAsync(link, cancellationToken).ConfigureAwait(false);
                }
                catch (DomainException ex)
                {
                    await WriteFailedAsync(null, ApplicationErrorCodes.ExternalAuthFailed, ex.Message, cancellationToken)
                        .ConfigureAwait(false);
                    return ApplicationResult<PlatformLoginResultDto>.Failure(ex.ErrorCode, ex.Message);
                }
            }
        }

        if (user.Status is not AccountStatus.Active)
        {
            await WriteFailedAsync(user.Id, ApplicationErrorCodes.AccountNotEligibleForLogin, "Account not active.", cancellationToken)
                .ConfigureAwait(false);
            return ApplicationResult<PlatformLoginResultDto>.Failure(
                ApplicationErrorCodes.AccountNotEligibleForLogin,
                "Account is not eligible for login.");
        }

        var credential = await _credentials.GetByUserIdAsync(user.Id, cancellationToken).ConfigureAwait(false);
        var createdCredential = false;
        if (credential is null)
        {
            credential = PlatformUserCredential.CreateForExternalLogin(user.Id, utcNow, emailVerified: true);
            createdCredential = true;
        }
        else if (credential.EmailVerifiedAtUtc is null)
        {
            credential.MarkEmailVerified(utcNow);
        }

        if (credential.IsLockedOut(utcNow))
        {
            await WriteFailedAsync(user.Id, ApplicationErrorCodes.CredentialLockedOut, "Locked out.", cancellationToken)
                .ConfigureAwait(false);
            return ApplicationResult<PlatformLoginResultDto>.Failure(
                ApplicationErrorCodes.CredentialLockedOut,
                "Credential is locked out.");
        }

        credential.RegisterSuccessfulAccess(utcNow);
        if (createdCredential)
        {
            await _credentials.AddAsync(credential, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _credentials.UpdateAsync(credential, cancellationToken).ConfigureAwait(false);
        }

        var profile = await _ensureProfiles
            .ExecuteAsync(user.Id, preferredClass: null, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var opaqueToken = _tokens.CreateOpaqueToken();
        var tokenHash = _tokens.HashToken(opaqueToken);
        var idle = TimeSpan.FromMinutes(Math.Max(1, _sessionOptions.IdleTimeoutMinutes));
        var absolute = TimeSpan.FromHours(Math.Max(1, _sessionOptions.AbsoluteLifetimeHours));
        var session = PlatformAuthSession.Create(
            user.Id,
            profile.Id,
            profile.AccountClass,
            tokenHash,
            credential.SecurityStamp,
            utcNow,
            idle,
            absolute,
            ipAddress,
            HashUserAgent(userAgent));

        await _sessions.AddAsync(session, cancellationToken).ConfigureAwait(false);
        await ApplyProviderPictureAsync(user.Id, identity.PictureUrl, utcNow, cancellationToken)
            .ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        Guid? orgId = null;
        string? orgName = null;
        var selectionState = "None";
        var activeCount = 0;
        if (profile.AccountClass is AccountClass.Organization)
        {
            (orgId, orgName, selectionState, activeCount) = await OrganizationContextResolver
                .ResolveAsync(
                    session,
                    _memberships,
                    _organizations,
                    _sessions,
                    _unitOfWork,
                    cancellationToken,
                    _orgPreferences,
                    utcNow,
                    user.HomeOrganizationId)
                .ConfigureAwait(false);
        }

        await _auditWriter.WriteAsync(
            $"platform-user:{user.Id.Value:D}",
            AuditActorType.PlatformUser,
            linkedExisting
                ? PlatformAuditActions.PlatformAuthExternalLoginLinked
                : PlatformAuditActions.PlatformAuthExternalLoginSucceeded,
            nameof(PlatformAuthSession),
            session.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            organizationId: session.SelectedOrganizationId,
            summary: linkedExisting
                ? $"External login linked ({provider}); {profile.AccountClass} session established (no roles/membership granted)."
                : $"External login succeeded ({provider}); {profile.AccountClass} session established (no roles/membership granted).",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var mfa = await _mfa.GetForUserAsync(user.Id, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<PlatformLoginResultDto>.Success(new PlatformLoginResultDto(
            opaqueToken,
            session.Id.Value,
            user.Id.Value,
            user.Username,
            user.DisplayName,
            user.NormalizedEmail,
            session.ExpiresAtUtc,
            session.AbsoluteExpiresAtUtc,
            orgId,
            orgName,
            selectionState,
            activeCount,
            mfa,
            profile.Id.Value,
            profile.AccountClass.ToString(),
            session.AllowedScope.ToString(),
            user.HomeOrganizationId?.Value,
            OrganizationContextLocked: user.IsOrganizationScopedStaff));
    }

    private async Task<string> AllocateUsernameAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var local = normalizedEmail.Split('@')[0];
        var baseName = NonUsernameChars.Replace(local.ToLowerInvariant(), string.Empty);
        if (baseName.Length < 3)
        {
            baseName = "user" + baseName;
        }

        if (baseName.Length > 48)
        {
            baseName = baseName[..48];
        }

        if (!char.IsLetterOrDigit(baseName[0]))
        {
            baseName = "u" + baseName;
        }

        if (!char.IsLetterOrDigit(baseName[^1]))
        {
            baseName += "0";
        }

        for (var attempt = 0; attempt < 32; attempt++)
        {
            var candidate = attempt == 0
                ? baseName
                : Truncate($"{baseName}{attempt}", 64);
            if (candidate.Length < 3)
            {
                candidate = $"user{RandomNumberGenerator.GetInt32(1000, 9999)}";
            }

            try
            {
                var (_, normalized) = PlatformUser.NormalizeUsername(candidate);
                var existing = await _users.GetByNormalizedUsernameAsync(normalized, cancellationToken)
                    .ConfigureAwait(false);
                if (existing is null)
                {
                    return candidate;
                }
            }
            catch (DomainException)
            {
                // try next
            }
        }

        return $"user{Guid.NewGuid():N}"[..16];
    }

    private static string ResolveDisplayName(string? displayName, string email)
    {
        return SanitizeToDisplayName(displayName)
            ?? SanitizeToDisplayName(MailboxLocalPart(email))
            ?? "Platform User";
    }

    private static string MailboxLocalPart(string email)
    {
        var at = email.IndexOf('@');
        return at > 0 ? email[..at] : email;
    }

    /// <summary>
    /// Google may send the mailbox, a comma, or punctuation the account name rule rejects.
    /// Keep letters, numbers, spaces, apostrophes, periods, and hyphens.
    /// </summary>
    private static string? SanitizeToDisplayName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Trim();
        var at = text.IndexOf('@');
        if (at > 0)
        {
            text = text[..at];
        }

        text = text
            .Replace('\u2019', '\'')
            .Replace('\u2018', '\'')
            .Replace('\u2013', '-')
            .Replace('\u2014', '-');

        var builder = new StringBuilder(text.Length);
        var previousSpace = false;
        foreach (var ch in text)
        {
            if (char.IsLetterOrDigit(ch) || ch is '\'' or '.' or '-')
            {
                builder.Append(ch);
                previousSpace = false;
                continue;
            }

            if (builder.Length > 0 && !previousSpace)
            {
                builder.Append(' ');
                previousSpace = true;
            }
        }

        var collapsed = builder.ToString().Trim();
        var start = 0;
        while (start < collapsed.Length && !char.IsLetterOrDigit(collapsed[start]))
        {
            start++;
        }

        collapsed = collapsed[start..].Trim(' ', '\'', '-');
        if (collapsed.Length > 100)
        {
            collapsed = collapsed[..100].TrimEnd(' ', '\'', '-', '.');
        }

        return collapsed.Length >= 2 ? collapsed : null;
    }

    private async Task ApplyProviderPictureAsync(
        PlatformUserId userId,
        string? pictureUrl,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        if (_personalProfiles is null || !IsProviderPictureUrl(pictureUrl))
        {
            return;
        }

        var picture = pictureUrl!.Trim();
        var personal = await _personalProfiles.GetByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        var current = personal?.ProfilePhotoUrl;
        if (!string.IsNullOrWhiteSpace(current) && !IsProviderPictureUrl(current))
        {
            return;
        }

        if (string.Equals(current, picture, StringComparison.Ordinal))
        {
            return;
        }

        var created = personal is null;
        personal ??= PersonalUserProfile.Create(userId, utcNow);
        personal.SetProfilePhotoUrl(picture, utcNow);
        if (created)
        {
            await _personalProfiles.AddAsync(personal, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _personalProfiles.UpdateAsync(personal, cancellationToken).ConfigureAwait(false);
        }
    }

    private static bool IsProviderPictureUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= PersonalUserProfile.MaxPhotoLength
            && Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps;
    }

    private async Task WriteFailedAsync(
        PlatformUserId? userId,
        string errorCode,
        string detail,
        CancellationToken cancellationToken)
    {
        await _auditWriter.WriteAsync(
            userId is null ? "anonymous" : $"platform-user:{userId.Value:D}",
            userId is null ? AuditActorType.System : AuditActorType.PlatformUser,
            PlatformAuditActions.PlatformAuthExternalLoginFailed,
            nameof(PlatformUser),
            userId?.Value.ToString("D") ?? "unknown",
            AuditOutcome.Denied,
            summary: "External authentication failed (provider secrets/tokens not recorded).",
            reason: errorCode,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        _ = detail;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private static string? HashUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return null;
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(userAgent.Trim()));
        return Convert.ToHexString(hash);
    }
}
