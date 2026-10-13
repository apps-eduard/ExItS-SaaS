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
    string? Phone = null,
    string? FirstName = null,
    string? MiddleName = null,
    string? LastName = null,
    string? DateOfBirth = null,
    string? Gender = null,
    string? Nationality = null,
    string? ProfilePhotoUrl = null,
    string? AlternativeMobile = null,
    string? Country = null,
    string? AddressLine1 = null,
    string? AddressLine2 = null,
    string? Barangay = null,
    string? CityMunicipality = null,
    string? ProvinceState = null,
    string? PostalCode = null,
    bool IsPrimary = false,
    string ShowProfilePhoto = "Private",
    string ShowDisplayName = "Connections",
    string ShowCity = "Private",
    string ShowMobile = "Private",
    string ShowEmail = "Private",
    int CompletionPercent = 0,
    IReadOnlyList<string>? MissingForBase = null,
    IReadOnlyList<string>? MissingForStaff = null,
    IReadOnlyList<string>? MissingForCustomer = null,
    IReadOnlyList<PersonalAddressDto>? Addresses = null);

public sealed record PersonalAddressDto(
    Guid Id,
    string AddressType,
    string? Country,
    string? AddressLine1,
    string? AddressLine2,
    string? Barangay,
    string? CityMunicipality,
    string? ProvinceState,
    string? PostalCode,
    bool IsPrimary,
    string? CountryCode = null);

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

public sealed record UpdatePersonalProfileRequest(
    string DisplayName,
    string? FirstName = null,
    string? MiddleName = null,
    string? LastName = null,
    string? Phone = null,
    string? AlternativeMobile = null,
    DateOnly? DateOfBirth = null,
    string? Gender = null,
    string? Nationality = null,
    string? ProfilePhotoUrl = null,
    string? Country = null,
    string? AddressLine1 = null,
    string? AddressLine2 = null,
    string? Barangay = null,
    string? CityMunicipality = null,
    string? Province = null,
    string? ProvinceState = null,
    string? PostalCode = null,
    string? ShowProfilePhoto = null,
    string? ShowDisplayName = null,
    string? ShowCity = null,
    string? ShowMobile = null,
    string? ShowEmail = null,
    bool ClearDateOfBirth = false);

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
    private readonly IPersonalUserProfileRepository _personalProfiles;
    private readonly IPersonalAddressRepository _addresses;
    private readonly GetOrAssignPublicIdentity _publicIdentity;

    public GetPersonalProfile(
        IPlatformUserRepository users,
        IAccountProfileRepository profiles,
        IPersonalUserProfileRepository personalProfiles,
        IPersonalAddressRepository addresses,
        GetOrAssignPublicIdentity publicIdentity)
    {
        _users = users;
        _profiles = profiles;
        _personalProfiles = personalProfiles;
        _addresses = addresses;
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

        var personal = await _personalProfiles.GetByUserAsync(userIdentityId, cancellationToken).ConfigureAwait(false);
        var addresses = await _addresses.ListByUserAsync(userIdentityId, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<PersonalProfileDto>.Success(
            PersonalProfileMapper.ToDto(user, profile, personal, addresses, publicUserId, qrPayload));
    }
}

public sealed class UpdatePersonalProfile
{
    private readonly IPlatformUserRepository _users;
    private readonly IAccountProfileRepository _profiles;
    private readonly IPersonalUserProfileRepository _personalProfiles;
    private readonly IPersonalAddressRepository _addresses;
    private readonly GetPersonalProfile _getProfile;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdatePersonalProfile(
        IPlatformUserRepository users,
        IAccountProfileRepository profiles,
        IPersonalUserProfileRepository personalProfiles,
        IPersonalAddressRepository addresses,
        GetPersonalProfile getProfile,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _profiles = profiles;
        _personalProfiles = personalProfiles;
        _addresses = addresses;
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
            // Email stays the login key. Personal details live on the person, not a staff or customer record.
            user.UpdatePersonalIdentity(
                request.FirstName ?? user.FirstName,
                request.LastName ?? user.LastName,
                request.DisplayName,
                request.Phone ?? user.Phone,
                _clock.UtcNow);
            var personal = await _personalProfiles
                .GetByUserAsync(userIdentityId, cancellationToken)
                .ConfigureAwait(false);
            var created = personal is null;
            personal ??= PersonalUserProfile.Create(userIdentityId, _clock.UtcNow);
            personal.Update(PersonalProfileMapper.ToDraft(request, personal), _clock.UtcNow);
            if (created)
            {
                await _personalProfiles.AddAsync(personal, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _personalProfiles.UpdateAsync(personal, cancellationToken).ConfigureAwait(false);
            }

            if (PersonalProfileMapper.IncludesAddress(request))
            {
                await UpsertPrimaryAddressAsync(userIdentityId, request, cancellationToken).ConfigureAwait(false);
            }
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

    private async Task UpsertPrimaryAddressAsync(
        PlatformUserId userIdentityId,
        UpdatePersonalProfileRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await _addresses.ListByUserAsync(userIdentityId, cancellationToken).ConfigureAwait(false);
        var primary = PersonalProfileMapper.Primary(existing);
        var draft = new PersonalAddressDraft(
            primary?.AddressType ?? PersonalAddressType.Home,
            request.Country ?? primary?.Country,
            request.AddressLine1 ?? primary?.AddressLine1,
            request.AddressLine2 ?? primary?.AddressLine2,
            request.Barangay ?? primary?.Barangay,
            request.CityMunicipality ?? primary?.CityMunicipality,
            request.ProvinceState ?? request.Province ?? primary?.ProvinceState,
            request.PostalCode ?? primary?.PostalCode);
        if (primary is null)
        {
            var created = PersonalAddress.Create(userIdentityId, draft, isPrimary: true, _clock.UtcNow);
            await _addresses.AddAsync(created, cancellationToken).ConfigureAwait(false);
            return;
        }

        primary.Update(draft, _clock.UtcNow);
        await _addresses.UpdateAsync(primary, cancellationToken).ConfigureAwait(false);
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
