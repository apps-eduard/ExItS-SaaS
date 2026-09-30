using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

public sealed record PersonalDashboardDto(
    Guid UserIdentityId,
    Guid AccountProfileId,
    string AccountClass,
    bool UtangAvailable,
    int ContactCount,
    int ActiveRelationshipCount,
    /// <summary>Sum of balances on relationships the viewer owns (My Records).</summary>
    decimal TotalLentBalance,
    /// <summary>Sum of balances on relationships the viewer owns (My Records).</summary>
    decimal TotalBorrowedBalance,
    int PendingConfirmationCount,
    /// <summary>Informational: shared-with-me Lent perspective (not included in TotalLentBalance).</summary>
    decimal SharedWithMeLentBalance = 0m,
    /// <summary>Informational: shared-with-me Borrowed perspective (not included in TotalBorrowedBalance).</summary>
    decimal SharedWithMeBorrowedBalance = 0m,
    int SharedWithMeActiveCount = 0);

public sealed record PersonalProfileDto(
    Guid UserIdentityId,
    Guid AccountProfileId,
    string Username,
    string DisplayName,
    string Email,
    string AccountClass,
    string Status,
    string? PublicUserId = null,
    string? QrPayload = null,
    string? Phone = null);

public sealed record PersonalAccountSettingsDto(
    Guid UserIdentityId,
    bool EmailNotificationsEnabled,
    bool PushNotificationsEnabled,
    bool InAppNotificationsEnabled,
    bool ReminderNotificationsEnabled,
    int Version,
    DateTimeOffset UpdatedAtUtc);

public sealed record UpdatePersonalAccountSettingsRequest(
    bool EmailNotificationsEnabled,
    bool PushNotificationsEnabled,
    bool InAppNotificationsEnabled,
    bool ReminderNotificationsEnabled,
    int? ExpectedVersion);

public sealed record UpdatePersonalProfileRequest(string DisplayName);

public sealed class GetPersonalDashboard
{
    private readonly IPlatformUserRepository _users;
    private readonly IAccountProfileRepository _profiles;
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPersonalUtangEntryRepository _entries;

    public GetPersonalDashboard(
        IPlatformUserRepository users,
        IAccountProfileRepository profiles,
        IPersonalContactRepository contacts,
        IPersonalDebtRelationshipRepository relationships,
        IPersonalUtangEntryRepository entries)
    {
        _users = users;
        _profiles = profiles;
        _contacts = contacts;
        _relationships = relationships;
        _entries = entries;
    }

    public async Task<ApplicationResult<PersonalDashboardDto>> ExecuteAsync(
        PlatformUserId userIdentityId,
        AccountProfileId accountProfileId,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userIdentityId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PersonalDashboardDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "User identity was not found.");
        }

        var profile = await _profiles.GetByIdAsync(accountProfileId, cancellationToken).ConfigureAwait(false);
        if (profile is null || profile.UserIdentityId != userIdentityId || profile.AccountClass is not AccountClass.Personal)
        {
            return ApplicationResult<PersonalDashboardDto>.Failure(
                ApplicationErrorCodes.AccountProfileNotAvailable,
                "Personal account profile is not available.");
        }

        var contacts = await _contacts.ListByOwnerAsync(userIdentityId, cancellationToken).ConfigureAwait(false);
        var relationships = await _relationships.ListForUserAsync(userIdentityId, cancellationToken).ConfigureAwait(false);
        var active = relationships.Where(r => r.Status is PersonalDebtRelationshipStatus.Active).ToList();

        decimal lent = 0m;
        decimal borrowed = 0m;
        decimal sharedLent = 0m;
        decimal sharedBorrowed = 0m;
        var sharedWithMeActive = 0;
        var ownedActive = 0;
        foreach (var relationship in active)
        {
            // Display totals may include legacy Pending signed amounts; CurrentBalance stays confirmed-only.
            var history = await _entries
                .ListByRelationshipAsync(relationship.Id, cancellationToken)
                .ConfigureAwait(false);
            var pendingDelta = history
                .Where(e => e.Status is PersonalUtangEntryStatus.Pending)
                .Sum(e => e.SignedDelta);
            var effectiveBalance = relationship.CurrentBalance + pendingDelta;

            var isOwned = relationship.IsLedgerOwner(userIdentityId);
            decimal? lentDelta = null;
            decimal? borrowedDelta = null;

            if (relationship.CreditorUserIdentityId == userIdentityId)
            {
                lentDelta = effectiveBalance;
            }
            else if (relationship.DebtorUserIdentityId == userIdentityId)
            {
                borrowedDelta = effectiveBalance;
            }
            else if (isOwned && relationship.DebtorContactId is not null)
            {
                // Private owner ledger: viewer is creditor (I Lent to contact).
                lentDelta = effectiveBalance;
            }
            else if (isOwned && relationship.CreditorContactId is not null)
            {
                // Private owner ledger: viewer is debtor (I Borrowed from contact).
                borrowedDelta = effectiveBalance;
            }

            if (isOwned)
            {
                ownedActive++;
                if (lentDelta is not null)
                {
                    lent += lentDelta.Value;
                }
                else if (borrowedDelta is not null)
                {
                    borrowed += borrowedDelta.Value;
                }
            }
            else
            {
                sharedWithMeActive++;
                if (lentDelta is not null)
                {
                    sharedLent += lentDelta.Value;
                }
                else if (borrowedDelta is not null)
                {
                    sharedBorrowed += borrowedDelta.Value;
                }
            }
        }

        var pendingConfirmationCount = await _entries
            .CountPendingAwaitingConfirmationAsync(userIdentityId, cancellationToken)
            .ConfigureAwait(false);

        return ApplicationResult<PersonalDashboardDto>.Success(new PersonalDashboardDto(
            userIdentityId.Value,
            accountProfileId.Value,
            AccountClass.Personal.ToString(),
            UtangAvailable: true,
            contacts.Count,
            ownedActive,
            lent,
            borrowed,
            pendingConfirmationCount,
            sharedLent,
            sharedBorrowed,
            sharedWithMeActive));
    }
}

public sealed class GetPersonalProfile
{
    private readonly IPlatformUserRepository _users;
    private readonly IAccountProfileRepository _profiles;
    private readonly GetOrAssignPublicIdentity _publicIdentity;

    public GetPersonalProfile(
        IPlatformUserRepository users,
        IAccountProfileRepository profiles,
        GetOrAssignPublicIdentity publicIdentity)
    {
        _users = users;
        _profiles = profiles;
        _publicIdentity = publicIdentity;
    }

    public async Task<ApplicationResult<PersonalProfileDto>> ExecuteAsync(
        PlatformUserId userIdentityId,
        AccountProfileId accountProfileId,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userIdentityId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PersonalProfileDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "User identity was not found.");
        }

        var profile = await _profiles.GetByIdAsync(accountProfileId, cancellationToken).ConfigureAwait(false);
        if (profile is null || profile.UserIdentityId != userIdentityId || profile.AccountClass is not AccountClass.Personal)
        {
            return ApplicationResult<PersonalProfileDto>.Failure(
                ApplicationErrorCodes.AccountProfileNotAvailable,
                "Personal account profile is not available.");
        }

        string? publicUserId = user.PublicUserId;
        string? qrPayload = null;
        var identity = await _publicIdentity.ExecuteAsync(userIdentityId, cancellationToken).ConfigureAwait(false);
        if (identity.IsSuccess && identity.Value is not null)
        {
            publicUserId = identity.Value.PublicUserId;
            qrPayload = identity.Value.QrPayload;
        }

        return ApplicationResult<PersonalProfileDto>.Success(new PersonalProfileDto(
            user.Id.Value,
            profile.Id.Value,
            user.Username,
            user.DisplayName,
            user.NormalizedEmail,
            profile.AccountClass.ToString(),
            profile.Status,
            publicUserId,
            qrPayload,
            user.Phone));
    }
}

public sealed class UpdatePersonalProfile
{
    private readonly IPlatformUserRepository _users;
    private readonly IAccountProfileRepository _profiles;
    private readonly GetPersonalProfile _getProfile;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdatePersonalProfile(
        IPlatformUserRepository users,
        IAccountProfileRepository profiles,
        GetPersonalProfile getProfile,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _profiles = profiles;
        _getProfile = getProfile;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalProfileDto>> ExecuteAsync(
        PlatformUserId userIdentityId,
        AccountProfileId accountProfileId,
        UpdatePersonalProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userIdentityId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PersonalProfileDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "User identity was not found.");
        }

        var profile = await _profiles.GetByIdAsync(accountProfileId, cancellationToken).ConfigureAwait(false);
        if (profile is null || profile.UserIdentityId != userIdentityId || profile.AccountClass is not AccountClass.Personal)
        {
            return ApplicationResult<PersonalProfileDto>.Failure(
                ApplicationErrorCodes.AccountProfileNotAvailable,
                "Personal account profile is not available.");
        }

        try
        {
            // Keep email immutable on this self-service path; only DisplayName changes.
            user.UpdateProfile(request.DisplayName, user.NormalizedEmail, _clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalProfileDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await _users.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditWriter.WriteAsync(
            $"platform-user:{userIdentityId.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PlatformUserProfileUpdated,
            nameof(PlatformUser),
            userIdentityId.Value.ToString("D"),
            AuditOutcome.Succeeded,
            summary: "Personal profile display name updated.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return await _getProfile.ExecuteAsync(userIdentityId, accountProfileId, cancellationToken)
            .ConfigureAwait(false);
    }
}

public sealed class GetPersonalAccountSettings
{
    private readonly IPersonalAccountSettingsRepository _settings;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public GetPersonalAccountSettings(
        IPersonalAccountSettingsRepository settings,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _settings = settings;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalAccountSettingsDto>> ExecuteAsync(
        PlatformUserId userIdentityId,
        CancellationToken cancellationToken = default)
    {
        var settings = await _settings.GetByUserAsync(userIdentityId, cancellationToken).ConfigureAwait(false);
        if (settings is null)
        {
            settings = PersonalAccountSettings.CreateDefaults(userIdentityId, _clock.UtcNow);
            await _settings.AddAsync(settings, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return ApplicationResult<PersonalAccountSettingsDto>.Success(ToDto(settings));
    }

    internal static PersonalAccountSettingsDto ToDto(PersonalAccountSettings settings) =>
        new(
            settings.UserIdentityId.Value,
            settings.EmailNotificationsEnabled,
            settings.PushNotificationsEnabled,
            settings.InAppNotificationsEnabled,
            settings.ReminderNotificationsEnabled,
            settings.Version,
            settings.UpdatedAtUtc);
}

public sealed class UpdatePersonalAccountSettings
{
    private readonly IPersonalAccountSettingsRepository _settings;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdatePersonalAccountSettings(
        IPersonalAccountSettingsRepository settings,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _settings = settings;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalAccountSettingsDto>> ExecuteAsync(
        PlatformUserId userIdentityId,
        UpdatePersonalAccountSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var settings = await _settings.GetByUserAsync(userIdentityId, cancellationToken).ConfigureAwait(false);
        if (settings is null)
        {
            settings = PersonalAccountSettings.CreateDefaults(userIdentityId, _clock.UtcNow);
            await _settings.AddAsync(settings, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            settings.UpdateNotificationPreferences(
                request.EmailNotificationsEnabled,
                request.PushNotificationsEnabled,
                request.InAppNotificationsEnabled,
                request.ReminderNotificationsEnabled,
                _clock.UtcNow,
                request.ExpectedVersion);
        }
        catch (DomainException ex) when (ex.ErrorCode == DomainErrorCodes.PersonalAccountSettingsConcurrencyConflict)
        {
            return ApplicationResult<PersonalAccountSettingsDto>.Failure(
                ApplicationErrorCodes.ConcurrencyConflict,
                ex.Message);
        }

        await _settings.UpdateAsync(settings, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditWriter.WriteAsync(
            $"platform-user:{userIdentityId.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PersonalAccountSettingsUpdated,
            nameof(PersonalAccountSettings),
            userIdentityId.Value.ToString("D"),
            AuditOutcome.Succeeded,
            summary: "Personal notification preferences updated.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ApplicationResult<PersonalAccountSettingsDto>.Success(GetPersonalAccountSettings.ToDto(settings));
    }
}
