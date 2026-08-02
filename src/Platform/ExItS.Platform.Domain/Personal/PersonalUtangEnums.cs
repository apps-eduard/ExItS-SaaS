namespace ExItS.Platform.Domain.Personal;

public enum PersonalContactStatus
{
    Active,
    Archived
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
