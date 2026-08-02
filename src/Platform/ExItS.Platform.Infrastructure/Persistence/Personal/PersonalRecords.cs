namespace ExItS.Platform.Infrastructure.Persistence.Personal;

internal sealed class PersonalAccountSettingsRecord
{
    public Guid UserIdentityId { get; set; }
    public bool EmailNotificationsEnabled { get; set; }
    public bool PushNotificationsEnabled { get; set; }
    public bool InAppNotificationsEnabled { get; set; }
    public bool ReminderNotificationsEnabled { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int Version { get; set; }
    public uint Xmin { get; set; }
}

internal sealed class PersonalContactRecord
{
    public Guid Id { get; set; }
    public Guid OwnerUserIdentityId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public Guid? LinkedUserIdentityId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}

internal sealed class PersonalDebtRelationshipRecord
{
    public Guid Id { get; set; }
    public Guid? CreditorUserIdentityId { get; set; }
    public Guid? CreditorContactId { get; set; }
    public Guid? DebtorUserIdentityId { get; set; }
    public Guid? DebtorContactId { get; set; }
    public string CurrencyCode { get; set; } = "PHP";
    public decimal CurrentBalance { get; set; }
    public DateTimeOffset? DueDateUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int AggregateVersion { get; set; }
    public uint Xmin { get; set; }
}

internal sealed class PersonalUtangEntryRecord
{
    public Guid Id { get; set; }
    public Guid RelationshipId { get; set; }
    public string EntryType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal SignedDelta { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset? DueDateUtc { get; set; }
    public Guid CreatedByUserIdentityId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

internal sealed class PersonalUtangInvitationRecord
{
    public Guid Id { get; set; }
    public Guid DebtRelationshipId { get; set; }
    public Guid InviteeContactId { get; set; }
    public Guid InvitedByUserIdentityId { get; set; }
    public string? InviteTargetNormalizedEmail { get; set; }
    public string? InviteTargetPhone { get; set; }
    public string Status { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? AcceptedAtUtc { get; set; }
    public DateTimeOffset? DeclinedAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public Guid? AcceptedByUserIdentityId { get; set; }
}

internal sealed class PersonalReminderRecord
{
    public Guid Id { get; set; }
    public Guid DebtRelationshipId { get; set; }
    public Guid CreatedByUserIdentityId { get; set; }
    public string ScheduleType { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTimeOffset ScheduledForUtc { get; set; }
    public DateTimeOffset? NextDeliveryAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? DeliveredAtUtc { get; set; }
    public int DeliveryAttemptCount { get; set; }
}

internal sealed class PersonalInAppNotificationRecord
{
    public Guid Id { get; set; }
    public Guid RecipientUserIdentityId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Preview { get; set; } = string.Empty;
    public string RelatedType { get; set; } = string.Empty;
    public string? RelatedId { get; set; }
    public bool IsRead { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ReadAtUtc { get; set; }
}

internal sealed class PersonalNotificationDeliveryRecord
{
    public Guid Id { get; set; }
    public Guid? ReminderId { get; set; }
    public Guid? NotificationId { get; set; }
    public Guid RecipientUserIdentityId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PreviewText { get; set; } = string.Empty;
    public DateTimeOffset AttemptedAtUtc { get; set; }
    public DateTimeOffset? DeliveredAtUtc { get; set; }
    public string? FailureReason { get; set; }
}
