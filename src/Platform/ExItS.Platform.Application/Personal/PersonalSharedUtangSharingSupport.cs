using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

/// <summary>
/// Recipient preference resolution and shared-visibility notifications.
/// Financial authority is ledger-owner only; AutoAccept is obsolete for new entries.
/// </summary>
internal static class PersonalSharedUtangSharingSupport
{
    public const string ShareOutcomePrivate = "Private";
    /// <summary>Shared visibility; owner writes Confirmed immediately.</summary>
    public const string ShareOutcomeShared = "Shared";
    /// <summary>Legacy outcome string retained for older clients.</summary>
    public const string ShareOutcomeSharedPending = "SharedPending";
    /// <summary>Legacy outcome string; auto-accept no longer applied to new entries.</summary>
    public const string ShareOutcomeSharedAutoSynced = "SharedAutoSynced";
    public const string ShareOutcomePrivateNotReceiving = "PrivateNotReceiving";

    public const string SharedEntryRelatedType = "PersonalUtangSharedEntry";
    public const string AutoSyncedRelatedType = "PersonalUtangAutoSynced";

    public static async Task<PersonalSharedUtangPreference> GetEffectiveAsync(
        PlatformUserId recipientUserIdentityId,
        PlatformUserId senderUserIdentityId,
        IPersonalSharedUtangPreferenceRepository preferences,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var existing = await preferences
            .GetByOwnerAndCounterpartyAsync(recipientUserIdentityId, senderUserIdentityId, cancellationToken)
            .ConfigureAwait(false);
        return existing
            ?? PersonalSharedUtangPreference.CreateDefaults(
                recipientUserIdentityId,
                senderUserIdentityId,
                clock.UtcNow);
    }

    /// <summary>
    /// After the ledger owner records a shared Confirmed entry: notify counterparty when enabled.
    /// Does not Pending or AutoAccept — owner-model entries are already Confirmed.
    /// </summary>
    public static async Task<string> ApplySharedEntryOutcomeAsync(
        PersonalDebtRelationship relationship,
        PersonalUtangEntry entry,
        PlatformUserId proposerUserIdentityId,
        IPersonalSharedUtangPreferenceRepository preferences,
        IPersonalDebtRelationshipRepository relationships,
        IPersonalUtangEntryRepository entries,
        IPlatformUserRepository users,
        IPersonalAccountSettingsRepository accountSettings,
        IPersonalInAppNotificationRepository notifications,
        IClock clock,
        CancellationToken cancellationToken)
    {
        _ = relationships;
        _ = entries;

        if (!relationship.IsSharedLinked)
        {
            return ShareOutcomePrivate;
        }

        // Legacy Pending entries: keep aggregate pending notification only (no AutoAccept).
        if (entry.Status is PersonalUtangEntryStatus.Pending)
        {
            var legacyRecipient = relationship.GetCounterpartyUserIdentityId(proposerUserIdentityId);
            if (legacyRecipient is null)
            {
                return ShareOutcomeSharedPending;
            }

            var legacyPrefs = await GetEffectiveAsync(
                legacyRecipient,
                proposerUserIdentityId,
                preferences,
                clock,
                cancellationToken).ConfigureAwait(false);
            if (legacyPrefs.SharedUtangNotifications)
            {
                await PersonalUtangProposalAntiSpam.NotifyOrAggregatePendingAsync(
                    relationship,
                    proposerUserIdentityId,
                    users,
                    accountSettings,
                    notifications,
                    entries,
                    clock,
                    cancellationToken).ConfigureAwait(false);
            }

            return ShareOutcomeSharedPending;
        }

        var recipient = relationship.GetCounterpartyUserIdentityId(proposerUserIdentityId);
        if (recipient is null)
        {
            return ShareOutcomeShared;
        }

        var prefs = await GetEffectiveAsync(
            recipient,
            proposerUserIdentityId,
            preferences,
            clock,
            cancellationToken).ConfigureAwait(false);

        if (prefs.SharedUtangNotifications)
        {
            await NotifySharedConfirmedAsync(
                recipient,
                proposerUserIdentityId,
                entry,
                users,
                accountSettings,
                notifications,
                clock,
                cancellationToken).ConfigureAwait(false);
        }

        return ShareOutcomeShared;
    }

    public static async Task NotifySharedConfirmedAsync(
        PlatformUserId recipientUserIdentityId,
        PlatformUserId proposerUserIdentityId,
        PersonalUtangEntry entry,
        IPlatformUserRepository users,
        IPersonalAccountSettingsRepository accountSettings,
        IPersonalInAppNotificationRepository notifications,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var accountPrefs = await accountSettings
            .GetByUserAsync(recipientUserIdentityId, cancellationToken)
            .ConfigureAwait(false)
            ?? PersonalAccountSettings.CreateDefaults(recipientUserIdentityId, clock.UtcNow);
        if (!accountPrefs.InAppNotificationsEnabled)
        {
            return;
        }

        var proposer = await users.GetByIdAsync(proposerUserIdentityId, cancellationToken).ConfigureAwait(false);
        var proposerName = string.IsNullOrWhiteSpace(proposer?.DisplayName)
            ? "Someone"
            : proposer!.DisplayName.Trim();

        var created = PersonalInAppNotification.Create(
            recipientUserIdentityId,
            "Shared Utang updated",
            $"{proposerName} updated a shared Utang with you.",
            SharedEntryRelatedType,
            clock.UtcNow,
            relatedId: entry.Id.Value.ToString("N"));
        await notifications.AddAsync(created, cancellationToken).ConfigureAwait(false);
    }
}
