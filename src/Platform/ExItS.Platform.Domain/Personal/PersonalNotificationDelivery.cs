using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Personal;

/// <summary>Delivery audit record for Personal notifications and reminders.</summary>
public sealed class PersonalNotificationDelivery
{
    public PersonalNotificationDeliveryId Id { get; }
    public PersonalReminderId? ReminderId { get; }
    public PersonalInAppNotificationId? NotificationId { get; }
    public PlatformUserId RecipientUserIdentityId { get; }
    public PersonalNotificationChannel Channel { get; }
    public PersonalNotificationDeliveryStatus Status { get; private set; }
    public string PreviewText { get; }
    public DateTimeOffset AttemptedAtUtc { get; }
    public DateTimeOffset? DeliveredAtUtc { get; private set; }
    public string? FailureReason { get; private set; }

    private PersonalNotificationDelivery(
        PersonalNotificationDeliveryId id,
        PersonalReminderId? reminderId,
        PersonalInAppNotificationId? notificationId,
        PlatformUserId recipientUserIdentityId,
        PersonalNotificationChannel channel,
        PersonalNotificationDeliveryStatus status,
        string previewText,
        DateTimeOffset attemptedAtUtc,
        DateTimeOffset? deliveredAtUtc,
        string? failureReason)
    {
        Id = id;
        ReminderId = reminderId;
        NotificationId = notificationId;
        RecipientUserIdentityId = recipientUserIdentityId;
        Channel = channel;
        Status = status;
        PreviewText = previewText;
        AttemptedAtUtc = attemptedAtUtc;
        DeliveredAtUtc = deliveredAtUtc;
        FailureReason = failureReason;
    }

    public static PersonalNotificationDelivery Create(
        PlatformUserId recipientUserIdentityId,
        PersonalNotificationChannel channel,
        string previewText,
        DateTimeOffset utcNow,
        PersonalReminderId? reminderId = null,
        PersonalInAppNotificationId? notificationId = null,
        PersonalNotificationDeliveryId? id = null)
    {
        ArgumentNullException.ThrowIfNull(recipientUserIdentityId);
        EnsureUtc(utcNow);
        if (!Enum.IsDefined(channel))
        {
            throw new DomainException(DomainErrorCodes.InvalidPersonalReminder, "Notification channel is invalid.");
        }

        previewText = string.IsNullOrWhiteSpace(previewText)
            ? PersonalReminder.BuildMinimizedPreview()
            : previewText.Trim();
        if (previewText.Length > 200)
        {
            previewText = previewText[..200];
        }

        return new PersonalNotificationDelivery(
            id ?? PersonalNotificationDeliveryId.New(),
            reminderId,
            notificationId,
            recipientUserIdentityId,
            channel,
            PersonalNotificationDeliveryStatus.Queued,
            previewText,
            utcNow,
            deliveredAtUtc: null,
            failureReason: null);
    }

    public static PersonalNotificationDelivery Rehydrate(
        PersonalNotificationDeliveryId id,
        PersonalReminderId? reminderId,
        PersonalInAppNotificationId? notificationId,
        PlatformUserId recipientUserIdentityId,
        PersonalNotificationChannel channel,
        PersonalNotificationDeliveryStatus status,
        string previewText,
        DateTimeOffset attemptedAtUtc,
        DateTimeOffset? deliveredAtUtc,
        string? failureReason) =>
        new(
            id,
            reminderId,
            notificationId,
            recipientUserIdentityId,
            channel,
            status,
            previewText,
            attemptedAtUtc,
            deliveredAtUtc,
            failureReason);

    public void MarkDelivered(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        Status = PersonalNotificationDeliveryStatus.Delivered;
        DeliveredAtUtc = utcNow;
        FailureReason = null;
    }

    public void MarkSkipped(string reason)
    {
        Status = PersonalNotificationDeliveryStatus.Skipped;
        FailureReason = TruncateReason(reason);
    }

    public void MarkFailed(string reason)
    {
        Status = PersonalNotificationDeliveryStatus.Failed;
        FailureReason = TruncateReason(reason);
    }

    private static string TruncateReason(string reason)
    {
        var trimmed = string.IsNullOrWhiteSpace(reason) ? "Delivery failed." : reason.Trim();
        return trimmed.Length > 256 ? trimmed[..256] : trimmed;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }
}

public sealed class PersonalNotificationDeliveryId : IEquatable<PersonalNotificationDeliveryId>
{
    public Guid Value { get; }

    private PersonalNotificationDeliveryId(Guid value) => Value = value;

    public static PersonalNotificationDeliveryId New() => new(Guid.NewGuid());

    public static PersonalNotificationDeliveryId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalNotificationId,
                "Delivery audit id is required.");
        }

        return new PersonalNotificationDeliveryId(value);
    }

    public bool Equals(PersonalNotificationDeliveryId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is PersonalNotificationDeliveryId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(PersonalNotificationDeliveryId? left, PersonalNotificationDeliveryId? right) =>
        Equals(left, right);

    public static bool operator !=(PersonalNotificationDeliveryId? left, PersonalNotificationDeliveryId? right) =>
        !Equals(left, right);
}
