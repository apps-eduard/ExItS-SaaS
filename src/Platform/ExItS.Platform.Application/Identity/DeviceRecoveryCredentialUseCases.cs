using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Application.Identity;

public sealed class EnrollDeviceRecoveryCredential
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformDeviceRecoveryCredentialRepository _recoveryCredentials;
    private readonly IPlatformSessionTokenService _tokenService;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PlatformDeviceRecoveryCredentialOptions _options;

    public EnrollDeviceRecoveryCredential(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformDeviceRecoveryCredentialRepository recoveryCredentials,
        IPlatformSessionTokenService tokenService,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IOptions<PlatformDeviceRecoveryCredentialOptions> options)
    {
        _users = users;
        _credentials = credentials;
        _recoveryCredentials = recoveryCredentials;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<ApplicationResult<DeviceRecoveryCredentialEnrollDto>> ExecuteAsync(
        Guid authenticatedUserId,
        string installationDeviceId,
        CancellationToken cancellationToken = default)
    {
        if (authenticatedUserId == Guid.Empty)
        {
            return ApplicationResult<DeviceRecoveryCredentialEnrollDto>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Authentication is required.");
        }

        if (string.IsNullOrWhiteSpace(installationDeviceId))
        {
            return ApplicationResult<DeviceRecoveryCredentialEnrollDto>.Failure(
                ApplicationErrorCodes.RecoveryCredentialDeviceMismatch,
                "Installation device id is required.");
        }

        var userId = PlatformUserId.From(authenticatedUserId);
        var user = await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status is not AccountStatus.Active)
        {
            return ApplicationResult<DeviceRecoveryCredentialEnrollDto>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Authentication is required.");
        }

        var credential = await _credentials.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (credential is null)
        {
            return ApplicationResult<DeviceRecoveryCredentialEnrollDto>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Authentication is required.");
        }

        var utcNow = _clock.UtcNow;
        var normalizedDeviceId = installationDeviceId.Trim();
        await _recoveryCredentials
            .RevokeActiveForUserAndDeviceAsync(userId, normalizedDeviceId, utcNow, cancellationToken)
            .ConfigureAwait(false);

        var rawToken = _tokenService.CreateOpaqueToken();
        var idleLifetime = _options.ResolveIdleLifetime();
        var absoluteLifetime = _options.ResolveAbsoluteLifetime();
        var recovery = PlatformDeviceRecoveryCredential.Create(
            userId,
            normalizedDeviceId,
            _tokenService.HashToken(rawToken),
            credential.SecurityStamp,
            utcNow,
            idleLifetime,
            absoluteLifetime);

        await _recoveryCredentials.AddAsync(recovery, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ApplicationResult<DeviceRecoveryCredentialEnrollDto>.Success(
            new DeviceRecoveryCredentialEnrollDto(
                rawToken,
                recovery.IdleExpiresAtUtc,
                recovery.AbsoluteExpiresAtUtc));
    }
}

public sealed class ExchangeDeviceRecoveryCredential
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformDeviceRecoveryCredentialRepository _recoveryCredentials;
    private readonly IssuePlatformAccessToken _issueAccessToken;
    private readonly IPlatformSessionTokenService _tokenService;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PlatformDeviceRecoveryCredentialOptions _options;

    public ExchangeDeviceRecoveryCredential(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformDeviceRecoveryCredentialRepository recoveryCredentials,
        IssuePlatformAccessToken issueAccessToken,
        IPlatformSessionTokenService tokenService,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IOptions<PlatformDeviceRecoveryCredentialOptions> options)
    {
        _users = users;
        _credentials = credentials;
        _recoveryCredentials = recoveryCredentials;
        _issueAccessToken = issueAccessToken;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _options = options.Value;
    }

    public async Task<ApplicationResult<DeviceRecoveryCredentialExchangeDto>> ExecuteAsync(
        string? recoveryCredential,
        string installationDeviceId,
        Guid? organizationId,
        string? productCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(recoveryCredential))
        {
            return ApplicationResult<DeviceRecoveryCredentialExchangeDto>.Failure(
                ApplicationErrorCodes.RecoveryCredentialInvalid,
                "Recovery credential is invalid.");
        }

        if (string.IsNullOrWhiteSpace(installationDeviceId))
        {
            return ApplicationResult<DeviceRecoveryCredentialExchangeDto>.Failure(
                ApplicationErrorCodes.RecoveryCredentialDeviceMismatch,
                "Installation device id is required.");
        }

        var utcNow = _clock.UtcNow;
        var normalizedDeviceId = installationDeviceId.Trim();
        var existing = await _recoveryCredentials
            .GetByTokenHashAsync(_tokenService.HashToken(recoveryCredential.Trim()), cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            return ApplicationResult<DeviceRecoveryCredentialExchangeDto>.Failure(
                ApplicationErrorCodes.RecoveryCredentialInvalid,
                "Recovery credential is invalid.");
        }

        if (!string.Equals(existing.InstallationDeviceId, normalizedDeviceId, StringComparison.Ordinal))
        {
            return ApplicationResult<DeviceRecoveryCredentialExchangeDto>.Failure(
                ApplicationErrorCodes.RecoveryCredentialDeviceMismatch,
                "Recovery credential is not valid for this device.");
        }

        if (existing.RevokedAtUtc is not null)
        {
            return ApplicationResult<DeviceRecoveryCredentialExchangeDto>.Failure(
                ApplicationErrorCodes.RecoveryCredentialRevoked,
                "Recovery credential has been revoked.");
        }

        if (existing.AbsoluteExpiresAtUtc <= utcNow)
        {
            return ApplicationResult<DeviceRecoveryCredentialExchangeDto>.Failure(
                ApplicationErrorCodes.RecoveryCredentialExpired,
                "Recovery credential has expired.");
        }

        if (existing.IdleExpiresAtUtc <= utcNow)
        {
            return ApplicationResult<DeviceRecoveryCredentialExchangeDto>.Failure(
                ApplicationErrorCodes.RecoveryCredentialExpired,
                "Recovery credential has expired due to inactivity.");
        }

        var user = await _users.GetByIdAsync(existing.UserId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status is not AccountStatus.Active)
        {
            return ApplicationResult<DeviceRecoveryCredentialExchangeDto>.Failure(
                ApplicationErrorCodes.AccountNotEligibleForLogin,
                "Account is not eligible for login.");
        }

        var userCredential = await _credentials.GetByUserIdAsync(existing.UserId, cancellationToken).ConfigureAwait(false);
        if (userCredential is null
            || !string.Equals(userCredential.SecurityStamp, existing.SecurityStampAtIssue, StringComparison.Ordinal))
        {
            existing.Revoke(utcNow);
            await _recoveryCredentials.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<DeviceRecoveryCredentialExchangeDto>.Failure(
                ApplicationErrorCodes.RecoveryCredentialRevoked,
                "Recovery credential has been revoked.");
        }

        var rawToken = _tokenService.CreateOpaqueToken();
        var rotated = PlatformDeviceRecoveryCredential.CreateRotated(
            existing,
            _tokenService.HashToken(rawToken),
            userCredential.SecurityStamp,
            utcNow,
            _options.ResolveIdleLifetime());

        existing.Revoke(utcNow);
        await _recoveryCredentials.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        await _recoveryCredentials.AddAsync(rotated, cancellationToken).ConfigureAwait(false);

        var accessToken = await _issueAccessToken
            .IssueForActiveUserAsync(existing.UserId.Value, organizationId, productCode, cancellationToken)
            .ConfigureAwait(false);
        if (!accessToken.IsSuccess || accessToken.Value is null)
        {
            return ApplicationResult<DeviceRecoveryCredentialExchangeDto>.Failure(
                accessToken.ErrorCode ?? ApplicationErrorCodes.AccessTokenInvalid,
                accessToken.ErrorMessage ?? "Access token could not be issued.");
        }

        try
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (PersistenceConflictException)
        {
            return ApplicationResult<DeviceRecoveryCredentialExchangeDto>.Failure(
                ApplicationErrorCodes.RecoveryCredentialInvalid,
                "Recovery credential is invalid.");
        }

        return ApplicationResult<DeviceRecoveryCredentialExchangeDto>.Success(
            new DeviceRecoveryCredentialExchangeDto(
                accessToken.Value,
                rawToken,
                rotated.IdleExpiresAtUtc,
                rotated.AbsoluteExpiresAtUtc));
    }
}

public sealed class RevokeDeviceRecoveryCredential
{
    private readonly IPlatformDeviceRecoveryCredentialRepository _recoveryCredentials;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RevokeDeviceRecoveryCredential(
        IPlatformDeviceRecoveryCredentialRepository recoveryCredentials,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _recoveryCredentials = recoveryCredentials;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<object>> ExecuteForUserAndDeviceAsync(
        Guid authenticatedUserId,
        string installationDeviceId,
        CancellationToken cancellationToken = default)
    {
        if (authenticatedUserId == Guid.Empty || string.IsNullOrWhiteSpace(installationDeviceId))
        {
            return ApplicationResult<object>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Authentication is required.");
        }

        await _recoveryCredentials
            .RevokeActiveForUserAndDeviceAsync(
                PlatformUserId.From(authenticatedUserId),
                installationDeviceId.Trim(),
                _clock.UtcNow,
                cancellationToken)
            .ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<object>.Success(new object());
    }

    public async Task<ApplicationResult<object>> ExecuteForUserAsync(
        Guid authenticatedUserId,
        CancellationToken cancellationToken = default)
    {
        if (authenticatedUserId == Guid.Empty)
        {
            return ApplicationResult<object>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Authentication is required.");
        }

        await _recoveryCredentials
            .RevokeActiveForUserAsync(PlatformUserId.From(authenticatedUserId), _clock.UtcNow, cancellationToken)
            .ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<object>.Success(new object());
    }
}
