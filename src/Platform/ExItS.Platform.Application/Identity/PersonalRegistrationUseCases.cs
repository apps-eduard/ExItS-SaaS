using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
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
    public const string GenericAcknowledgement =
        "If the email is eligible, a verification message was sent. Open the message to activate your Personal Account.";

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
    private readonly IPublicUserIdGenerator _publicUserIds;

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
        IOptions<PlatformCredentialLifecycleOptions> lifecycle,
        IPublicUserIdGenerator publicUserIds)
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
        _publicUserIds = publicUserIds;
    }

    public async Task<ApplicationResult<PersonalRegistrationAckDto>> ExecuteAsync(
        string displayName,
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var utcNow = _clock.UtcNow;
            var normalizedEmail = PlatformUser.NormalizeEmail(email);

            // Validate display-name rules before the existence lookup so invalid names
            // fail the same way for new and existing emails.
            _ = PlatformUser.CreatePendingVerification(
                "signup-name-check",
                displayName,
                "signup-name-check@example.com",
                utcNow);

            var existing = await _users
                .GetByNormalizedEmailAsync(normalizedEmail, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                if (existing.Status == AccountStatus.PendingVerification
                    && !existing.IsOrganizationScopedStaff)
                {
                    var reissued = await IssueEmailVerificationAsync(existing, utcNow, cancellationToken)
                        .ConfigureAwait(false);
                    await _auditWriter.WriteAsync(
                        $"platform-user:{existing.Id.Value:D}",
                        AuditActorType.PlatformUser,
                        PlatformAuditActions.PersonalAccountRegistrationStarted,
                        nameof(PlatformUser),
                        existing.Id.Value.ToString("D"),
                        AuditOutcome.Succeeded,
                        summary: "Pending Personal registration verification token reissued (token not recorded).",
                        cancellationToken: cancellationToken).ConfigureAwait(false);
                    return ApplicationResult<PersonalRegistrationAckDto>.Success(reissued);
                }

                return ApplicationResult<PersonalRegistrationAckDto>.Success(PublicAck());
            }

            var username = await AllocateUsernameFromEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false);
            var user = PlatformUser.CreatePendingVerification(username, displayName, email, utcNow);
            var publicId = await _publicUserIds.GenerateUniqueAsync(cancellationToken).ConfigureAwait(false);
            user.AssignPublicUserId(publicId, utcNow);
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

            var issued = await IssueEmailVerificationAsync(user, utcNow, cancellationToken)
                .ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{user.Id.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalAccountRegistrationStarted,
                nameof(PlatformUser),
                user.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Personal Account registration started (Pending Verification; token not recorded).",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalRegistrationAckDto>.Success(issued);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalRegistrationAckDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<PersonalRegistrationAckDto> IssueEmailVerificationAsync(
        PlatformUser user,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        await _tokens.InvalidateActiveForUserAsync(
            user.Id,
            PlatformCredentialTokenPurpose.EmailVerification,
            utcNow,
            cancellationToken).ConfigureAwait(false);

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

        return PublicAck(opaque, token.ExpiresAtUtc);
    }

    private PersonalRegistrationAckDto PublicAck(string? opaque = null, DateTimeOffset? expiresAtUtc = null) =>
        new(
            GenericAcknowledgement,
            _lifecycle.ExposeDebugTokens ? opaque : null,
            _lifecycle.ExposeDebugTokens ? expiresAtUtc : null);

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

    public ActivatePersonalAccountRegistration(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformCredentialTokenRepository tokens,
        IPlatformSessionTokenService tokenService,
        IPlatformPasswordHasher hasher,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IOptions<PlatformPasswordOptions> passwordOptions)
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
