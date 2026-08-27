namespace ExItS.Platform.Domain.Personal;

public enum PersonalContactStatus
{
    Active,
    Archived
}

public enum PersonalConnectionRequestStatus
{
    Pending,
    Accepted,
    Declined,
    Revoked,
    Expired
}

public enum PersonalDebtRelationshipStatus
{
    Active,
    Closed,
    Archived,
    Transferred
}

public enum PersonalUtangSourceDisposition
{
    Retain,
    Archive,
    MarkTransferred
}

public enum PersonalUtangMigrationBatchStatus
{
    Previewed,
    Executed,
    Failed
}

public enum PersonalUtangMigrationSourceType
{
    PersonalContact,
    PersonalDebtRelationship,
    PersonalUtangEntry
}

public enum PersonalUtangMigrationDestinationType
{
    BusinessCustomer,
    CreditCustomer,
    BusinessCreditOpeningBalance
}

public enum PersonalUtangMigrationItemStatus
{
    Previewed,
    Migrated,
    Skipped,
    Blocked
}

public enum PersonalUtangEntryType
{
    Loan,
    Payment,
    Adjustment
}

/// <summary>
/// Distinguishes ordinary ledger payments from explicit full-balance settlement intent.
/// Settlement uses the existing Payment entry type — never a second ledger.
/// </summary>
public enum PersonalUtangEntryIntent
{
    Regular = 0,
    Settlement = 1
}

/// <summary>
/// Lifecycle of a personal utang ledger entry.
/// Only <see cref="Confirmed"/> entries affect relationship balance and dashboard totals.
/// </summary>
public enum PersonalUtangEntryStatus
{
    Pending,
    Confirmed,
    Disputed,
    Cancelled
}

public enum PersonalUtangInvitationStatus
{
    Pending,
    Accepted,
    Declined,
    Revoked,
    Expired
}

public enum PersonalReminderScheduleType
{
    OneTime,
    OnDueDate,
    BeforeDueDate,
    RecurringOverdue
}

public enum PersonalReminderStatus
{
    Scheduled,
    Delivered,
    Cancelled,
    Failed
}

public enum PersonalNotificationChannel
{
    InApp,
    Push,
    Email
}

public enum PersonalNotificationDeliveryStatus
{
    Queued,
    Delivered,
    Skipped,
    Failed
}
