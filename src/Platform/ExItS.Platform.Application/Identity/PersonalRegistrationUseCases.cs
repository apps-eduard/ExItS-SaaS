using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Application.Identity;

public sealed record PersonalRegistrationAckDto(
    string Message,
    string? DebugToken,
    DateTimeOffset? ExpiresAtUtc);

/// <summary>
/// Public Personal Account signup: identity + exclusive Personal profile + Pending Verification + verification email.
/// </summary>
public sealed class RegisterPersonalAccount
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformCredentialTokenRepository _tokens;
    private readonly IPlatformSessionTokenService _tokenService;
    private readonly EnsureAccountProfilesForUser _ensureProfiles;
    private readonly IPlatformAuthOutboundMessageSink _messages;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PlatformCredentialLifecycleOptions _lifecycle;

    public RegisterPersonalAccount(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformCredentialTokenRepository tokens,
        IPlatformSessionTokenService tokenService,
        EnsureAccountProfilesForUser ensureProfiles,
        IPlatformAuthOutboundMessageSink messages,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IOptions<PlatformCredentialLifecycleOptions> lifecycle)
    {
        _users = users;
        _credentials = credentials;
        _tokens = tokens;
        _tokenService = tokenService;
        _ensureProfiles = ensureProfiles;
        _messages = messages;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _lifecycle = lifecycle.Value;
    }

    public async Task<ApplicationResult<PersonalRegistrationAckDto>> ExecuteAsync(
        string displayName,
        string email,
        CancellationToken cancellationToken = default)
    {
        const string genericAck =
            "If the email is eligible, a verification message was sent. Open the message to activate your Personal Account.";

        try
        {
            var utcNow = _clock.UtcNow;
            var normalizedEmail = PlatformUser.NormalizeEmail(email);

            if (await _users.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false) is not null)
            {
                return ApplicationResult<PersonalRegistrationAckDto>.Failure(
                    ApplicationErrorCodes.EmailConflict,
                    "An account with this email already exists.");
            }

            var username = await AllocateUsernameFromEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false);
            var user = PlatformUser.CreatePendingVerification(username, displayName, email, utcNow);
            await _users.AddAsync(user, cancellationToken).ConfigureAwait(false);

            var credential = PlatformUserCredential.CreateForExternalLogin(user.Id, utcNow, emailVerified: false);
            await _credentials.AddAsync(credential, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _ensureProfiles
                .ExecuteAsync(
                    user.Id,
                    AccountClass.Personal,
                    exclusivePreferredClass: true,
                    cancellationToken)
                .ConfigureAwait(false);

            var opaque = _tokenService.CreateOpaqueToken();
            var lifetime = TimeSpan.FromHours(Math.Max(1, _lifecycle.EmailVerificationTokenLifetimeHours));
            var token = PlatformCredentialToken.Create(
                user.Id,
                PlatformCredentialTokenPurpose.EmailVerification,
                _tokenService.HashToken(opaque),
                utcNow,
                lifetime);
            await _tokens.AddAsync(token, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _messages.PublishAsync(
                new PlatformAuthOutboundMessage(
                    PlatformAuthOutboundMessageKinds.EmailVerification,
                    user.Id.Value,
                    user.NormalizedEmail,
                    opaque,
                    token.ExpiresAtUtc),
                cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{user.Id.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalAccountRegistrationStarted,
                nameof(PlatformUser),
                user.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Personal Account registration started (Pending Verification; token not recorded).",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalRegistrationAckDto>.Success(
                new PersonalRegistrationAckDto(
                    genericAck,
                    _lifecycle.ExposeDebugTokens ? opaque : null,
                    token.ExpiresAtUtc));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalRegistrationAckDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    /// <summary>
    /// Internal username derived from email local-part. Login uses email; username is not collected at signup.
    /// </summary>
    private async Task<string> AllocateUsernameFromEmailAsync(string normalizedEmail, CancellationToken cancellationToken)
    {
        var local = normalizedEmail;
        var at = normalizedEmail.IndexOf('@');
        if (at > 0)
        {
            local = normalizedEmail[..at];
        }

        var cleaned = System.Text.RegularExpressions.Regex.Replace(local, @"[^a-z0-9._-]", string.Empty);
        cleaned = cleaned.Trim('.', '_', '-');
        if (cleaned.Length < 3)
        {
            cleaned = "user" + cleaned;
        }

        if (cleaned.Length > 48)
        {
            cleaned = cleaned[..48];
        }

        if (!char.IsLetterOrDigit(cleaned[^1]))
        {
            cleaned += "0";
        }

        if (!char.IsLetterOrDigit(cleaned[0]))
        {
            cleaned = "u" + cleaned;
        }

        for (var attempt = 0; attempt < 20; attempt++)
        {
            var candidate = attempt == 0
                ? cleaned
                : $"{cleaned}-{attempt:x}";
            if (candidate.Length > 64)
            {
                candidate = candidate[..64];
            }

            try
            {
                var (display, normalized) = PlatformUser.NormalizeUsername(candidate);
                if (await _users.GetByNormalizedUsernameAsync(normalized, cancellationToken).ConfigureAwait(false) is null)
                {
                    return display;
                }
            }
            catch (DomainException)
            {
                // try next suffix
            }
        }

        return $"user-{Guid.NewGuid():N}"[..16];
    }
}

/// <summary>
/// Completes Personal registration: redeem verification token, set password, mark email verified, activate account.
/// </summary>
public sealed class ActivatePersonalAccountRegistration
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformCredentialTokenRepository _tokens;
    private readonly IPlatformSessionTokenService _tokenService;
    private readonly IPlatformPasswordHasher _hasher;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PlatformPasswordOptions _passwordOptions;
    private readonly AcceptPendingOrganizationInvitationsForUser? _acceptOrgInvitations;

    public ActivatePersonalAccountRegistration(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformCredentialTokenRepository tokens,
        IPlatformSessionTokenService tokenService,
        IPlatformPasswordHasher hasher,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IOptions<PlatformPasswordOptions> passwordOptions,
        AcceptPendingOrganizationInvitationsForUser? acceptOrgInvitations = null)
    {
        _users = users;
        _credentials = credentials;
        _tokens = tokens;
        _tokenService = tokenService;
        _hasher = hasher;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _passwordOptions = passwordOptions.Value;
        _acceptOrgInvitations = acceptOrgInvitations;
    }

    public async Task<ApplicationResult<PlatformCredentialStatusDto>> ExecuteAsync(
        string? opaqueToken,
        string? password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(opaqueToken))
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.CredentialTokenInvalid,
                "Verification token is invalid.");
        }

        var policyError = PlatformPasswordPolicy.Validate(password ?? string.Empty, _passwordOptions);
        if (policyError is not null)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.PasswordInvalid,
                policyError);
        }

        var utcNow = _clock.UtcNow;
        var token = await _tokens
            .GetByTokenHashAsync(_tokenService.HashToken(opaqueToken), cancellationToken)
            .ConfigureAwait(false);
        if (token is null
            || token.Purpose != PlatformCredentialTokenPurpose.EmailVerification
            || token.ConsumedAtUtc is not null)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.CredentialTokenInvalid,
                "Verification token is invalid.");
        }

        if (token.ExpiresAtUtc <= utcNow)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.CredentialTokenExpired,
                "Verification token has expired.");
        }

        var user = await _users.GetByIdAsync(token.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status != AccountStatus.PendingVerification)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.CredentialTokenInvalid,
                "Verification token is invalid.");
        }

        var credential = await _credentials.GetByUserIdAsync(user.Id, cancellationToken).ConfigureAwait(false);
        if (credential is null)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.CredentialNotFound,
                "Platform User has no credential.");
        }

        try
        {
            var hash = _hasher.HashPassword(password!);
            credential.ReplacePasswordHash(hash, _hasher.Algorithm, utcNow);
            credential.MarkEmailVerified(utcNow);
            token.Consume(utcNow);
            user.ActivateFromPendingVerification(utcNow);

            await _credentials.UpdateAsync(credential, cancellationToken).ConfigureAwait(false);
            await _tokens.UpdateAsync(token, cancellationToken).ConfigureAwait(false);
            await _users.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{user.Id.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalAccountRegistrationActivated,
                nameof(PlatformUser),
                user.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Account activated after email verification and password setup (token/password not recorded).",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (_acceptOrgInvitations is not null)
            {
                await _acceptOrgInvitations.ExecuteAsync(user, cancellationToken).ConfigureAwait(false);
            }

            return await new GetPlatformCredentialStatus(_users, _credentials, _clock)
                .ExecuteAsync(user.Id.Value, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
