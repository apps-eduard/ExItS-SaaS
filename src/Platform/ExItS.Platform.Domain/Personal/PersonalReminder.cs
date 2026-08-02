using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Personal;

/// <summary>
/// Personal Utang reminder schedule. Reminders are communication events, not financial transactions.
/// </summary>
public sealed class PersonalReminder
{
    public const int MaxDeliveriesPerRelationshipPerDay = 3;
    public static readonly TimeSpan MinIntervalBetweenDeliveries = TimeSpan.FromHours(1);

    public PersonalReminderId Id { get; }
    public PersonalDebtRelationshipId DebtRelationshipId { get; }
    public PlatformUserId CreatedByUserIdentityId { get; }
    public PersonalReminderScheduleType ScheduleType { get; }
    public string? Message { get; private set; }
    public DateTimeOffset ScheduledForUtc { get; private set; }
    public DateTimeOffset? NextDeliveryAtUtc { get; private set; }
    public PersonalReminderStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? DeliveredAtUtc { get; private set; }
    public int DeliveryAttemptCount { get; private set; }

    private PersonalReminder(
        PersonalReminderId id,
        PersonalDebtRelationshipId debtRelationshipId,
        PlatformUserId createdByUserIdentityId,
        PersonalReminderScheduleType scheduleType,
        string? message,
        DateTimeOffset scheduledForUtc,
        DateTimeOffset? nextDeliveryAtUtc,
        PersonalReminderStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? deliveredAtUtc,
        int deliveryAttemptCount)
    {
        Id = id;
        DebtRelationshipId = debtRelationshipId;
        CreatedByUserIdentityId = createdByUserIdentityId;
        ScheduleType = scheduleType;
        Message = message;
        ScheduledForUtc = scheduledForUtc;
        NextDeliveryAtUtc = nextDeliveryAtUtc;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        DeliveredAtUtc = deliveredAtUtc;
        DeliveryAttemptCount = deliveryAttemptCount;
    }

    public static PersonalReminder Create(
        PersonalDebtRelationshipId debtRelationshipId,
        PlatformUserId createdByUserIdentityId,
        PersonalReminderScheduleType scheduleType,
        DateTimeOffset scheduledForUtc,
        DateTimeOffset utcNow,
        string? message = null,
        PersonalReminderId? id = null)
    {
        ArgumentNullException.ThrowIfNull(debtRelationshipId);
        ArgumentNullException.ThrowIfNull(createdByUserIdentityId);
        EnsureUtc(utcNow);
        EnsureUtc(scheduledForUtc);

        if (!Enum.IsDefined(scheduleType))
        {
            throw new DomainException(DomainErrorCodes.InvalidPersonalReminder, "Reminder schedule type is invalid.");
        }

        message = NormalizeMessage(message);
        return new PersonalReminder(
            id ?? PersonalReminderId.New(),
            debtRelationshipId,
            createdByUserIdentityId,
            scheduleType,
            message,
            scheduledForUtc,
            nextDeliveryAtUtc: scheduledForUtc,
            PersonalReminderStatus.Scheduled,
            utcNow,
            utcNow,
            deliveredAtUtc: null,
            deliveryAttemptCount: 0);
    }

    public static PersonalReminder Rehydrate(
        PersonalReminderId id,
        PersonalDebtRelationshipId debtRelationshipId,
        PlatformUserId createdByUserIdentityId,
        PersonalReminderScheduleType scheduleType,
        string? message,
        DateTimeOffset scheduledForUtc,
        DateTimeOffset? nextDeliveryAtUtc,
        PersonalReminderStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? deliveredAtUtc,
        int deliveryAttemptCount) =>
        new(
            id,
            debtRelationshipId,
            createdByUserIdentityId,
            scheduleType,
            message,
            scheduledForUtc,
            nextDeliveryAtUtc,
            status,
            createdAtUtc,
            updatedAtUtc,
            deliveredAtUtc,
            deliveryAttemptCount);

    public void Cancel(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status is PersonalReminderStatus.Delivered or PersonalReminderStatus.Cancelled)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalReminder,
                $"Cannot cancel a reminder in status {Status}.");
        }

        Status = PersonalReminderStatus.Cancelled;
        NextDeliveryAtUtc = null;
        UpdatedAtUtc = utcNow;
    }

    public void MarkDelivered(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status is not PersonalReminderStatus.Scheduled)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalReminder,
                $"Cannot deliver a reminder in status {Status}.");
        }

        DeliveryAttemptCount++;
        DeliveredAtUtc = utcNow;
        UpdatedAtUtc = utcNow;

        if (ScheduleType is PersonalReminderScheduleType.RecurringOverdue)
        {
            NextDeliveryAtUtc = utcNow.AddDays(1);
            Status = PersonalReminderStatus.Scheduled;
        }
        else
        {
            Status = PersonalReminderStatus.Delivered;
            NextDeliveryAtUtc = null;
        }
    }

    public void MarkFailed(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        DeliveryAttemptCount++;
        Status = PersonalReminderStatus.Failed;
        UpdatedAtUtc = utcNow;
    }

    public static void EnsureDeliveryAllowed(
        int deliveriesInLast24Hours,
        DateTimeOffset? lastDeliveryAtUtc,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (deliveriesInLast24Hours >= MaxDeliveriesPerRelationshipPerDay)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalReminderRateLimited,
                "Reminder delivery rate limit exceeded for this relationship.");
        }

        if (lastDeliveryAtUtc is not null)
        {
            EnsureUtc(lastDeliveryAtUtc.Value);
            if (utcNow - lastDeliveryAtUtc.Value < MinIntervalBetweenDeliveries)
            {
                throw new DomainException(
                    DomainErrorCodes.PersonalReminderRateLimited,
                    "Reminders must be spaced at least one hour apart.");
            }
        }
    }

    /// <summary>Minimized lock-screen / push preview — no balances or exact amounts.</summary>
    public static string BuildMinimizedPreview(string? customMessage = null)
    {
        if (!string.IsNullOrWhiteSpace(customMessage))
        {
            var trimmed = customMessage.Trim();
            return trimmed.Length <= 80 ? trimmed : trimmed[..80];
        }

        return "You have a Personal Utang reminder.";
    }

    private static string? NormalizeMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var trimmed = message.Trim();
        return trimmed.Length > 280 ? trimmed[..280] : trimmed;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }
}

public sealed class PersonalReminderId : IEquatable<PersonalReminderId>
{
    public Guid Value { get; }

    private PersonalReminderId(Guid value) => Value = value;

    public static PersonalReminderId New() => new(Guid.NewGuid());

    public static PersonalReminderId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidPersonalReminderId, "Reminder id is required.");
        }

        return new PersonalReminderId(value);
    }

    public bool Equals(PersonalReminderId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is PersonalReminderId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(PersonalReminderId? left, PersonalReminderId? right) => Equals(left, right);

    public static bool operator !=(PersonalReminderId? left, PersonalReminderId? right) => !Equals(left, right);
}
