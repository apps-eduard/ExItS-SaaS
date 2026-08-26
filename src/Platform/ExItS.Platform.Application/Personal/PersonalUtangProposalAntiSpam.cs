using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

/// <summary>
/// Anti-spam gates for shared Personal Utang principal (Loan) proposals.
/// Limits are directional: sender → counterparty.
/// </summary>
internal static class PersonalUtangProposalAntiSpam
{
    public const int MaxUnresolvedPendingPerDirection = 3;
    public const int MaxNewProposalsPerDirectionPerRollingDay = 10;
    public static readonly TimeSpan RollingDayWindow = TimeSpan.FromHours(24);
    public static readonly TimeSpan DuplicateWindow = TimeSpan.FromMinutes(2);

    public const string PendingProposalsRelatedType = "PersonalUtangPendingProposals";

    public static string ActiveNotificationRelatedId(PlatformUserId senderUserIdentityId) =>
        $"from:{senderUserIdentityId.Value:N}";

    /// <summary>
    /// Closed cycles keep history but free the active related-id slot.
    /// Kept under PersonalInAppNotification.RelatedId varchar(64).
    /// </summary>
    public static string ClosedNotificationRelatedId(PlatformUserId senderUserIdentityId, Guid notificationId) =>
        $"done:{senderUserIdentityId.Value:N}:{notificationId.ToString("N")[..8]}";
    public sealed record GateFailure(string ErrorCode, string ErrorMessage);

    public static string? NormalizePurposeForMatch(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var trimmed = notes.Trim();
        return trimmed.Length > PersonalUtangEntry.NotesMaxLength
            ? trimmed[..PersonalUtangEntry.NotesMaxLength]
            : trimmed;
    }

    public static async Task<GateFailure?> EnsureSharedLoanProposalAllowedAsync(
        PlatformUserId senderUserIdentityId,
        PlatformUserId counterpartyUserIdentityId,
        decimal amount,
        string? notes,
        IPersonalUtangEntryRepository entries,
        IPersonalContactRepository contacts,
        IClock clock,
        CancellationToken cancellationToken)
    {
        if (await PersonalConnectionSupport.IsBlockedEitherWayAsync(
                senderUserIdentityId,
                counterpartyUserIdentityId,
                contacts,
                cancellationToken).ConfigureAwait(false))
        {
            return new GateFailure(
                ApplicationErrorCodes.PersonalConnectionBlocked,
                "This relationship is blocked.");
        }

        var normalizedNotes = NormalizePurposeForMatch(notes);

        var duplicate = await entries
            .FindRecentDuplicateLoanAsync(
                senderUserIdentityId,
                counterpartyUserIdentityId,
                amount,
                normalizedNotes,
                clock.UtcNow - DuplicateWindow,
                cancellationToken)
            .ConfigureAwait(false);
        if (duplicate is not null)
        {
            return new GateFailure(
                ApplicationErrorCodes.PersonalUtangDuplicateSubmission,
                "This Utang entry was already submitted.");
        }

        var pending = await entries
            .CountPendingProposalsBySenderTowardAsync(
                senderUserIdentityId,
                counterpartyUserIdentityId,
                cancellationToken)
            .ConfigureAwait(false);
        if (pending >= MaxUnresolvedPendingPerDirection)
        {
            return new GateFailure(
                ApplicationErrorCodes.PersonalUtangPendingLimitReached,
                "You already have the maximum unresolved Utang entries waiting for review with this person.");
        }

        var since = clock.UtcNow - RollingDayWindow;
        var createdToday = await entries
            .CountLoanProposalsCreatedBySenderTowardSinceAsync(
                senderUserIdentityId,
                counterpartyUserIdentityId,
                since,
                cancellationToken)
            .ConfigureAwait(false);
        if (createdToday >= MaxNewProposalsPerDirectionPerRollingDay)
        {
            return new GateFailure(
                ApplicationErrorCodes.PersonalUtangDailyLimitReached,
                "You've reached today's limit for new Utang entries with this person.");
        }

        return null;
    }

    public static async Task NotifyOrAggregatePendingAsync(
        PersonalDebtRelationship relationship,
        PlatformUserId proposerUserIdentityId,
        IPlatformUserRepository users,
        IPersonalAccountSettingsRepository settings,
        IPersonalInAppNotificationRepository notifications,
        IPersonalUtangEntryRepository entries,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var counterparty = relationship.GetCounterpartyUserIdentityId(proposerUserIdentityId);
        if (counterparty is null)
        {
            return;
        }

        var prefs = await settings.GetByUserAsync(counterparty, cancellationToken).ConfigureAwait(false)
            ?? PersonalAccountSettings.CreateDefaults(counterparty, clock.UtcNow);
        if (!prefs.InAppNotificationsEnabled)
        {
            return;
        }

        var pendingCount = await entries
            .CountPendingProposalsBySenderTowardAsync(proposerUserIdentityId, counterparty, cancellationToken)
            .ConfigureAwait(false);
        if (pendingCount <= 0)
        {
            return;
        }

        var proposer = await users.GetByIdAsync(proposerUserIdentityId, cancellationToken).ConfigureAwait(false);
        var proposerName = string.IsNullOrWhiteSpace(proposer?.DisplayName)
            ? "Someone"
            : proposer!.DisplayName.Trim();

        var relatedId = ActiveNotificationRelatedId(proposerUserIdentityId);
        var title = "Utang entries to review";
        var preview = pendingCount == 1
            ? $"{proposerName} recorded an Utang entry for your review."
            : $"{proposerName} has {pendingCount} Utang entries waiting for your review.";

        var existing = await notifications
            .FindByRecipientRelatedAsync(counterparty, PendingProposalsRelatedType, relatedId, cancellationToken)
            .ConfigureAwait(false);

        if (existing is null)
        {
            var created = PersonalInAppNotification.Create(
                counterparty,
                title,
                preview,
                PendingProposalsRelatedType,
                clock.UtcNow,
                relatedId);
            await notifications.AddAsync(created, cancellationToken).ConfigureAwait(false);
            return;
        }

        existing.UpdateContent(title, preview);
        if (existing.IsRead)
        {
            existing.MarkUnread();
        }

        await notifications.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
    }

    public static async Task RefreshAggregatedNotificationAfterResolveAsync(
        PersonalDebtRelationship relationship,
        PlatformUserId proposerUserIdentityId,
        IPlatformUserRepository users,
        IPersonalAccountSettingsRepository settings,
        IPersonalInAppNotificationRepository notifications,
        IPersonalUtangEntryRepository entries,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var counterparty = relationship.GetCounterpartyUserIdentityId(proposerUserIdentityId);
        if (counterparty is null)
        {
            return;
        }

        var relatedId = ActiveNotificationRelatedId(proposerUserIdentityId);
        var existing = await notifications
            .FindByRecipientRelatedAsync(counterparty, PendingProposalsRelatedType, relatedId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            return;
        }

        var pendingCount = await entries
            .CountPendingProposalsBySenderTowardAsync(proposerUserIdentityId, counterparty, cancellationToken)
            .ConfigureAwait(false);

        if (pendingCount <= 0)
        {
            existing.RetargetRelatedId(ClosedNotificationRelatedId(proposerUserIdentityId, existing.Id.Value));
            if (!existing.IsRead)
            {
                existing.MarkRead(clock.UtcNow);
            }

            await notifications.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            return;
        }

        var proposer = await users.GetByIdAsync(proposerUserIdentityId, cancellationToken).ConfigureAwait(false);
        var proposerName = string.IsNullOrWhiteSpace(proposer?.DisplayName)
            ? "Someone"
            : proposer!.DisplayName.Trim();
        var preview = pendingCount == 1
            ? $"{proposerName} recorded an Utang entry for your review."
            : $"{proposerName} has {pendingCount} Utang entries waiting for your review.";
        existing.UpdateContent("Utang entries to review", preview);
        await notifications.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
    }
}
