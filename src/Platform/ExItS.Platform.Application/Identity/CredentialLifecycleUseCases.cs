using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Application.Identity;

internal static class CredentialSessionInvalidation
{
    public static async Task RevokeAllAsync(
        IPlatformAuthSessionRepository sessions,
        IPlatformAccessTokenRepository accessTokens,
        IPlatformDeviceRecoveryCredentialRepository recoveryCredentials,
        IAuditWriter auditWriter,
        PlatformUserId userId,
        DateTimeOffset utcNow,
        string summary,
        CancellationToken cancellationToken)
    {
        var sessionCount = await sessions.RevokeAllActiveForUserAsync(userId, utcNow, cancellationToken).ConfigureAwait(false);
        var tokenCount = await accessTokens.RevokeAllActiveForUserAsync(userId, utcNow, cancellationToken).ConfigureAwait(false);
        var recoveryCount = await recoveryCredentials.RevokeActiveForUserAsync(userId, utcNow, cancellationToken).ConfigureAwait(false);
        if (sessionCount <= 0 && tokenCount <= 0 && recoveryCount <= 0)
        {
            return;
        }

        await auditWriter.WriteAsync(
            $"platform-user:{userId.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PlatformAuthSessionRevoked,
            nameof(PlatformAuthSession),
            userId.Value.ToString("D"),
            AuditOutcome.Succeeded,
            summary: $"{summary} (sessions={sessionCount}, accessTokens={tokenCount}, recoveryCredentials={recoveryCount}).",
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}

public sealed class ChangePlatformUserPassword
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IPlatformAccessTokenRepository _accessTokens;
    private readonly IPlatformDeviceRecoveryCredentialRepository _recoveryCredentials;
    private readonly IPlatformPasswordHasher _hasher;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PlatformPasswordOptions _passwordOptions;
    private readonly PlatformLockoutOptions _lockout;

    public ChangePlatformUserPassword(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformAuthSessionRepository sessions,
        IPlatformAccessTokenRepository accessTokens,
        IPlatformDeviceRecoveryCredentialRepository recoveryCredentials,
        IPlatformPasswordHasher hasher,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IOptions<PlatformPasswordOptions> passwordOptions,
        IOptions<PlatformLockoutOptions> lockout)
    {
        _users = users;
        _credentials = credentials;
        _sessions = sessions;
        _accessTokens = accessTokens;
        _recoveryCredentials = recoveryCredentials;
        _hasher = hasher;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _passwordOptions = passwordOptions.Value;
        _lockout = lockout.Value;
    }

    public async Task<ApplicationResult<PlatformCredentialStatusDto>> ExecuteAsync(
        Guid authenticatedUserId,
        string? currentPassword,
        string? newPassword,
        CancellationToken cancellationToken = default)
    {
        var policyError = PlatformPasswordPolicy.Validate(newPassword, _passwordOptions);
        if (policyError is not null)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.PasswordInvalid,
                policyError);
        }

        var id = PlatformUserId.From(authenticatedUserId);
        var user = await _users.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status is not AccountStatus.Active)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.AccountNotEligibleForLogin,
                "Account is not eligible.");
        }

        var credential = await _credentials.GetByUserIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (credential is null)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.CredentialNotFound,
                "Platform User has no credential.");
        }

        var utcNow = _clock.UtcNow;
        if (credential.IsLockedOut(utcNow))
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.CredentialLockedOut,
                "Credential is locked out.");
        }

        var verification = _hasher.VerifyHashedPassword(credential.PasswordHash, currentPassword ?? string.Empty);
        if (verification == PlatformPasswordVerificationResult.Failed)
        {
            credential.RegisterFailedAccess(
                _lockout.MaxFailedAccessAttempts,
                TimeSpan.FromMinutes(Math.Max(1, _lockout.LockoutMinutes)),
                utcNow);
            await _credentials.UpdateAsync(credential, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.CurrentPasswordInvalid,
                "Current password is incorrect.");
        }

        credential.ReplacePasswordHash(_hasher.HashPassword(newPassword!), _hasher.Algorithm, utcNow);
        await _credentials.UpdateAsync(credential, cancellationToken).ConfigureAwait(false);
            await CredentialSessionInvalidation.RevokeAllAsync(
                _sessions,
                _accessTokens,
                _recoveryCredentials,
                _auditWriter,
            id,
            utcNow,
            "All browser sessions revoked after password change.",
            cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditWriter.WriteAsync(
            $"platform-user:{id.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PlatformAuthPasswordChanged,
            nameof(PlatformUserCredential),
            id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            summary: "Authenticated password change completed (password not recorded).",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return await new GetPlatformCredentialStatus(_users, _credentials, _clock)
            .ExecuteAsync(authenticatedUserId, cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class RequestPasswordReset
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformCredentialTokenRepository _tokens;
    private readonly IPlatformSessionTokenService _tokenService;
    private readonly IPlatformAuthOutboundMessageSink _messages;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PlatformCredentialLifecycleOptions _lifecycle;

    public RequestPasswordReset(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformCredentialTokenRepository tokens,
        IPlatformSessionTokenService tokenService,
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
        _messages = messages;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _lifecycle = lifecycle.Value;
    }

    public async Task<ApplicationResult<CredentialWorkflowAckDto>> ExecuteAsync(
        string? usernameOrEmail,
        string? publicSurface = null,
        CancellationToken cancellationToken = default)
    {
        const string generic = "If an eligible account exists, a password reset token was issued.";
        var ack = new CredentialWorkflowAckDto(generic, null, null);
        var surfaceResult = PlatformAuthPublicSurfaces.Normalize(publicSurface);
        if (!surfaceResult.IsSuccess)
        {
            return ApplicationResult<CredentialWorkflowAckDto>.Failure(
                surfaceResult.ErrorCode!,
                surfaceResult.ErrorMessage!);
        }

        var identifier = (usernameOrEmail ?? string.Empty).Trim();
        if (identifier.Length == 0)
        {
            return ApplicationResult<CredentialWorkflowAckDto>.Success(ack);
        }

        var (user, matchedViaRecoveryEmail) = await ResolveUserAsync(identifier, cancellationToken)
            .ConfigureAwait(false);
        if (user is null || user.Status is not AccountStatus.Active)
        {
            return ApplicationResult<CredentialWorkflowAckDto>.Success(ack);
        }

        var credential = await _credentials.GetByUserIdAsync(user.Id, cancellationToken).ConfigureAwait(false);
        if (credential is null)
        {
            return ApplicationResult<CredentialWorkflowAckDto>.Success(ack);
        }

        // Prefer real contact email for org-scoped staff (login is synthetic local@ORG######).
        var deliveryEmail = !string.IsNullOrWhiteSpace(user.NormalizedContactEmail)
            ? user.NormalizedContactEmail!
            : user.NormalizedEmail;
        if (matchedViaRecoveryEmail
            && !string.IsNullOrWhiteSpace(credential.RecoveryNormalizedEmail)
            && credential.HasVerifiedRecoveryEmail)
        {
            deliveryEmail = credential.RecoveryNormalizedEmail!;
        }

        var utcNow = _clock.UtcNow;
        await _tokens.InvalidateActiveForUserAsync(
            user.Id,
            PlatformCredentialTokenPurpose.PasswordReset,
            utcNow,
            cancellationToken).ConfigureAwait(false);

        var opaque = _tokenService.CreateOpaqueToken();
        var lifetime = TimeSpan.FromMinutes(Math.Max(5, _lifecycle.PasswordResetTokenLifetimeMinutes));
        var token = PlatformCredentialToken.Create(
            user.Id,
            PlatformCredentialTokenPurpose.PasswordReset,
            _tokenService.HashToken(opaque),
            utcNow,
            lifetime);
        await _tokens.AddAsync(token, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _messages.PublishAsync(
            new PlatformAuthOutboundMessage(
                PlatformAuthOutboundMessageKinds.PasswordReset,
                user.Id.Value,
                deliveryEmail,
                opaque,
                token.ExpiresAtUtc,
                PublicSurface: surfaceResult.Value),
            cancellationToken).ConfigureAwait(false);

        await _auditWriter.WriteAsync(
            $"platform-user:{user.Id.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PlatformAuthPasswordResetRequested,
            nameof(PlatformCredentialToken),
            token.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            summary: "Password reset token issued (token/password not recorded).",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ApplicationResult<CredentialWorkflowAckDto>.Success(
            new CredentialWorkflowAckDto(
                generic,
                _lifecycle.ExposeDebugTokens ? opaque : null,
                token.ExpiresAtUtc));
    }

    private async Task<(PlatformUser? User, bool MatchedViaRecoveryEmail)> ResolveUserAsync(
        string identifier,
        CancellationToken cancellationToken)
    {
        try
        {
            if (identifier.Contains('@', StringComparison.Ordinal))
            {
                var normalizedEmail = PlatformUser.NormalizeEmail(identifier);
                var byPrimary = await _users
                    .GetByNormalizedEmailAsync(normalizedEmail, cancellationToken)
                    .ConfigureAwait(false);
                if (byPrimary is not null)
                {
                    return (byPrimary, false);
                }

                // Contact email is not unique. Resolve only when exactly one active match exists
                // (prefer exact staff-login primary match already handled above).
                var byContact = await _users
                    .ListByNormalizedContactEmailAsync(normalizedEmail, cancellationToken)
                    .ConfigureAwait(false);
                var activeByContact = byContact
                    .Where(u => u.Status == AccountStatus.Active)
                    .ToList();
                if (activeByContact.Count == 1)
                {
                    return (activeByContact[0], false);
                }

                var recoveryUserId = await _credentials
                    .FindUserIdByVerifiedRecoveryEmailAsync(normalizedEmail, cancellationToken)
                    .ConfigureAwait(false);
                if (recoveryUserId is null)
                {
                    return (null, false);
                }

                var byRecovery = await _users.GetByIdAsync(recoveryUserId, cancellationToken).ConfigureAwait(false);
                return (byRecovery, byRecovery is not null);
            }

            var (_, normalized) = PlatformUser.NormalizeUsername(identifier);
            var byUsername = await _users
                .GetByNormalizedUsernameAsync(normalized, cancellationToken)
                .ConfigureAwait(false);
            return (byUsername, false);
        }
        catch (DomainException)
        {
            return (null, false);
        }
    }
}

public sealed class ResetPasswordWithToken
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformCredentialTokenRepository _tokens;
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IPlatformAccessTokenRepository _accessTokens;
    private readonly IPlatformDeviceRecoveryCredentialRepository _recoveryCredentials;
    private readonly IPlatformSessionTokenService _tokenService;
    private readonly IPlatformPasswordHasher _hasher;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PlatformPasswordOptions _passwordOptions;

    public ResetPasswordWithToken(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformCredentialTokenRepository tokens,
        IPlatformAuthSessionRepository sessions,
        IPlatformAccessTokenRepository accessTokens,
        IPlatformDeviceRecoveryCredentialRepository recoveryCredentials,
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
        _sessions = sessions;
        _accessTokens = accessTokens;
        _recoveryCredentials = recoveryCredentials;
        _tokenService = tokenService;
        _hasher = hasher;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _passwordOptions = passwordOptions.Value;
    }

    public async Task<ApplicationResult<PlatformCredentialStatusDto>> ExecuteAsync(
        string? opaqueToken,
        string? newPassword,
        CancellationToken cancellationToken = default)
    {
        var policyError = PlatformPasswordPolicy.Validate(newPassword, _passwordOptions);
        if (policyError is not null)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.PasswordInvalid,
                policyError);
        }

        if (string.IsNullOrWhiteSpace(opaqueToken))
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.CredentialTokenInvalid,
                "Reset token is invalid.");
        }

        var utcNow = _clock.UtcNow;
        var token = await _tokens
            .GetByTokenHashAsync(_tokenService.HashToken(opaqueToken), cancellationToken)
            .ConfigureAwait(false);
        if (token is null || token.Purpose != PlatformCredentialTokenPurpose.PasswordReset)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.CredentialTokenInvalid,
                "Reset token is invalid.");
        }

        if (token.ConsumedAtUtc is not null)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.CredentialTokenInvalid,
                "Reset token is invalid.");
        }

        if (token.ExpiresAtUtc <= utcNow)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.CredentialTokenExpired,
                "Reset token has expired.");
        }

        var user = await _users.GetByIdAsync(token.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status is not AccountStatus.Active)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.CredentialTokenInvalid,
                "Reset token is invalid.");
        }

        var credential = await _credentials.GetByUserIdAsync(user.Id, cancellationToken).ConfigureAwait(false);
        if (credential is null)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.CredentialNotFound,
                "Platform User has no credential.");
        }

        token.Consume(utcNow);
        credential.ReplacePasswordHash(_hasher.HashPassword(newPassword!), _hasher.Algorithm, utcNow);
        await _tokens.UpdateAsync(token, cancellationToken).ConfigureAwait(false);
        await _credentials.UpdateAsync(credential, cancellationToken).ConfigureAwait(false);
            await CredentialSessionInvalidation.RevokeAllAsync(
                _sessions,
                _accessTokens,
                _recoveryCredentials,
                _auditWriter,
            user.Id,
            utcNow,
            "All browser sessions revoked after password reset.",
            cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditWriter.WriteAsync(
            $"platform-user:{user.Id.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PlatformAuthPasswordResetCompleted,
            nameof(PlatformUserCredential),
            user.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            summary: "Password reset completed (token/password not recorded).",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return await new GetPlatformCredentialStatus(_users, _credentials, _clock)
            .ExecuteAsync(user.Id.Value, cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class RequestEmailVerification
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformCredentialTokenRepository _tokens;
    private readonly IPlatformSessionTokenService _tokenService;
    private readonly IPlatformAuthOutboundMessageSink _messages;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PlatformCredentialLifecycleOptions _lifecycle;

    public RequestEmailVerification(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformCredentialTokenRepository tokens,
        IPlatformSessionTokenService tokenService,
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
        _messages = messages;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _lifecycle = lifecycle.Value;
    }

    public async Task<ApplicationResult<CredentialWorkflowAckDto>> ExecuteAsync(
        Guid authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        var id = PlatformUserId.From(authenticatedUserId);
        var user = await _users.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status is not AccountStatus.Active)
        {
            return ApplicationResult<CredentialWorkflowAckDto>.Failure(
                ApplicationErrorCodes.AccountNotEligibleForLogin,
                "Account is not eligible.");
        }

        var credential = await _credentials.GetByUserIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (credential is null)
        {
            return ApplicationResult<CredentialWorkflowAckDto>.Failure(
                ApplicationErrorCodes.CredentialNotFound,
                "Platform User has no credential.");
        }

        if (credential.EmailVerifiedAtUtc is not null)
        {
            return ApplicationResult<CredentialWorkflowAckDto>.Success(
                new CredentialWorkflowAckDto("Email is already verified.", null, null));
        }

        var utcNow = _clock.UtcNow;
        await _tokens.InvalidateActiveForUserAsync(
            id,
            PlatformCredentialTokenPurpose.EmailVerification,
            utcNow,
            cancellationToken).ConfigureAwait(false);

        var opaque = _tokenService.CreateOpaqueToken();
        var lifetime = TimeSpan.FromHours(Math.Max(1, _lifecycle.EmailVerificationTokenLifetimeHours));
        var token = PlatformCredentialToken.Create(
            id,
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
            $"platform-user:{id.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PlatformAuthEmailVerificationRequested,
            nameof(PlatformCredentialToken),
            token.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            summary: "Email verification token issued (token not recorded; no vendor email delivery in this WP).",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ApplicationResult<CredentialWorkflowAckDto>.Success(
            new CredentialWorkflowAckDto(
                "If eligible, an email verification token was issued.",
                _lifecycle.ExposeDebugTokens ? opaque : null,
                token.ExpiresAtUtc));
    }
}

public sealed class ConfirmEmailVerification
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformCredentialTokenRepository _tokens;
    private readonly IPlatformSessionTokenService _tokenService;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ConfirmEmailVerification(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformCredentialTokenRepository tokens,
        IPlatformSessionTokenService tokenService,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _credentials = credentials;
        _tokens = tokens;
        _tokenService = tokenService;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformCredentialStatusDto>> ExecuteAsync(
        string? opaqueToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(opaqueToken))
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.CredentialTokenInvalid,
                "Verification token is invalid.");
        }

        var utcNow = _clock.UtcNow;
        var token = await _tokens
            .GetByTokenHashAsync(_tokenService.HashToken(opaqueToken), cancellationToken)
            .ConfigureAwait(false);
        if (token is null || token.Purpose != PlatformCredentialTokenPurpose.EmailVerification)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.CredentialTokenInvalid,
                "Verification token is invalid.");
        }

        if (token.ConsumedAtUtc is not null)
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
        if (user is null)
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

        token.Consume(utcNow);
        credential.MarkEmailVerified(utcNow);
        await _tokens.UpdateAsync(token, cancellationToken).ConfigureAwait(false);
        await _credentials.UpdateAsync(credential, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditWriter.WriteAsync(
            $"platform-user:{user.Id.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PlatformAuthEmailVerificationCompleted,
            nameof(PlatformUserCredential),
            user.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            summary: "Email verification completed via token (token not recorded).",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return await new GetPlatformCredentialStatus(_users, _credentials, _clock)
            .ExecuteAsync(user.Id.Value, cancellationToken)
            .ConfigureAwait(false);
    }
}
