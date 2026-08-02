using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Application.Identity;

public sealed record PlatformUserOrganizationDirectoryItem(
    string Name,
    string Role,
    string RoleDisplay);

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
    IReadOnlyList<string> OrganizationNames,
    IReadOnlyList<PlatformUserOrganizationDirectoryItem>? Organizations = null,
    string? FirstName = null,
    string? LastName = null,
    string? Phone = null,
    string? EmployeeCode = null,
    string? StaffNumber = null,
    Guid? CreatedByUserId = null);

/// <summary>Directory extras for Platform Admin user lists (account class + org memberships).</summary>
public sealed record PlatformUserDirectoryExtras(
    IReadOnlyList<string> AccountClasses,
    IReadOnlyList<string> OrganizationNames,
    IReadOnlyList<PlatformUserOrganizationDirectoryItem>? Organizations = null);

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
            extras.OrganizationNames,
            extras.Organizations ?? Array.Empty<PlatformUserOrganizationDirectoryItem>(),
            user.FirstName,
            user.LastName,
            user.Phone,
            user.EmployeeCode,
            user.StaffNumber,
            user.CreatedByUserId?.Value);
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
        bool requireEmailVerification = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = requireEmailVerification
                ? PlatformUser.CreatePendingVerification(username, displayName, email, _clock.UtcNow)
                : PlatformUser.Create(username, displayName, email, _clock.UtcNow);

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

    public async Task<ApplicationResult<PlatformUser>> ExecuteForStaffAsync(
        string? username,
        string firstName,
        string lastName,
        string displayName,
        string email,
        string staffNumber,
        bool requireEmailVerification = false,
        string? phone = null,
        string? employeeCode = null,
        Guid? createdByUserId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var resolvedUsername = await ResolveUsernameAsync(username, email, cancellationToken).ConfigureAwait(false);
            if (!resolvedUsername.IsSuccess)
            {
                return ApplicationResult<PlatformUser>.Failure(
                    resolvedUsername.ErrorCode ?? ApplicationErrorCodes.DomainViolation,
                    resolvedUsername.ErrorMessage ?? "Unable to resolve username.");
            }

            var user = PlatformUser.CreatePlatformStaff(
                resolvedUsername.Value!,
                firstName,
                lastName,
                displayName,
                email,
                staffNumber,
                _clock.UtcNow,
                phone,
                employeeCode,
                createdByUserId is null ? null : PlatformUserId.From(createdByUserId.Value),
                requireEmailVerification);

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

    private async Task<ApplicationResult<string>> ResolveUsernameAsync(
        string? username,
        string email,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(username))
        {
            try
            {
                var (display, normalized) = PlatformUser.NormalizeUsername(username);
                _ = display;
                if (await _users.GetByNormalizedUsernameAsync(normalized, cancellationToken).ConfigureAwait(false) is not null)
                {
                    return ApplicationResult<string>.Failure(
                        ApplicationErrorCodes.UsernameConflict,
                        "A Platform User with this username already exists.");
                }

                return ApplicationResult<string>.Success(display);
            }
            catch (DomainException ex)
            {
                return ApplicationResult<string>.Failure(ex.ErrorCode, ex.Message);
            }
        }

        var usernameBase = PlatformUsernameDerivation.DeriveFromEmail(email);
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var candidate = attempt == 0 ? usernameBase : $"{usernameBase}{attempt + 1}";
            if (candidate.Length < 3)
            {
                candidate = $"user{attempt + 1}{candidate}";
            }

            try
            {
                var (display, normalized) = PlatformUser.NormalizeUsername(candidate);
                _ = display;
                if (await _users.GetByNormalizedUsernameAsync(normalized, cancellationToken).ConfigureAwait(false) is null)
                {
                    return ApplicationResult<string>.Success(display);
                }
            }
            catch (DomainException)
            {
                continue;
            }
        }

        return ApplicationResult<string>.Failure(
            ApplicationErrorCodes.UsernameConflict,
            "Unable to allocate a unique username for the Platform Staff identity.");
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
        string? firstName = null,
        string? lastName = null,
        string? phone = null,
        string? employeeCode = null,
        string? staffNumber = null,
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

            if (user.StaffNumber is not null || firstName is not null || lastName is not null || phone is not null || employeeCode is not null || staffNumber is not null)
            {
                user.UpdateStaffProfile(
                    firstName,
                    lastName,
                    displayName,
                    email,
                    _clock.UtcNow,
                    phone,
                    employeeCode,
                    staffNumber);
            }
            else
            {
                user.UpdateProfile(displayName, email, _clock.UtcNow);
            }

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
    private readonly IPlatformRoleAssignmentRepository _roles;
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IPlatformAccessTokenRepository _accessTokens;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public SuspendPlatformUser(
        IPlatformUserRepository users,
        IPlatformRoleAssignmentRepository roles,
        IPlatformAuthSessionRepository sessions,
        IPlatformAccessTokenRepository accessTokens,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _roles = roles;
        _sessions = sessions;
        _accessTokens = accessTokens;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformUser>> ExecuteAsync(
        PlatformUserId userId,
        string? reason = null,
        bool requireReason = false,
        CancellationToken cancellationToken = default)
    {
        if (requireReason && string.IsNullOrWhiteSpace(reason))
        {
            return ApplicationResult<PlatformUser>.Failure(
                ApplicationErrorCodes.DomainViolation,
                "A reason is required for this suspension.");
        }

        var user = await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PlatformUser>.Failure(ApplicationErrorCodes.UserNotFound, "Platform User was not found.");
        }

        if (user.Status == AccountStatus.Active)
        {
            var guard = await PlatformAdministratorLifecycleGuard
                .EnsureCanBlockLoginAsync(_users, _roles, userId, cancellationToken)
                .ConfigureAwait(false);
            if (guard is not null)
            {
                return ApplicationResult<PlatformUser>.Failure(guard.ErrorCode!, guard.ErrorMessage!);
            }
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
    private readonly PlatformLifecycleStepUp _stepUp;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReactivatePlatformUser(
        IPlatformUserRepository users,
        PlatformLifecycleStepUp stepUp,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _stepUp = stepUp;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformUser>> ExecuteAsync(
        PlatformUserId userId,
        PlatformUserId? actingUserId = null,
        string? reason = null,
        string? actorPassword = null,
        string? mfaCode = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PlatformUser>.Failure(ApplicationErrorCodes.UserNotFound, "Platform User was not found.");
        }

        if (user.Status == AccountStatus.Deactivated)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                return ApplicationResult<PlatformUser>.Failure(
                    ApplicationErrorCodes.DomainViolation,
                    "A reactivation reason is required for a deactivated account.");
            }

            var stepUp = await _stepUp
                .VerifyAsync(actingUserId, actorPassword, mfaCode, cancellationToken)
                .ConfigureAwait(false);
            if (stepUp is not null)
            {
                return ApplicationResult<PlatformUser>.Failure(stepUp.ErrorCode!, stepUp.ErrorMessage!);
            }
        }

        try
        {
            user.Reactivate(_clock.UtcNow, user.Status == AccountStatus.Deactivated ? reason : null);
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
    private readonly IPlatformRoleAssignmentRepository _roles;
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IPlatformAccessTokenRepository _accessTokens;
    private readonly IAuditWriter _auditWriter;
    private readonly PlatformLifecycleStepUp _stepUp;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeactivatePlatformUser(
        IPlatformUserRepository users,
        IPlatformRoleAssignmentRepository roles,
        IPlatformAuthSessionRepository sessions,
        IPlatformAccessTokenRepository accessTokens,
        IAuditWriter auditWriter,
        PlatformLifecycleStepUp stepUp,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _roles = roles;
        _sessions = sessions;
        _accessTokens = accessTokens;
        _auditWriter = auditWriter;
        _stepUp = stepUp;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformUser>> ExecuteAsync(
        PlatformUserId userId,
        string reason,
        PlatformUserId? actingUserId = null,
        string? actorPassword = null,
        string? mfaCode = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return ApplicationResult<PlatformUser>.Failure(
                ApplicationErrorCodes.DomainViolation,
                "A reason is required to deactivate a Platform User.");
        }

        var user = await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PlatformUser>.Failure(ApplicationErrorCodes.UserNotFound, "Platform User was not found.");
        }

        var stepUp = await _stepUp
            .VerifyAsync(actingUserId, actorPassword, mfaCode, cancellationToken)
            .ConfigureAwait(false);
        if (stepUp is not null)
        {
            return ApplicationResult<PlatformUser>.Failure(stepUp.ErrorCode!, stepUp.ErrorMessage!);
        }

        if (user.Status == AccountStatus.Active)
        {
            var guard = await PlatformAdministratorLifecycleGuard
                .EnsureCanBlockLoginAsync(_users, _roles, userId, cancellationToken)
                .ConfigureAwait(false);
            if (guard is not null)
            {
                return ApplicationResult<PlatformUser>.Failure(guard.ErrorCode!, guard.ErrorMessage!);
            }
        }

        try
        {
            var utcNow = _clock.UtcNow;
            user.Deactivate(utcNow, reason);
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

public sealed class MovePlatformUserToSuspended
{
    private readonly IPlatformUserRepository _users;
    private readonly PlatformLifecycleStepUp _stepUp;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public MovePlatformUserToSuspended(
        IPlatformUserRepository users,
        PlatformLifecycleStepUp stepUp,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _stepUp = stepUp;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PlatformUser>> ExecuteAsync(
        PlatformUserId userId,
        string reason,
        PlatformUserId? actingUserId = null,
        string? actorPassword = null,
        string? mfaCode = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return ApplicationResult<PlatformUser>.Failure(
                ApplicationErrorCodes.DomainViolation,
                "A reason is required to move a deactivated Platform User to Suspended.");
        }

        var user = await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PlatformUser>.Failure(ApplicationErrorCodes.UserNotFound, "Platform User was not found.");
        }

        var stepUp = await _stepUp
            .VerifyAsync(actingUserId, actorPassword, mfaCode, cancellationToken)
            .ConfigureAwait(false);
        if (stepUp is not null)
        {
            return ApplicationResult<PlatformUser>.Failure(stepUp.ErrorCode!, stepUp.ErrorMessage!);
        }

        try
        {
            user.MoveToSuspended(_clock.UtcNow, reason);
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
