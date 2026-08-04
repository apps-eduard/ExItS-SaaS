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
    decimal TotalLentBalance,
    decimal TotalBorrowedBalance);

public sealed record PersonalProfileDto(
    Guid UserIdentityId,
    Guid AccountProfileId,
    string Username,
    string DisplayName,
    string Email,
    string AccountClass,
    string Status,
    string? PublicUserId = null,
    string? QrPayload = null);

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

public sealed class GetPersonalDashboard
{
    private readonly IPlatformUserRepository _users;
    private readonly IAccountProfileRepository _profiles;
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalDebtRelationshipRepository _relationships;

    public GetPersonalDashboard(
        IPlatformUserRepository users,
        IAccountProfileRepository profiles,
        IPersonalContactRepository contacts,
        IPersonalDebtRelationshipRepository relationships)
    {
        _users = users;
        _profiles = profiles;
        _contacts = contacts;
        _relationships = relationships;
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
        foreach (var relationship in active)
        {
            if (relationship.CreditorUserIdentityId == userIdentityId)
            {
                lent += relationship.CurrentBalance;
            }
            else if (relationship.DebtorUserIdentityId == userIdentityId)
            {
                borrowed += relationship.CurrentBalance;
            }
            else if (relationship.CreditorContactId is not null)
            {
                lent += relationship.CurrentBalance;
            }
            else if (relationship.DebtorContactId is not null)
            {
                borrowed += relationship.CurrentBalance;
            }
        }

        return ApplicationResult<PersonalDashboardDto>.Success(new PersonalDashboardDto(
            userIdentityId.Value,
            accountProfileId.Value,
            AccountClass.Personal.ToString(),
            UtangAvailable: true,
            contacts.Count,
            active.Count,
            lent,
            borrowed));
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
            qrPayload));
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
