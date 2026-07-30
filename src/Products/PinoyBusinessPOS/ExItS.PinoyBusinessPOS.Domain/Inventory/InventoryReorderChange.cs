using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Append-only audit of reorder level/quantity configuration changes for a tracked product.
/// </summary>
public sealed class InventoryReorderChange
{
    public const int ReasonMaxLength = 512;

    public InventoryReorderChangeId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public InventoryAccountId InventoryAccountId { get; }
    public CatalogProductId ProductId { get; }
    public decimal? PreviousReorderLevel { get; }
    public decimal? NewReorderLevel { get; }
    public decimal? PreviousReorderQuantity { get; }
    public decimal? NewReorderQuantity { get; }
    public string Reason { get; }
    public Guid ChangedBy { get; }
    public DateTimeOffset ChangedAtUtc { get; }

    private InventoryReorderChange(
        InventoryReorderChangeId id,
        PosOrganizationId organizationId,
        InventoryAccountId inventoryAccountId,
        CatalogProductId productId,
        decimal? previousReorderLevel,
        decimal? newReorderLevel,
        decimal? previousReorderQuantity,
        decimal? newReorderQuantity,
        string reason,
        Guid changedBy,
        DateTimeOffset changedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        InventoryAccountId = inventoryAccountId;
        ProductId = productId;
        PreviousReorderLevel = previousReorderLevel;
        NewReorderLevel = newReorderLevel;
        PreviousReorderQuantity = previousReorderQuantity;
        NewReorderQuantity = newReorderQuantity;
        Reason = reason;
        ChangedBy = changedBy;
        ChangedAtUtc = changedAtUtc;
    }

    public static InventoryReorderChange Create(
        PosOrganizationId organizationId,
        InventoryAccountId inventoryAccountId,
        CatalogProductId productId,
        decimal? previousReorderLevel,
        decimal? newReorderLevel,
        decimal? previousReorderQuantity,
        decimal? newReorderQuantity,
        string reason,
        Guid changedBy,
        DateTimeOffset utcNow,
        InventoryReorderChangeId? id = null)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }

        if (changedBy == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryReorderActor,
                "Actor id must be a non-empty GUID.");
        }

        if (previousReorderLevel == newReorderLevel && previousReorderQuantity == newReorderQuantity)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryReorderUnchanged,
                "Reorder configuration must differ from the previous values.");
        }

        return new InventoryReorderChange(
            id ?? InventoryReorderChangeId.New(),
            organizationId,
            inventoryAccountId,
            productId,
            previousReorderLevel,
            newReorderLevel,
            previousReorderQuantity,
            newReorderQuantity,
            NormalizeReason(reason),
            changedBy,
            utcNow);
    }

    public static InventoryReorderChange Rehydrate(
        InventoryReorderChangeId id,
        PosOrganizationId organizationId,
        InventoryAccountId inventoryAccountId,
        CatalogProductId productId,
        decimal? previousReorderLevel,
        decimal? newReorderLevel,
        decimal? previousReorderQuantity,
        decimal? newReorderQuantity,
        string reason,
        Guid changedBy,
        DateTimeOffset changedAtUtc) =>
        new(
            id,
            organizationId,
            inventoryAccountId,
            productId,
            previousReorderLevel,
            newReorderLevel,
            previousReorderQuantity,
            newReorderQuantity,
            reason,
            changedBy,
            changedAtUtc);

    public static string NormalizeReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryReorderReason,
                "Reorder configuration change reason is required.");
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > ReasonMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryReorderReason,
                $"Reorder configuration change reason must be at most {ReasonMaxLength} characters.");
        }

        return trimmed;
    }
}
