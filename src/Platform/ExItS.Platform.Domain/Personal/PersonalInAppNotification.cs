using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Personal;

/// <summary>In-app notification for Personal Scope. Previews must minimize sensitive debt values.</summary>
public sealed class PersonalInAppNotification
{
    public PersonalInAppNotificationId Id { get; }
    public PlatformUserId RecipientUserIdentityId { get; }
    public string Title { get; }
    public string Preview { get; }
    public string RelatedType { get; }
    public string? RelatedId { get; }
    public bool IsRead { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? ReadAtUtc { get; private set; }

    private PersonalInAppNotification(
        PersonalInAppNotificationId id,
        PlatformUserId recipientUserIdentityId,
        string title,
        string preview,
        string relatedType,
        string? relatedId,
        bool isRead,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? readAtUtc)
    {
        Id = id;
        RecipientUserIdentityId = recipientUserIdentityId;
        Title = title;
        Preview = preview;
        RelatedType = relatedType;
        RelatedId = relatedId;
        IsRead = isRead;
        CreatedAtUtc = createdAtUtc;
        ReadAtUtc = readAtUtc;
    }

    public static PersonalInAppNotification Create(
        PlatformUserId recipientUserIdentityId,
        string title,
        string preview,
        string relatedType,
        DateTimeOffset utcNow,
        string? relatedId = null,
        PersonalInAppNotificationId? id = null)
    {
        ArgumentNullException.ThrowIfNull(recipientUserIdentityId);
        EnsureUtc(utcNow);
        title = NormalizeRequired(title, 120, "title");
        preview = NormalizeRequired(preview, 200, "preview");
        relatedType = NormalizeRequired(relatedType, 64, "relatedType");

        return new PersonalInAppNotification(
            id ?? PersonalInAppNotificationId.New(),
            recipientUserIdentityId,
            title,
            preview,
            relatedType,
            relatedId,
            isRead: false,
            utcNow,
            readAtUtc: null);
    }

    public static PersonalInAppNotification Rehydrate(
        PersonalInAppNotificationId id,
        PlatformUserId recipientUserIdentityId,
        string title,
        string preview,
        string relatedType,
        string? relatedId,
        bool isRead,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? readAtUtc) =>
        new(id, recipientUserIdentityId, title, preview, relatedType, relatedId, isRead, createdAtUtc, readAtUtc);

    public void MarkRead(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAtUtc = utcNow;
    }

    private static string NormalizeRequired(string value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrorCodes.InvalidPersonalNotificationId, $"{fieldName} is required.");
        }

        var trimmed = value.Trim();
        return trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }
}

public sealed class PersonalInAppNotificationId : IEquatable<PersonalInAppNotificationId>
{
    public Guid Value { get; }

    private PersonalInAppNotificationId(Guid value) => Value = value;

    public static PersonalInAppNotificationId New() => new(Guid.NewGuid());

    public static PersonalInAppNotificationId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalNotificationId,
                "Notification id is required.");
        }

        return new PersonalInAppNotificationId(value);
    }

    public bool Equals(PersonalInAppNotificationId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is PersonalInAppNotificationId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(PersonalInAppNotificationId? left, PersonalInAppNotificationId? right) =>
        Equals(left, right);

    public static bool operator !=(PersonalInAppNotificationId? left, PersonalInAppNotificationId? right) =>
        !Equals(left, right);
}
