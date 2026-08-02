using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Application.Identity;

public sealed record PlatformUserDto(
    Guid Id,
    string Username,
    string DisplayName,
    string Email,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? SuspendedAtUtc,
    string? SuspensionReason,
    IReadOnlyList<string> AccountClasses,
    IReadOnlyList<string> OrganizationNames);

/// <summary>Directory extras for Platform Admin user lists (account class + org memberships).</summary>
public sealed record PlatformUserDirectoryExtras(
    IReadOnlyList<string> AccountClasses,
    IReadOnlyList<string> OrganizationNames);

public sealed class PlatformUserQueryService
{
    private static readonly PlatformUserDirectoryExtras EmptyExtras = new([], []);

    private readonly IPlatformUserRepository _users;

    public PlatformUserQueryService(IPlatformUserRepository users) => _users = users;

    public async Task<PlatformUserDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(PlatformUserId.From(id), cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return null;
        }

        var extras = await _users
            .GetDirectoryExtrasAsync([id], cancellationToken)
            .ConfigureAwait(false);
        extras.TryGetValue(id, out var directory);
        return Map(user, directory);
    }

    public async Task<PagedResult<PlatformUserDto>> ListAsync(
        AccountStatus? status,
        string? search,
        int? page,
        int? pageSize,
        UserDirectoryFilter? directoryFilter = null,
        string? sortBy = null,
        bool sortDesc = false,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, total) = await _users
            .ListAsync(status, search, directoryFilter, sortBy, sortDesc, skip, take, cancellationToken)
            .ConfigureAwait(false);
        var extras = await _users
            .GetDirectoryExtrasAsync(items.Select(u => u.Id.Value).ToList(), cancellationToken)
            .ConfigureAwait(false);
        return new PagedResult<PlatformUserDto>(
            items.Select(u =>
            {
                extras.TryGetValue(u.Id.Value, out var directory);
                return Map(u, directory);
            }).ToList(),
            total,
            Math.Max(page ?? 1, 1),
            take);
    }

    public static PlatformUserDto Map(PlatformUser user, PlatformUserDirectoryExtras? extras = null)
    {
        extras ??= EmptyExtras;
        return new(
            user.Id.Value,
            user.Username,
            user.DisplayName,
            user.NormalizedEmail,
            user.Status.ToString(),
            user.CreatedAtUtc,
            user.UpdatedAtUtc,
            user.SuspendedAtUtc,
            user.SuspensionReason,
            extras.AccountClasses,
            extras.OrganizationNames);
    }
}

public sealed class CreatePlatformUser
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreatePlatformUser(IPlatformUserRepository users, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformUser>> ExecuteAsync(
        string username,
        string displayName,
        string email,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = PlatformUser.Create(username, displayName, email, _clock.UtcNow);

            if (await _users.GetByNormalizedUsernameAsync(user.NormalizedUsername, cancellationToken).ConfigureAwait(false) is not null)
            {
                return ApplicationResult<PlatformUser>.Failure(
                    ApplicationErrorCodes.UsernameConflict,
                    "A Platform User with this username already exists.");
            }

            if (await _users.GetByNormalizedEmailAsync(user.NormalizedEmail, cancellationToken).ConfigureAwait(false) is not null)
            {
                return ApplicationResult<PlatformUser>.Failure(
                    ApplicationErrorCodes.EmailConflict,
                    "A Platform User with this email already exists.");
            }

            await _users.AddAsync(user, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PlatformUser>.Success(user);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformUser>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UpdatePlatformUserProfile
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdatePlatformUserProfile(IPlatformUserRepository users, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformUser>> ExecuteAsync(
        PlatformUserId userId,
        string displayName,
        string email,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PlatformUser>.Failure(ApplicationErrorCodes.UserNotFound, "Platform User was not found.");
        }

        try
        {
            var normalizedEmail = PlatformUser.NormalizeEmail(email);
            if (!string.Equals(normalizedEmail, user.NormalizedEmail, StringComparison.Ordinal))
            {
                var existing = await _users.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false);
                if (existing is not null && existing.Id != user.Id)
                {
                    return ApplicationResult<PlatformUser>.Failure(
                        ApplicationErrorCodes.EmailConflict,
                        "A Platform User with this email already exists.");
                }
            }

            user.UpdateProfile(displayName, email, _clock.UtcNow);
            await _users.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PlatformUser>.Success(user);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformUser>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class SuspendPlatformUser
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IPlatformAccessTokenRepository _accessTokens;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SuspendPlatformUser(
        IPlatformUserRepository users,
        IPlatformAuthSessionRepository sessions,
        IPlatformAccessTokenRepository accessTokens,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _sessions = sessions;
        _accessTokens = accessTokens;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformUser>> ExecuteAsync(
        PlatformUserId userId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PlatformUser>.Failure(ApplicationErrorCodes.UserNotFound, "Platform User was not found.");
        }

        try
        {
            var utcNow = _clock.UtcNow;
            user.Suspend(utcNow, reason);
            await _users.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
            await CredentialSessionInvalidation.RevokeAllAsync(
                _sessions,
                _accessTokens,
                _auditWriter,
                userId,
                utcNow,
                "All active sessions and access tokens revoked after Platform User suspend.",
                cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PlatformUser>.Success(user);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformUser>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ReactivatePlatformUser
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReactivatePlatformUser(IPlatformUserRepository users, IPlatformUnitOfWork unitOfWork, IClock clock)
    {
        _users = users;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformUser>> ExecuteAsync(
        PlatformUserId userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PlatformUser>.Failure(ApplicationErrorCodes.UserNotFound, "Platform User was not found.");
        }

        try
        {
            user.Reactivate(_clock.UtcNow);
            await _users.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PlatformUser>.Success(user);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformUser>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class DeactivatePlatformUser
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IPlatformAccessTokenRepository _accessTokens;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeactivatePlatformUser(
        IPlatformUserRepository users,
        IPlatformAuthSessionRepository sessions,
        IPlatformAccessTokenRepository accessTokens,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _sessions = sessions;
        _accessTokens = accessTokens;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformUser>> ExecuteAsync(
        PlatformUserId userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PlatformUser>.Failure(ApplicationErrorCodes.UserNotFound, "Platform User was not found.");
        }

        try
        {
            var utcNow = _clock.UtcNow;
            user.Deactivate(utcNow);
            await _users.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
            await CredentialSessionInvalidation.RevokeAllAsync(
                _sessions,
                _accessTokens,
                _auditWriter,
                userId,
                utcNow,
                "All active sessions and access tokens revoked after Platform User deactivate.",
                cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PlatformUser>.Success(user);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PlatformUser>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
