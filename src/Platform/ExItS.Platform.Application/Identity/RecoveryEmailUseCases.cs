using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Application.Identity;

public sealed class RequestRecoveryEmailChange
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

    public RequestRecoveryEmailChange(
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
        string? recoveryEmail,
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

        string normalized;
        try
        {
            normalized = PlatformUser.NormalizeEmail(recoveryEmail ?? string.Empty);
        }
        catch (DomainException)
        {
            return ApplicationResult<CredentialWorkflowAckDto>.Failure(
                ApplicationErrorCodes.RecoveryEmailInvalid,
                "Recovery email format is invalid.");
        }

        if (string.Equals(normalized, user.NormalizedEmail, StringComparison.Ordinal))
        {
            return ApplicationResult<CredentialWorkflowAckDto>.Failure(
                ApplicationErrorCodes.RecoveryEmailInvalid,
                "Recovery email must differ from the account login email.");
        }

        var primaryOwner = await _users.GetByNormalizedEmailAsync(normalized, cancellationToken).ConfigureAwait(false);
        if (primaryOwner is not null && primaryOwner.Id != id)
        {
            return ApplicationResult<CredentialWorkflowAckDto>.Failure(
                ApplicationErrorCodes.RecoveryEmailConflict,
                "Recovery email is unavailable.");
        }

        if (await _credentials.IsRecoveryEmailInUseAsync(normalized, id, cancellationToken).ConfigureAwait(false))
        {
            return ApplicationResult<CredentialWorkflowAckDto>.Failure(
                ApplicationErrorCodes.RecoveryEmailConflict,
                "Recovery email is unavailable.");
        }

        var utcNow = _clock.UtcNow;
        try
        {
            credential.BeginRecoveryEmailChange(normalized, utcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<CredentialWorkflowAckDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await _tokens.InvalidateActiveForUserAsync(
            id,
            PlatformCredentialTokenPurpose.RecoveryEmailVerification,
            utcNow,
            cancellationToken).ConfigureAwait(false);

        var opaque = _tokenService.CreateOpaqueToken();
        var lifetime = TimeSpan.FromHours(Math.Max(1, _lifecycle.EmailVerificationTokenLifetimeHours));
        var token = PlatformCredentialToken.Create(
            id,
            PlatformCredentialTokenPurpose.RecoveryEmailVerification,
            _tokenService.HashToken(opaque),
            utcNow,
            lifetime);

        await _credentials.UpdateAsync(credential, cancellationToken).ConfigureAwait(false);
        await _tokens.AddAsync(token, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _messages.PublishAsync(
            new PlatformAuthOutboundMessage(
                PlatformAuthOutboundMessageKinds.RecoveryEmailVerification,
                user.Id.Value,
                normalized,
                opaque,
                token.ExpiresAtUtc),
            cancellationToken).ConfigureAwait(false);

        await _auditWriter.WriteAsync(
            $"platform-user:{id.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PlatformAuthRecoveryEmailRequested,
            nameof(PlatformUserCredential),
            id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            summary: "Recovery email verification token issued (token not recorded; recovery-only, no roles granted).",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ApplicationResult<CredentialWorkflowAckDto>.Success(
            new CredentialWorkflowAckDto(
                "If the address is valid, a recovery-email verification token was issued. It must be confirmed before recovery use.",
                _lifecycle.ExposeDebugTokens ? opaque : null,
                token.ExpiresAtUtc));
    }
}

public sealed class ConfirmRecoveryEmailChange
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformCredentialTokenRepository _tokens;
    private readonly IPlatformSessionTokenService _tokenService;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ConfirmRecoveryEmailChange(
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
                "Recovery email verification token is invalid.");
        }

        var utcNow = _clock.UtcNow;
        var token = await _tokens
            .GetByTokenHashAsync(_tokenService.HashToken(opaqueToken), cancellationToken)
            .ConfigureAwait(false);
        if (token is null
            || token.Purpose != PlatformCredentialTokenPurpose.RecoveryEmailVerification
            || !token.IsRedeemable(utcNow))
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.CredentialTokenInvalid,
                "Recovery email verification token is invalid.");
        }

        var user = await _users.GetByIdAsync(token.UserId, cancellationToken).ConfigureAwait(false);
        var credential = await _credentials.GetByUserIdAsync(token.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null || credential is null)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.CredentialTokenInvalid,
                "Recovery email verification token is invalid.");
        }

        try
        {
            token.Consume(utcNow);
            credential.ConfirmRecoveryEmail(utcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await _tokens.UpdateAsync(token, cancellationToken).ConfigureAwait(false);
        await _credentials.UpdateAsync(credential, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditWriter.WriteAsync(
            $"platform-user:{user.Id.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PlatformAuthRecoveryEmailConfirmed,
            nameof(PlatformUserCredential),
            user.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            summary: "Recovery email confirmed (recovery-only; no roles/membership granted).",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return await new GetPlatformCredentialStatus(_users, _credentials, _clock)
            .ExecuteAsync(user.Id.Value, cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class SkipRecoveryEmailPrompt
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SkipRecoveryEmailPrompt(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _credentials = credentials;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformCredentialStatusDto>> ExecuteAsync(
        Guid authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
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

        credential.SkipRecoveryEmailPrompt(_clock.UtcNow);
        await _credentials.UpdateAsync(credential, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditWriter.WriteAsync(
            $"platform-user:{id.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PlatformAuthRecoveryEmailSkipped,
            nameof(PlatformUserCredential),
            id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            summary: "Recovery email prompt skipped (login not blocked; no roles granted).",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return await new GetPlatformCredentialStatus(_users, _credentials, _clock)
            .ExecuteAsync(id.Value, cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class ClearRecoveryEmail
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformCredentialTokenRepository _tokens;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ClearRecoveryEmail(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformCredentialTokenRepository tokens,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _credentials = credentials;
        _tokens = tokens;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformCredentialStatusDto>> ExecuteAsync(
        Guid authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
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
        credential.ClearRecoveryEmail(utcNow);
        await _tokens.InvalidateActiveForUserAsync(
            id,
            PlatformCredentialTokenPurpose.RecoveryEmailVerification,
            utcNow,
            cancellationToken).ConfigureAwait(false);
        await _credentials.UpdateAsync(credential, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditWriter.WriteAsync(
            $"platform-user:{id.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PlatformAuthRecoveryEmailCleared,
            nameof(PlatformUserCredential),
            id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            summary: "Recovery email cleared (recovery-only).",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return await new GetPlatformCredentialStatus(_users, _credentials, _clock)
            .ExecuteAsync(id.Value, cancellationToken)
            .ConfigureAwait(false);
    }
}
