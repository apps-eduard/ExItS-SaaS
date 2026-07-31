using System.Security.Cryptography;
using System.Text;
using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Application.Identity;

public sealed class LoginPlatformUser
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IPlatformPasswordHasher _hasher;
    private readonly IPlatformSessionTokenService _tokens;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PlatformLockoutOptions _lockout;
    private readonly PlatformSessionOptions _sessionOptions;

    public LoginPlatformUser(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformAuthSessionRepository sessions,
        IPlatformPasswordHasher hasher,
        IPlatformSessionTokenService tokens,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IOptions<PlatformLockoutOptions> lockout,
        IOptions<PlatformSessionOptions> sessionOptions)
    {
        _users = users;
        _credentials = credentials;
        _sessions = sessions;
        _hasher = hasher;
        _tokens = tokens;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _lockout = lockout.Value;
        _sessionOptions = sessionOptions.Value;
    }

    public async Task<ApplicationResult<PlatformLoginResultDto>> ExecuteAsync(
        string? usernameOrEmail,
        string? password,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var utcNow = _clock.UtcNow;
        var identifier = (usernameOrEmail ?? string.Empty).Trim();
        if (identifier.Length == 0 || string.IsNullOrEmpty(password))
        {
            await WriteLoginFailedAsync(null, cancellationToken).ConfigureAwait(false);
            return LoginFailedResult();
        }

        PlatformUser? user = null;
        if (identifier.Contains('@', StringComparison.Ordinal))
        {
            try
            {
                var normalizedEmail = PlatformUser.NormalizeEmail(identifier);
                user = await _users.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false);
            }
            catch (DomainException)
            {
                await WriteLoginFailedAsync(null, cancellationToken).ConfigureAwait(false);
                return LoginFailedResult();
            }
        }
        else
        {
            try
            {
                var (_, normalizedUsername) = PlatformUser.NormalizeUsername(identifier);
                user = await _users.GetByNormalizedUsernameAsync(normalizedUsername, cancellationToken).ConfigureAwait(false);
            }
            catch (DomainException)
            {
                await WriteLoginFailedAsync(null, cancellationToken).ConfigureAwait(false);
                return LoginFailedResult();
            }
        }

        if (user is null)
        {
            await WriteLoginFailedAsync(null, cancellationToken).ConfigureAwait(false);
            return LoginFailedResult();
        }

        if (user.Status is not AccountStatus.Active)
        {
            await WriteLoginFailedAsync(
                user.Id,
                cancellationToken,
                ApplicationErrorCodes.AccountNotEligibleForLogin,
                "Account is not eligible for login.").ConfigureAwait(false);
            return ApplicationResult<PlatformLoginResultDto>.Failure(
                ApplicationErrorCodes.AccountNotEligibleForLogin,
                "Account is not eligible for login.");
        }

        var credential = await _credentials.GetByUserIdAsync(user.Id, cancellationToken).ConfigureAwait(false);
        if (credential is null)
        {
            await WriteLoginFailedAsync(user.Id, cancellationToken).ConfigureAwait(false);
            return LoginFailedResult();
        }

        if (credential.IsLockedOut(utcNow))
        {
            await WriteLoginFailedAsync(
                user.Id,
                cancellationToken,
                ApplicationErrorCodes.CredentialLockedOut,
                "Credential is locked out.").ConfigureAwait(false);
            return ApplicationResult<PlatformLoginResultDto>.Failure(
                ApplicationErrorCodes.CredentialLockedOut,
                "Credential is locked out.");
        }

        var verification = _hasher.VerifyHashedPassword(credential.PasswordHash, password);
        if (verification == PlatformPasswordVerificationResult.Failed)
        {
            credential.RegisterFailedAccess(
                _lockout.MaxFailedAccessAttempts,
                TimeSpan.FromMinutes(Math.Max(1, _lockout.LockoutMinutes)),
                utcNow);
            await _credentials.UpdateAsync(credential, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await WriteLoginFailedAsync(user.Id, cancellationToken).ConfigureAwait(false);

            if (credential.IsLockedOut(_clock.UtcNow))
            {
                await _auditWriter.WriteAsync(
                    $"platform-user:{user.Id.Value:D}",
                    AuditActorType.PlatformUser,
                    PlatformAuditActions.PlatformAuthLockoutStarted,
                    nameof(PlatformUserCredential),
                    user.Id.Value.ToString("D"),
                    AuditOutcome.Denied,
                    summary: "Credential lockout started after failed login attempts.",
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                return ApplicationResult<PlatformLoginResultDto>.Failure(
                    ApplicationErrorCodes.CredentialLockedOut,
                    "Credential is locked out.");
            }

            return LoginFailedResult();
        }

        if (verification == PlatformPasswordVerificationResult.SuccessRehashNeeded)
        {
            credential.ReplacePasswordHash(_hasher.HashPassword(password), _hasher.Algorithm, utcNow);
        }

        credential.RegisterSuccessfulAccess(utcNow);
        await _credentials.UpdateAsync(credential, cancellationToken).ConfigureAwait(false);

        var opaqueToken = _tokens.CreateOpaqueToken();
        var tokenHash = _tokens.HashToken(opaqueToken);
        var idle = TimeSpan.FromMinutes(Math.Max(1, _sessionOptions.IdleTimeoutMinutes));
        var absolute = TimeSpan.FromHours(Math.Max(1, _sessionOptions.AbsoluteLifetimeHours));
        var session = PlatformAuthSession.Create(
            user.Id,
            tokenHash,
            credential.SecurityStamp,
            utcNow,
            idle,
            absolute,
            ipAddress,
            HashUserAgent(userAgent));

        await _sessions.AddAsync(session, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditWriter.WriteAsync(
            $"platform-user:{user.Id.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PlatformAuthLoginSucceeded,
            nameof(PlatformAuthSession),
            session.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            summary: "Platform User browser session established (token/password not recorded).",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ApplicationResult<PlatformLoginResultDto>.Success(new PlatformLoginResultDto(
            opaqueToken,
            session.Id.Value,
            user.Id.Value,
            user.Username,
            user.DisplayName,
            user.NormalizedEmail,
            session.ExpiresAtUtc,
            session.AbsoluteExpiresAtUtc));
    }

    private static ApplicationResult<PlatformLoginResultDto> LoginFailedResult() =>
        ApplicationResult<PlatformLoginResultDto>.Failure(
            ApplicationErrorCodes.LoginFailed,
            "Invalid username/email or password.");

    private async Task WriteLoginFailedAsync(
        PlatformUserId? userId,
        CancellationToken cancellationToken,
        string? errorCode = null,
        string? detail = null)
    {
        await _auditWriter.WriteAsync(
            userId is null ? "anonymous" : $"platform-user:{userId.Value:D}",
            userId is null ? AuditActorType.System : AuditActorType.PlatformUser,
            PlatformAuditActions.PlatformAuthLoginFailed,
            nameof(PlatformUser),
            userId?.Value.ToString("D") ?? "unknown",
            AuditOutcome.Denied,
            summary: detail ?? "Login failed (credentials not recorded).",
            reason: errorCode ?? ApplicationErrorCodes.LoginFailed,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

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

public sealed class LogoutPlatformSession
{
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IPlatformSessionTokenService _tokens;

    public LogoutPlatformSession(
        IPlatformAuthSessionRepository sessions,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IPlatformSessionTokenService tokens)
    {
        _sessions = sessions;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _tokens = tokens;
    }

    public async Task<ApplicationResult<bool>> ExecuteAsync(
        string? opaqueToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(opaqueToken))
        {
            return ApplicationResult<bool>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Session is invalid.");
        }

        var session = await _sessions
            .GetByTokenHashAsync(_tokens.HashToken(opaqueToken), cancellationToken)
            .ConfigureAwait(false);
        if (session is null)
        {
            return ApplicationResult<bool>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Session is invalid.");
        }

        var utcNow = _clock.UtcNow;
        session.Revoke(utcNow);
        await _sessions.UpdateAsync(session, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditWriter.WriteAsync(
            $"platform-user:{session.UserId.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PlatformAuthLogout,
            nameof(PlatformAuthSession),
            session.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            summary: "Platform User browser session revoked (token not recorded).",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ApplicationResult<bool>.Success(true);
    }
}

public sealed class ValidateAndRenewPlatformSession
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IPlatformSessionTokenService _tokens;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PlatformSessionOptions _sessionOptions;

    public ValidateAndRenewPlatformSession(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformAuthSessionRepository sessions,
        IPlatformSessionTokenService tokens,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IOptions<PlatformSessionOptions> sessionOptions)
    {
        _users = users;
        _credentials = credentials;
        _sessions = sessions;
        _tokens = tokens;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _sessionOptions = sessionOptions.Value;
    }

    public async Task<ApplicationResult<PlatformAuthSessionInfoDto>> ExecuteAsync(
        string? opaqueToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(opaqueToken))
        {
            return ApplicationResult<PlatformAuthSessionInfoDto>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Session is invalid.");
        }

        var utcNow = _clock.UtcNow;
        var session = await _sessions
            .GetByTokenHashAsync(_tokens.HashToken(opaqueToken), cancellationToken)
            .ConfigureAwait(false);
        if (session is null)
        {
            return ApplicationResult<PlatformAuthSessionInfoDto>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Session is invalid.");
        }

        if (session.RevokedAtUtc is not null)
        {
            return ApplicationResult<PlatformAuthSessionInfoDto>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Session is invalid.");
        }

        if (session.ExpiresAtUtc <= utcNow || session.AbsoluteExpiresAtUtc <= utcNow)
        {
            return ApplicationResult<PlatformAuthSessionInfoDto>.Failure(
                ApplicationErrorCodes.SessionExpired,
                "Session has expired.");
        }

        var user = await _users.GetByIdAsync(session.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status is not AccountStatus.Active)
        {
            return ApplicationResult<PlatformAuthSessionInfoDto>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Session is invalid.");
        }

        var credential = await _credentials.GetByUserIdAsync(user.Id, cancellationToken).ConfigureAwait(false);
        if (credential is null
            || !string.Equals(credential.SecurityStamp, session.SecurityStampAtIssue, StringComparison.Ordinal))
        {
            return ApplicationResult<PlatformAuthSessionInfoDto>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Session is invalid.");
        }

        if (_sessionOptions.SlidingRenewal)
        {
            var idle = TimeSpan.FromMinutes(Math.Max(1, _sessionOptions.IdleTimeoutMinutes));
            session.RecordActivity(utcNow, idle);
            await _sessions.UpdateAsync(session, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return ApplicationResult<PlatformAuthSessionInfoDto>.Success(new PlatformAuthSessionInfoDto(
            session.Id.Value,
            user.Id.Value,
            user.Username,
            user.DisplayName,
            user.NormalizedEmail,
            session.ExpiresAtUtc,
            session.AbsoluteExpiresAtUtc,
            session.LastActivityAtUtc));
    }
}
