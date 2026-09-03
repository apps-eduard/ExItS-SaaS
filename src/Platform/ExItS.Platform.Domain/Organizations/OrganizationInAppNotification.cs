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
    /// <summary>
    /// Operational branch this notification is addressed to. Null means organization-wide:
    /// legacy rows stay null and are never backfilled with a guessed branch.
    /// </summary>
    public Guid? BranchId { get; }
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
        DateTimeOffset? readAtUtc,
        Guid? branchId)
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
        BranchId = branchId == Guid.Empty ? null : branchId;
    }

    public static OrganizationInAppNotification Create(
        PlatformOrganizationId organizationId,
        PlatformUserId recipientUserIdentityId,
        string title,
        string preview,
        string relatedType,
        DateTimeOffset utcNow,
        string? relatedId = null,
        OrganizationInAppNotificationId? id = null,
        Guid? branchId = null)
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
            readAtUtc: null,
            branchId);
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
        DateTimeOffset? readAtUtc,
        Guid? branchId = null) =>
        new(id, organizationId, recipientUserIdentityId, title, preview, relatedType, relatedId, isRead, createdAtUtc, readAtUtc, branchId);

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
    public const string PersonalCustomerLinkReminder = "PersonalCustomerLinkReminder";
    public const string OrganizationAccepted = "CustomerLinkAccepted";
    public const string OrganizationDeclined = "CustomerLinkDeclined";
}

/// <summary>RelatedType values for Connected ExItS supplier connection lifecycle.</summary>
public static class SupplierConnectionNotificationTypes
{
    public const string Requested = "SupplierConnectionRequested";
    public const string Accepted = "SupplierConnectionAccepted";
    public const string Declined = "SupplierConnectionDeclined";
    /// <summary>Supplier-org local activity after Accept (same-org publish allowed).</summary>
    public const string AcceptedConfirmation = "SupplierConnectionAcceptedConfirmation";
    /// <summary>Supplier-org local activity after Decline (same-org publish allowed).</summary>
    public const string DeclinedConfirmation = "SupplierConnectionDeclinedConfirmation";

    public static bool IsKnown(string? relatedType) =>
        string.Equals(relatedType, Requested, StringComparison.Ordinal)
        || string.Equals(relatedType, Accepted, StringComparison.Ordinal)
        || string.Equals(relatedType, Declined, StringComparison.Ordinal)
        || string.Equals(relatedType, AcceptedConfirmation, StringComparison.Ordinal)
        || string.Equals(relatedType, DeclinedConfirmation, StringComparison.Ordinal);

    /// <summary>Types that may be published into the same organization (supplier activity history).</summary>
    public static bool IsLocalActivity(string? relatedType) =>
        string.Equals(relatedType, AcceptedConfirmation, StringComparison.Ordinal)
        || string.Equals(relatedType, DeclinedConfirmation, StringComparison.Ordinal);
}

/// <summary>Allowlisted RelatedType values products may publish into the organization inbox.</summary>
public static class OrganizationBusinessNotificationTypes
{
    public static bool IsPublishable(string? relatedType) =>
        SupplierConnectionNotificationTypes.IsKnown(relatedType)
        || CustomerOrderNotificationTypes.IsKnown(relatedType)
        || ConnectedPurchaseOrderNotificationTypes.IsKnown(relatedType);

    /// <summary>
    /// Same-organization publish is allowed for supplier local confirmations and seller-facing
    /// customer-order inbox events (e.g. New customer order).
    /// </summary>
    public static bool AllowsSameOrganization(string? relatedType) =>
        SupplierConnectionNotificationTypes.IsLocalActivity(relatedType)
        || CustomerOrderNotificationTypes.IsKnown(relatedType);

    /// <summary>
    /// Types that may be addressed to a single operational branch. Everything else stays
    /// organization-wide even when a caller supplies a target branch.
    /// </summary>
    public static bool IsBranchTargetable(string? relatedType) =>
        string.Equals(relatedType, SupplierConnectionNotificationTypes.Requested, StringComparison.Ordinal);
}

/// <summary>Branch-workspace visibility rule for organization inbox reads and counts.</summary>
public static class OrganizationNotificationBranchScope
{
    /// <summary>
    /// A branch workspace sees its own branch plus organization-wide (null) notifications.
    /// A null <paramref name="workspaceBranchId"/> is the global organization inbox and sees everything.
    /// </summary>
    public static bool IsVisible(Guid? notificationBranchId, Guid? workspaceBranchId) =>
        workspaceBranchId is null
        || notificationBranchId is null
        || notificationBranchId == workspaceBranchId;
}

/// <summary>Personal inbox RelatedType for Organization ownership-transfer requests.</summary>
public static class OwnershipTransferNotificationTypes
{
    public const string Requested = "OrganizationOwnershipTransfer";
}

/// <summary>Personal inbox RelatedType for Organization staff invitations (EX-ID / Personal QR).</summary>
public static class OrganizationStaffInvitationNotificationTypes
{
    public const string PersonalPendingInvite = "OrganizationStaffInvitation";
}

/// <summary>RelatedType values for customer-order lifecycle organization inbox events.</summary>
public static class CustomerOrderNotificationTypes
{
    public const string Submitted = "CustomerOrderSubmitted";
    public const string Accepted = "CustomerOrderAccepted";
    public const string Rejected = "CustomerOrderRejected";
    public const string Cancelled = "CustomerOrderCancelled";
    public const string Ready = "CustomerOrderReady";
    public const string OutForDelivery = "CustomerOrderOutForDelivery";
    public const string Delivered = "CustomerOrderDelivered";
    public const string Collected = "CustomerOrderCollected";
    public const string Completed = "CustomerOrderCompleted";

    public static bool IsKnown(string? relatedType) =>
        string.Equals(relatedType, Submitted, StringComparison.Ordinal)
        || string.Equals(relatedType, Accepted, StringComparison.Ordinal)
        || string.Equals(relatedType, Rejected, StringComparison.Ordinal)
        || string.Equals(relatedType, Cancelled, StringComparison.Ordinal)
        || string.Equals(relatedType, Ready, StringComparison.Ordinal)
        || string.Equals(relatedType, OutForDelivery, StringComparison.Ordinal)
        || string.Equals(relatedType, Delivered, StringComparison.Ordinal)
        || string.Equals(relatedType, Collected, StringComparison.Ordinal)
        || string.Equals(relatedType, Completed, StringComparison.Ordinal);
}

/// <summary>RelatedType values for connected purchase-order lifecycle organization inbox events.</summary>
public static class ConnectedPurchaseOrderNotificationTypes
{
    public const string Submitted = "ConnectedPurchaseOrderSubmitted";
    public const string Accepted = "ConnectedPurchaseOrderAccepted";
    public const string Declined = "ConnectedPurchaseOrderDeclined";
    public const string Preparing = "ConnectedPurchaseOrderPreparing";
    public const string Fulfilled = "ConnectedPurchaseOrderFulfilled";
    public const string Withdrawn = "ConnectedPurchaseOrderWithdrawn";
    public const string Received = "ConnectedPurchaseOrderReceived";
    public const string PartiallyReceived = "ConnectedPurchaseOrderPartiallyReceived";
    public const string ReceivingIssue = "ConnectedPurchaseOrderReceivingIssue";
    public const string ChangesProposed = "ConnectedPurchaseOrderChangesProposed";
    public const string ChangesAccepted = "ConnectedPurchaseOrderChangesAccepted";
    public const string ChangesRejected = "ConnectedPurchaseOrderChangesRejected";

    public static bool IsKnown(string? relatedType) =>
        string.Equals(relatedType, Submitted, StringComparison.Ordinal)
        || string.Equals(relatedType, Accepted, StringComparison.Ordinal)
        || string.Equals(relatedType, Declined, StringComparison.Ordinal)
        || string.Equals(relatedType, Preparing, StringComparison.Ordinal)
        || string.Equals(relatedType, Fulfilled, StringComparison.Ordinal)
        || string.Equals(relatedType, Withdrawn, StringComparison.Ordinal)
        || string.Equals(relatedType, Received, StringComparison.Ordinal)
        || string.Equals(relatedType, PartiallyReceived, StringComparison.Ordinal)
        || string.Equals(relatedType, ReceivingIssue, StringComparison.Ordinal)
        || string.Equals(relatedType, ChangesProposed, StringComparison.Ordinal)
        || string.Equals(relatedType, ChangesAccepted, StringComparison.Ordinal)
        || string.Equals(relatedType, ChangesRejected, StringComparison.Ordinal);

    public static bool IsBuyerFacing(string? relatedType) =>
        string.Equals(relatedType, Accepted, StringComparison.Ordinal)
        || string.Equals(relatedType, Declined, StringComparison.Ordinal)
        || string.Equals(relatedType, Preparing, StringComparison.Ordinal)
        || string.Equals(relatedType, Fulfilled, StringComparison.Ordinal)
        || string.Equals(relatedType, ChangesProposed, StringComparison.Ordinal);

    public static bool IsSupplierFacing(string? relatedType) =>
        string.Equals(relatedType, Submitted, StringComparison.Ordinal)
        || string.Equals(relatedType, Withdrawn, StringComparison.Ordinal)
        || string.Equals(relatedType, Received, StringComparison.Ordinal)
        || string.Equals(relatedType, PartiallyReceived, StringComparison.Ordinal)
        || string.Equals(relatedType, ReceivingIssue, StringComparison.Ordinal)
        || string.Equals(relatedType, ChangesAccepted, StringComparison.Ordinal)
        || string.Equals(relatedType, ChangesRejected, StringComparison.Ordinal);
}
