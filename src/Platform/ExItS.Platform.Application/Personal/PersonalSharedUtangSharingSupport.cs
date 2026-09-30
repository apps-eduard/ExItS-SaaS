using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

/// <summary>
/// Recipient preference resolution and post-record shared-entry outcomes
/// (pending review vs standing auto-accept).
/// </summary>
internal static class PersonalSharedUtangSharingSupport
{
    public const string ShareOutcomePrivate = "Private";
    public const string ShareOutcomeSharedPending = "SharedPending";
    public const string ShareOutcomeSharedAutoSynced = "SharedAutoSynced";
    public const string ShareOutcomePrivateNotReceiving = "PrivateNotReceiving";

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
    /// After a shared Regular entry is recorded as Pending: auto-confirm when allowed,
    /// otherwise aggregate pending review notification when enabled.
    /// Settlement is never auto-accepted.
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
        if (!relationship.IsSharedLinked || entry.Status is not PersonalUtangEntryStatus.Pending)
        {
            return ShareOutcomePrivate;
        }

        var recipient = relationship.GetCounterpartyUserIdentityId(proposerUserIdentityId);
        if (recipient is null)
        {
            return ShareOutcomeSharedPending;
        }

        var prefs = await GetEffectiveAsync(
            recipient,
            proposerUserIdentityId,
            preferences,
            clock,
            cancellationToken).ConfigureAwait(false);

        if (!entry.IsSettlement && prefs.AutoAcceptSharedUtang && prefs.ReceiveSharedUtang)
        {
            relationship.ConfirmEntry(
                entry,
                recipient,
                clock.UtcNow,
                expectedVersion: null,
                PersonalUtangConfirmationSource.RecipientAutoAccept);
            await relationships.UpdateAsync(relationship, cancellationToken).ConfigureAwait(false);
            await entries.UpdateAsync(entry, cancellationToken).ConfigureAwait(false);

            if (prefs.SharedUtangNotifications)
            {
                await NotifyAutoSyncedAsync(
                    recipient,
                    proposerUserIdentityId,
                    entry,
                    users,
                    accountSettings,
                    notifications,
                    clock,
                    cancellationToken).ConfigureAwait(false);
            }

            return ShareOutcomeSharedAutoSynced;
        }

        if (prefs.SharedUtangNotifications)
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

    public static async Task NotifyAutoSyncedAsync(
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

        var title = "Shared Utang updated";
        var preview = $"{proposerName} recorded a shared Utang entry that was synced automatically.";
        var created = PersonalInAppNotification.Create(
            recipientUserIdentityId,
            title,
            preview,
            AutoSyncedRelatedType,
            clock.UtcNow,
            relatedId: entry.Id.Value.ToString("N"));
        await notifications.AddAsync(created, cancellationToken).ConfigureAwait(false);
    }
}
