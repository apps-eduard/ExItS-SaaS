using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Organization-scoped in-app notification (e.g. customer-link Accept/Decline responses).
/// Recipients are specific org actors — never broadcast to every member.
/// </summary>
public sealed class OrganizationInAppNotification
{
    public OrganizationInAppNotificationId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public PlatformUserId RecipientUserIdentityId { get; }
    public string Title { get; }
    public string Preview { get; }
    public string RelatedType { get; }
    public string? RelatedId { get; }
    public bool IsRead { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset? ReadAtUtc { get; private set; }

    private OrganizationInAppNotification(
        OrganizationInAppNotificationId id,
        PlatformOrganizationId organizationId,
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
        OrganizationId = organizationId;
        RecipientUserIdentityId = recipientUserIdentityId;
        Title = title;
        Preview = preview;
        RelatedType = relatedType;
        RelatedId = relatedId;
        IsRead = isRead;
        CreatedAtUtc = createdAtUtc;
        ReadAtUtc = readAtUtc;
    }

    public static OrganizationInAppNotification Create(
        PlatformOrganizationId organizationId,
        PlatformUserId recipientUserIdentityId,
        string title,
        string preview,
        string relatedType,
        DateTimeOffset utcNow,
        string? relatedId = null,
        OrganizationInAppNotificationId? id = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(recipientUserIdentityId);
        EnsureUtc(utcNow);
        title = NormalizeRequired(title, 120, "title");
        preview = NormalizeRequired(preview, 200, "preview");
        relatedType = NormalizeRequired(relatedType, 64, "relatedType");

        return new OrganizationInAppNotification(
            id ?? OrganizationInAppNotificationId.New(),
            organizationId,
            recipientUserIdentityId,
            title,
            preview,
            relatedType,
            relatedId,
            isRead: false,
            utcNow,
            readAtUtc: null);
    }

    public static OrganizationInAppNotification Rehydrate(
        OrganizationInAppNotificationId id,
        PlatformOrganizationId organizationId,
        PlatformUserId recipientUserIdentityId,
        string title,
        string preview,
        string relatedType,
        string? relatedId,
        bool isRead,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? readAtUtc) =>
        new(id, organizationId, recipientUserIdentityId, title, preview, relatedType, relatedId, isRead, createdAtUtc, readAtUtc);

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
            throw new DomainException(DomainErrorCodes.InvalidOrganizationNotificationId, $"{fieldName} is required.");
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

public sealed class OrganizationInAppNotificationId : IEquatable<OrganizationInAppNotificationId>
{
    public Guid Value { get; }

    private OrganizationInAppNotificationId(Guid value) => Value = value;

    public static OrganizationInAppNotificationId New() => new(Guid.NewGuid());

    public static OrganizationInAppNotificationId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationNotificationId,
                "Notification id is required.");
        }

        return new OrganizationInAppNotificationId(value);
    }

    public bool Equals(OrganizationInAppNotificationId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is OrganizationInAppNotificationId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(OrganizationInAppNotificationId? left, OrganizationInAppNotificationId? right) =>
        Equals(left, right);

    public static bool operator !=(OrganizationInAppNotificationId? left, OrganizationInAppNotificationId? right) =>
        !Equals(left, right);
}

/// <summary>RelatedType values for customer-link consent notifications.</summary>
public static class CustomerLinkNotificationTypes
{
    public const string PersonalPendingRequest = "CustomerLinkRequest";
    public const string OrganizationAccepted = "CustomerLinkAccepted";
    public const string OrganizationDeclined = "CustomerLinkDeclined";
}
