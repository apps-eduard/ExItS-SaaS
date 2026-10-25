using ExItS.Platform.Application.Realtime;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

/// <summary>
/// Post-commit Personal Utang SignalR helpers. Recipients are relationship-authorized only.
/// Private/local ledgers notify the ledger owner alone. Shared linked ledgers notify both linked users.
/// Envelopes carry ids/kind only — never amounts, notes, or notification preview text.
/// </summary>
public static class PersonalUtangRealtime
{
    public static async Task NotifyEntryChangedAsync(
        IRealtimeChangePublisher publisher,
        PersonalDebtRelationship relationship,
        Guid entryId,
        string kind,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["kind"] = kind,
            ["relationshipId"] = relationship.Id.Value.ToString("D"),
            ["shared"] = relationship.IsSharedLinked ? "1" : "0"
        };

        foreach (var userId in ResolveEntryRecipients(relationship))
        {
            try
            {
                await RealtimePublish
                    .PersonalUserEventAsync(
                        publisher,
                        userId,
                        RealtimeEventTypes.PersonalUtangEntryChanged,
                        RealtimeResourceTypes.PersonalUtang,
                        entryId.ToString("D"),
                        data,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                /* notification-only */
            }
        }
    }

    public static async Task NotifyInvitationChangedAsync(
        IRealtimeChangePublisher publisher,
        Guid invitationId,
        Guid relationshipId,
        string kind,
        IEnumerable<Guid> recipientUserIds,
        CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["kind"] = kind,
            ["relationshipId"] = relationshipId.ToString("D")
        };

        foreach (var userId in Distinct(recipientUserIds))
        {
            try
            {
                await RealtimePublish
                    .PersonalUserEventAsync(
                        publisher,
                        userId,
                        RealtimeEventTypes.PersonalUtangInvitationChanged,
                        RealtimeResourceTypes.PersonalUtang,
                        invitationId.ToString("D"),
                        data,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch
            {
                /* notification-only */
            }
        }
    }

    public static async Task NotifyNotificationChangedAsync(
        IRealtimeChangePublisher publisher,
        Guid recipientUserId,
        string kind,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await RealtimePublish
                .PersonalUserEventAsync(
                    publisher,
                    recipientUserId,
                    RealtimeEventTypes.PersonalNotificationChanged,
                    RealtimeResourceTypes.PersonalNotification,
                    recipientUserId.ToString("D"),
                    new Dictionary<string, string>(StringComparer.Ordinal) { ["kind"] = kind },
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            /* notification-only */
        }
    }

    /// <summary>
    /// Private entries: ledger owner only. Shared linked: both linked Personal users.
    /// Never broadcast to unrelated authenticated users.
    /// </summary>
    public static IReadOnlyList<Guid> ResolveEntryRecipients(PersonalDebtRelationship relationship)
    {
        if (relationship.IsSharedLinked)
        {
            var set = new HashSet<Guid>();
            if (relationship.CreditorUserIdentityId is PlatformUserId c)
            {
                set.Add(c.Value);
            }

            if (relationship.DebtorUserIdentityId is PlatformUserId d)
            {
                set.Add(d.Value);
            }

            set.Add(relationship.LedgerOwnerUserIdentityId.Value);
            return set.ToList();
        }

        return [relationship.LedgerOwnerUserIdentityId.Value];
    }

    private static IEnumerable<Guid> Distinct(IEnumerable<Guid> ids) =>
        ids.Where(id => id != Guid.Empty).Distinct();
}
