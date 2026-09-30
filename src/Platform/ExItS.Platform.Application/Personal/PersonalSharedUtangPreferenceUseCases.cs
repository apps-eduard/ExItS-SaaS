using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

public sealed record PersonalSharedUtangPreferenceDto(
    Guid Id,
    Guid OwnerUserIdentityId,
    Guid CounterpartyUserIdentityId,
    bool ReceiveSharedUtang,
    /// <summary>Retained for API compatibility; always false under ledger-owner model.</summary>
    bool AutoAcceptSharedUtang,
    bool SharedUtangNotifications,
    int Version,
    DateTimeOffset UpdatedAtUtc);

/// <summary>
/// Update request. <see cref="AutoAcceptSharedUtang"/> is accepted for compatibility but ignored
/// (forced false) — financial writes are ledger-owner only.
/// </summary>
public sealed record UpdatePersonalSharedUtangPreferenceRequest(
    bool ReceiveSharedUtang,
    bool AutoAcceptSharedUtang,
    bool SharedUtangNotifications,
    int? ExpectedVersion = null);

public sealed class GetPersonalSharedUtangPreference
{
    private readonly IPersonalSharedUtangPreferenceRepository _preferences;
    private readonly IPersonalContactRepository _contacts;
    private readonly IClock _clock;

    public GetPersonalSharedUtangPreference(
        IPersonalSharedUtangPreferenceRepository preferences,
        IPersonalContactRepository contacts,
        IClock clock)
    {
        _preferences = preferences;
        _contacts = contacts;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalSharedUtangPreferenceDto>> ExecuteAsync(
        PlatformUserId ownerUserIdentityId,
        Guid counterpartyUserIdentityId,
        CancellationToken cancellationToken = default)
    {
        var counterparty = PlatformUserId.From(counterpartyUserIdentityId);
        if (counterparty == ownerUserIdentityId)
        {
            return ApplicationResult<PersonalSharedUtangPreferenceDto>.Failure(
                ApplicationErrorCodes.PersonalSharedUtangPreferenceUnauthorized,
                "Cannot load shared Utang preferences for yourself.");
        }

        // Preference UI is for a connected counterparty (either direction).
        var connected = await _contacts
            .FindActiveByOwnerAndLinkedUserAsync(ownerUserIdentityId, counterparty, cancellationToken)
            .ConfigureAwait(false);
        if (connected is null || connected.IsBlocked)
        {
            var reverse = await _contacts
                .FindActiveByOwnerAndLinkedUserAsync(counterparty, ownerUserIdentityId, cancellationToken)
                .ConfigureAwait(false);
            if (reverse is null)
            {
                return ApplicationResult<PersonalSharedUtangPreferenceDto>.Failure(
                    ApplicationErrorCodes.PersonalSharedUtangPreferenceNotFound,
                    "Shared Utang preferences require a connected person.");
            }
        }

        var prefs = await PersonalSharedUtangSharingSupport.GetEffectiveAsync(
            ownerUserIdentityId,
            counterparty,
            _preferences,
            _clock,
            cancellationToken).ConfigureAwait(false);

        return ApplicationResult<PersonalSharedUtangPreferenceDto>.Success(ToDto(prefs));
    }

    internal static PersonalSharedUtangPreferenceDto ToDto(PersonalSharedUtangPreference prefs) =>
        new(
            prefs.Id.Value,
            prefs.OwnerUserIdentityId.Value,
            prefs.CounterpartyUserIdentityId.Value,
            prefs.ReceiveSharedUtang,
            prefs.AutoAcceptSharedUtang,
            prefs.SharedUtangNotifications,
            prefs.Version,
            prefs.UpdatedAtUtc);
}

public sealed class UpdatePersonalSharedUtangPreference
{
    private readonly IPersonalSharedUtangPreferenceRepository _preferences;
    private readonly IPersonalContactRepository _contacts;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UpdatePersonalSharedUtangPreference(
        IPersonalSharedUtangPreferenceRepository preferences,
        IPersonalContactRepository contacts,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _preferences = preferences;
        _contacts = contacts;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalSharedUtangPreferenceDto>> ExecuteAsync(
        PlatformUserId ownerUserIdentityId,
        Guid counterpartyUserIdentityId,
        UpdatePersonalSharedUtangPreferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var counterparty = PlatformUserId.From(counterpartyUserIdentityId);
        if (counterparty == ownerUserIdentityId)
        {
            return ApplicationResult<PersonalSharedUtangPreferenceDto>.Failure(
                ApplicationErrorCodes.PersonalSharedUtangPreferenceUnauthorized,
                "Cannot update shared Utang preferences for yourself.");
        }

        var connected = await _contacts
            .FindActiveByOwnerAndLinkedUserAsync(ownerUserIdentityId, counterparty, cancellationToken)
            .ConfigureAwait(false);
        if (connected is null)
        {
            return ApplicationResult<PersonalSharedUtangPreferenceDto>.Failure(
                ApplicationErrorCodes.PersonalSharedUtangPreferenceNotFound,
                "Shared Utang preferences require a connected person.");
        }

        if (connected.IsBlocked)
        {
            return ApplicationResult<PersonalSharedUtangPreferenceDto>.Failure(
                ApplicationErrorCodes.PersonalConnectionBlocked,
                "This relationship is blocked.");
        }

        try
        {
            var existing = await _preferences
                .GetByOwnerAndCounterpartyAsync(ownerUserIdentityId, counterparty, cancellationToken)
                .ConfigureAwait(false);

            // AutoAccept is obsolete under single-writer ledger-owner; keep DTO field but never store true.
            const bool autoAcceptForcedOff = false;

            PersonalSharedUtangPreference prefs;
            if (existing is null)
            {
                prefs = PersonalSharedUtangPreference.CreateDefaults(
                    ownerUserIdentityId,
                    counterparty,
                    _clock.UtcNow);
                prefs.Update(
                    request.ReceiveSharedUtang,
                    autoAcceptForcedOff,
                    request.SharedUtangNotifications,
                    _clock.UtcNow,
                    expectedVersion: null);
                await _preferences.AddAsync(prefs, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                existing.Update(
                    request.ReceiveSharedUtang,
                    autoAcceptForcedOff,
                    request.SharedUtangNotifications,
                    _clock.UtcNow,
                    request.ExpectedVersion);
                await _preferences.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
                prefs = existing;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{ownerUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                "PersonalSharedUtangPreferenceUpdated",
                nameof(PersonalSharedUtangPreference),
                prefs.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary:
                    $"Shared Utang preferences updated (receive={prefs.ReceiveSharedUtang}, notifications={prefs.SharedUtangNotifications}).",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalSharedUtangPreferenceDto>.Success(
                GetPersonalSharedUtangPreference.ToDto(prefs));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalSharedUtangPreferenceDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
