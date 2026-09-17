namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Platform-controlled, organization-scoped online supplier payments authority.
/// Default is Disabled. Does not perform payment settlement.
/// </summary>
public sealed class OrganizationOnlineSupplierPaymentsCapability
{
    public const int MaxReasonLength = 1000;

    public PlatformOrganizationId OrganizationId { get; }
    public string Status { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public string? UpdatedByActorReference { get; private set; }
    public string? Reason { get; private set; }

    private OrganizationOnlineSupplierPaymentsCapability(
        PlatformOrganizationId organizationId,
        string status,
        DateTimeOffset updatedAtUtc,
        string? updatedByActorReference,
        string? reason)
    {
        OrganizationId = organizationId;
        Status = NormalizeStatus(status);
        UpdatedAtUtc = EnsureUtc(updatedAtUtc);
        UpdatedByActorReference = updatedByActorReference;
        Reason = NormalizeReason(reason);
    }

    public static OrganizationOnlineSupplierPaymentsCapability CreateDefault(
        PlatformOrganizationId organizationId,
        DateTimeOffset utcNow) =>
        new(
            organizationId,
            OrganizationOnlineSupplierPaymentsStatuses.Disabled,
            EnsureUtc(utcNow),
            null,
            null);

    public static OrganizationOnlineSupplierPaymentsCapability Rehydrate(
        PlatformOrganizationId organizationId,
        string status,
        DateTimeOffset updatedAtUtc,
        string? updatedByActorReference,
        string? reason) =>
        new(organizationId, status, updatedAtUtc, updatedByActorReference, reason);

    public bool Enable(string actorReference, DateTimeOffset utcNow, string? reason = null) =>
        Transition(OrganizationOnlineSupplierPaymentsStatuses.Available, actorReference, utcNow, reason);

    public bool Disable(string actorReference, DateTimeOffset utcNow, string? reason = null) =>
        Transition(OrganizationOnlineSupplierPaymentsStatuses.Disabled, actorReference, utcNow, reason);

    public bool Suspend(string actorReference, DateTimeOffset utcNow, string? reason = null) =>
        Transition(OrganizationOnlineSupplierPaymentsStatuses.Suspended, actorReference, utcNow, reason);

    public bool Restore(string actorReference, DateTimeOffset utcNow, string? reason = null) =>
        Transition(OrganizationOnlineSupplierPaymentsStatuses.Available, actorReference, utcNow, reason);

    public bool Transition(
        string targetStatus,
        string actorReference,
        DateTimeOffset utcNow,
        string? reason = null)
    {
        var target = NormalizeStatus(targetStatus);
        if (Status == target)
        {
            Touch(actorReference, utcNow, reason);
            return false;
        }

        if (!IsAllowedTransition(Status, target))
        {
            throw new InvalidOperationException(
                $"Online supplier payments capability cannot move from '{Status}' to '{target}'.");
        }

        Status = target;
        Touch(actorReference, utcNow, reason);
        return true;
    }

    public static bool IsAllowedTransition(string from, string to) =>
        (NormalizeStatus(from), NormalizeStatus(to)) switch
        {
            (OrganizationOnlineSupplierPaymentsStatuses.Disabled, OrganizationOnlineSupplierPaymentsStatuses.Available) => true,
            (OrganizationOnlineSupplierPaymentsStatuses.Available, OrganizationOnlineSupplierPaymentsStatuses.Disabled) => true,
            (OrganizationOnlineSupplierPaymentsStatuses.Available, OrganizationOnlineSupplierPaymentsStatuses.Suspended) => true,
            (OrganizationOnlineSupplierPaymentsStatuses.Suspended, OrganizationOnlineSupplierPaymentsStatuses.Available) => true,
            (OrganizationOnlineSupplierPaymentsStatuses.Suspended, OrganizationOnlineSupplierPaymentsStatuses.Disabled) => true,
            _ => false
        };

    private void Touch(string actorReference, DateTimeOffset utcNow, string? reason)
    {
        if (string.IsNullOrWhiteSpace(actorReference))
        {
            throw new ArgumentException("Actor reference is required.", nameof(actorReference));
        }

        UpdatedAtUtc = EnsureUtc(utcNow);
        UpdatedByActorReference = actorReference.Trim();
        if (reason is not null)
        {
            Reason = NormalizeReason(reason);
        }
    }

    private static string NormalizeStatus(string status)
    {
        if (!OrganizationOnlineSupplierPaymentsStatuses.IsKnown(status))
        {
            throw new ArgumentException($"Unknown online supplier payments status '{status}'.", nameof(status));
        }

        return status;
    }

    private static string? NormalizeReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return null;
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > MaxReasonLength)
        {
            throw new ArgumentException(
                $"Reason must be at most {MaxReasonLength} characters.",
                nameof(reason));
        }

        return trimmed;
    }

    private static DateTimeOffset EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Timestamp must be UTC.", nameof(value));
        }

        return value;
    }
}
