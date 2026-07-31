using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Application.Identity;

public sealed record PlatformCredentialStatusDto(
    Guid UserId,
    bool HasPassword,
    bool EmailVerified,
    DateTimeOffset? EmailVerifiedAtUtc,
    bool IsLockedOut,
    DateTimeOffset? LockoutEndUtc,
    int FailedAccessCount,
    DateTimeOffset? PasswordChangedAtUtc);

public static class PlatformPasswordPolicy
{
    public static string? Validate(string? password, PlatformPasswordOptions options)
    {
        if (string.IsNullOrEmpty(password))
        {
            return "Password is required.";
        }

        if (password.Length < options.MinimumLength)
        {
            return $"Password must be at least {options.MinimumLength} characters.";
        }

        if (password.Length > options.MaximumLength)
        {
            return $"Password must be at most {options.MaximumLength} characters.";
        }

        if (options.RequireUppercase && !password.Any(char.IsUpper))
        {
            return "Password must contain an uppercase letter.";
        }

        if (options.RequireLowercase && !password.Any(char.IsLower))
        {
            return "Password must contain a lowercase letter.";
        }

        if (options.RequireDigit && !password.Any(char.IsDigit))
        {
            return "Password must contain a digit.";
        }

        if (options.RequireNonAlphanumeric && password.All(char.IsLetterOrDigit))
        {
            return "Password must contain a non-alphanumeric character.";
        }

        return null;
    }
}

public sealed class GetPlatformCredentialStatus
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IClock _clock;

    public GetPlatformCredentialStatus(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IClock clock)
    {
        _users = users;
        _credentials = credentials;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformCredentialStatusDto>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var id = PlatformUserId.From(userId);
        var user = await _users.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "Platform User was not found.");
        }

        var credential = await _credentials.GetByUserIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (credential is null)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Success(
                new PlatformCredentialStatusDto(
                    userId,
                    HasPassword: false,
                    EmailVerified: false,
                    EmailVerifiedAtUtc: null,
                    IsLockedOut: false,
                    LockoutEndUtc: null,
                    FailedAccessCount: 0,
                    PasswordChangedAtUtc: null));
        }

        return ApplicationResult<PlatformCredentialStatusDto>.Success(
            new PlatformCredentialStatusDto(
                userId,
                HasPassword: credential.SupportsPasswordLogin,
                EmailVerified: credential.EmailVerifiedAtUtc is not null,
                EmailVerifiedAtUtc: credential.EmailVerifiedAtUtc,
                IsLockedOut: credential.IsLockedOut(_clock.UtcNow),
                LockoutEndUtc: credential.LockoutEndUtc,
                FailedAccessCount: credential.FailedAccessCount,
                PasswordChangedAtUtc: credential.PasswordChangedAtUtc));
    }
}

public sealed class SetPlatformUserPassword
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IPlatformAccessTokenRepository _accessTokens;
    private readonly IPlatformPasswordHasher _hasher;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PlatformPasswordOptions _passwordOptions;

    public SetPlatformUserPassword(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformAuthSessionRepository sessions,
        IPlatformAccessTokenRepository accessTokens,
        IPlatformPasswordHasher hasher,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IOptions<PlatformPasswordOptions> passwordOptions)
    {
        _users = users;
        _credentials = credentials;
        _sessions = sessions;
        _accessTokens = accessTokens;
        _hasher = hasher;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _passwordOptions = passwordOptions.Value;
    }

    public async Task<ApplicationResult<PlatformCredentialStatusDto>> ExecuteAsync(
        Guid userId,
        string password,
        CancellationToken cancellationToken = default)
    {
        var policyError = PlatformPasswordPolicy.Validate(password, _passwordOptions);
        if (policyError is not null)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.PasswordInvalid,
                policyError);
        }

        var id = PlatformUserId.From(userId);
        var user = await _users.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "Platform User was not found.");
        }

        try
        {
            var utcNow = _clock.UtcNow;
            var hash = _hasher.HashPassword(password);
            var existing = await _credentials.GetByUserIdAsync(id, cancellationToken).ConfigureAwait(false);
            if (existing is null)
            {
                var created = PlatformUserCredential.Create(id, hash, _hasher.Algorithm, utcNow);
                await _credentials.AddAsync(created, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                existing.ReplacePasswordHash(hash, _hasher.Algorithm, utcNow);
                await _credentials.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            }

            await CredentialSessionInvalidation.RevokeAllAsync(
                _sessions,
                _accessTokens,
                _auditWriter,
                id,
                utcNow,
                "All browser sessions revoked after administrative password set.",
                cancellationToken).ConfigureAwait(false);

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(ex.ErrorCode, ex.Message);
        }

        return await new GetPlatformCredentialStatus(_users, _credentials, _clock)
            .ExecuteAsync(userId, cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class UnlockPlatformUserCredential
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UnlockPlatformUserCredential(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _credentials = credentials;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformCredentialStatusDto>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var id = PlatformUserId.From(userId);
        if (await _users.GetByIdAsync(id, cancellationToken).ConfigureAwait(false) is null)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "Platform User was not found.");
        }

        var credential = await _credentials.GetByUserIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (credential is null)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.CredentialNotFound,
                "Platform User has no credential.");
        }

        credential.Unlock(_clock.UtcNow);
        await _credentials.UpdateAsync(credential, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await new GetPlatformCredentialStatus(_users, _credentials, _clock)
            .ExecuteAsync(userId, cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class MarkPlatformUserEmailVerified
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public MarkPlatformUserEmailVerified(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _credentials = credentials;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformCredentialStatusDto>> ExecuteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var id = PlatformUserId.From(userId);
        if (await _users.GetByIdAsync(id, cancellationToken).ConfigureAwait(false) is null)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "Platform User was not found.");
        }

        var credential = await _credentials.GetByUserIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (credential is null)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.CredentialNotFound,
                "Platform User has no credential.");
        }

        credential.MarkEmailVerified(_clock.UtcNow);
        await _credentials.UpdateAsync(credential, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await new GetPlatformCredentialStatus(_users, _credentials, _clock)
            .ExecuteAsync(userId, cancellationToken)
            .ConfigureAwait(false);
    }
}

/// <summary>
/// Application-layer password verification for later login WPs and tests.
/// Does not issue sessions or tokens.
/// </summary>
public sealed class VerifyPlatformUserPassword
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformPasswordHasher _hasher;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PlatformLockoutOptions _lockoutOptions;

    public VerifyPlatformUserPassword(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformPasswordHasher hasher,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IOptions<PlatformLockoutOptions> lockoutOptions)
    {
        _users = users;
        _credentials = credentials;
        _hasher = hasher;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _lockoutOptions = lockoutOptions.Value;
    }

    public async Task<ApplicationResult<PlatformCredentialStatusDto>> ExecuteAsync(
        Guid userId,
        string password,
        CancellationToken cancellationToken = default)
    {
        var id = PlatformUserId.From(userId);
        var user = await _users.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "Platform User was not found.");
        }

        var credential = await _credentials.GetByUserIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (credential is null || !credential.SupportsPasswordLogin)
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

        var verification = _hasher.VerifyHashedPassword(credential.PasswordHash, password ?? string.Empty);
        if (verification == PlatformPasswordVerificationResult.Failed)
        {
            credential.RegisterFailedAccess(
                _lockoutOptions.MaxFailedAccessAttempts,
                TimeSpan.FromMinutes(Math.Max(1, _lockoutOptions.LockoutMinutes)),
                utcNow);
            await _credentials.UpdateAsync(credential, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            if (credential.IsLockedOut(_clock.UtcNow))
            {
                return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                    ApplicationErrorCodes.CredentialLockedOut,
                    "Credential is locked out.");
            }

            return ApplicationResult<PlatformCredentialStatusDto>.Failure(
                ApplicationErrorCodes.PasswordInvalid,
                "Password verification failed.");
        }

        if (verification == PlatformPasswordVerificationResult.SuccessRehashNeeded)
        {
            var rehash = _hasher.HashPassword(password!);
            credential.ReplacePasswordHash(rehash, _hasher.Algorithm, utcNow);
        }

        credential.RegisterSuccessfulAccess(utcNow);
        await _credentials.UpdateAsync(credential, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return await new GetPlatformCredentialStatus(_users, _credentials, _clock)
            .ExecuteAsync(userId, cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class BootstrapFirstPlatformAdministrator
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IPlatformRoleAssignmentRepository _roles;
    private readonly IPlatformPasswordHasher _hasher;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PlatformAuthBootstrapOptions _bootstrap;
    private readonly PlatformPasswordOptions _passwordOptions;

    public BootstrapFirstPlatformAdministrator(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IPlatformRoleAssignmentRepository roles,
        IPlatformPasswordHasher hasher,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IOptions<PlatformAuthBootstrapOptions> bootstrap,
        IOptions<PlatformPasswordOptions> passwordOptions)
    {
        _users = users;
        _credentials = credentials;
        _roles = roles;
        _hasher = hasher;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _bootstrap = bootstrap.Value;
        _passwordOptions = passwordOptions.Value;
    }

    public async Task<ApplicationResult<PlatformUserDto>> ExecuteAsync(
        string? providedSharedSecret,
        bool isProductionEnvironment,
        CancellationToken cancellationToken = default)
    {
        if (isProductionEnvironment)
        {
            return ApplicationResult<PlatformUserDto>.Failure(
                ApplicationErrorCodes.BootstrapForbiddenInEnvironment,
                "First-admin bootstrap is forbidden in Production.");
        }

        if (!_bootstrap.Enabled)
        {
            return ApplicationResult<PlatformUserDto>.Failure(
                ApplicationErrorCodes.BootstrapDisabled,
                "First-admin bootstrap is disabled.");
        }

        if (string.IsNullOrWhiteSpace(_bootstrap.SharedSecret)
            || _bootstrap.SharedSecret.Length < PlatformAuthBootstrapOptions.MinimumSharedSecretLength)
        {
            return ApplicationResult<PlatformUserDto>.Failure(
                ApplicationErrorCodes.BootstrapConfigurationInvalid,
                "Bootstrap SharedSecret must be configured (minimum 32 characters).");
        }

        if (!BootstrapSecretComparer.EqualsConfigured(_bootstrap.SharedSecret, providedSharedSecret))
        {
            return ApplicationResult<PlatformUserDto>.Failure(
                ApplicationErrorCodes.BootstrapUnauthorized,
                "Bootstrap authorization failed.");
        }

        var (_, adminCount) = await _roles.ListAsync(
            userId: null,
            role: PlatformSystemRole.PlatformAdministrator,
            organizationId: null,
            status: PlatformRoleAssignmentStatus.Active,
            skip: 0,
            take: 1,
            cancellationToken).ConfigureAwait(false);
        if (adminCount > 0)
        {
            return ApplicationResult<PlatformUserDto>.Failure(
                ApplicationErrorCodes.BootstrapAlreadyCompleted,
                "A Platform Administrator already exists.");
        }

        if (string.IsNullOrWhiteSpace(_bootstrap.Username)
            || string.IsNullOrWhiteSpace(_bootstrap.Email)
            || string.IsNullOrWhiteSpace(_bootstrap.DisplayName)
            || string.IsNullOrWhiteSpace(_bootstrap.Password))
        {
            return ApplicationResult<PlatformUserDto>.Failure(
                ApplicationErrorCodes.BootstrapConfigurationInvalid,
                "Bootstrap username, display name, email, and password must be configured.");
        }

        var policyError = PlatformPasswordPolicy.Validate(_bootstrap.Password, _passwordOptions);
        if (policyError is not null)
        {
            return ApplicationResult<PlatformUserDto>.Failure(
                ApplicationErrorCodes.PasswordInvalid,
                policyError);
        }

        try
        {
            var user = PlatformUser.Create(
                _bootstrap.Username,
                _bootstrap.DisplayName,
                _bootstrap.Email,
                _clock.UtcNow);

            if (await _users.GetByNormalizedUsernameAsync(user.NormalizedUsername, cancellationToken).ConfigureAwait(false) is not null
                || await _users.GetByNormalizedEmailAsync(user.NormalizedEmail, cancellationToken).ConfigureAwait(false) is not null)
            {
                return ApplicationResult<PlatformUserDto>.Failure(
                    ApplicationErrorCodes.BootstrapConfigurationInvalid,
                    "Bootstrap user username or email already exists.");
            }

            await _users.AddAsync(user, cancellationToken).ConfigureAwait(false);

            var hash = _hasher.HashPassword(_bootstrap.Password);
            var credential = PlatformUserCredential.Create(user.Id, hash, _hasher.Algorithm, _clock.UtcNow);
            credential.MarkEmailVerified(_clock.UtcNow);
            await _credentials.AddAsync(credential, cancellationToken).ConfigureAwait(false);

            var assignment = PlatformRoleAssignment.Grant(
                user.Id,
                PlatformSystemRole.PlatformAdministrator,
                organizationId: null,
                grantedByActor: "system:auth-bootstrap",
                utcNow: _clock.UtcNow,
                reason: "First Platform Administrator bootstrap");
            await _roles.AddAsync(assignment, cancellationToken).ConfigureAwait(false);

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                "system:auth-bootstrap",
                AuditActorType.System,
                PlatformAuditActions.PlatformAuthBootstrapCompleted,
                nameof(PlatformUser),
                user.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Bootstrapped first Platform Administrator (credentials set; password and bootstrap secret not recorded).",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PlatformUserDto>.Success(PlatformUserQueryService.Map(user));
        }
        catch (PersistenceConflictException ex) when (
            ex.ErrorCode is ApplicationErrorCodes.RoleAssignmentConflict
                or ApplicationErrorCodes.UsernameConflict
                or ApplicationErrorCodes.EmailConflict
                or ApplicationErrorCodes.DomainViolation)
        {
            return ApplicationResult<PlatformUserDto>.Failure(
                ApplicationErrorCodes.BootstrapAlreadyCompleted,
                "A Platform Administrator already exists.");
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformUserDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
