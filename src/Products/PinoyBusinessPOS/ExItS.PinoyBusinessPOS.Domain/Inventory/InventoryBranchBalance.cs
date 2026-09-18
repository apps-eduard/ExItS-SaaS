using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Per-branch physical on-hand and reservation overlay for one catalog product.
/// <see cref="OnHandQuantity"/> is physical stock only; reservations never reduce it.
/// </summary>
public sealed class InventoryBranchBalance
{
    public PosOrganizationId OrganizationId { get; }
    public PosBranchId BranchId { get; }
    public CatalogProductId ProductId { get; }
    public decimal OnHandQuantity { get; private set; }
    public decimal ReservedQuantity { get; private set; }
    public decimal PendingReturnQuantity { get; private set; }
    /// <summary>
    /// Sellable stock. Pending returns are excluded until disposition finalizes sellable qty.
    /// </summary>
    public decimal AvailableQuantity => OnHandQuantity - ReservedQuantity - PendingReturnQuantity;
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private InventoryBranchBalance(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        decimal onHandQuantity,
        decimal reservedQuantity,
        decimal pendingReturnQuantity,
        DateTimeOffset updatedAtUtc)
    {
        OrganizationId = organizationId;
        BranchId = branchId;
        ProductId = productId;
        OnHandQuantity = onHandQuantity;
        ReservedQuantity = reservedQuantity;
        PendingReturnQuantity = pendingReturnQuantity;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static InventoryBranchBalance Create(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        decimal onHandQuantity,
        DateTimeOffset utcNow,
        decimal reservedQuantity = 0m,
        decimal pendingReturnQuantity = 0m)
    {
        EnsureUtc(utcNow);
        if (onHandQuantity < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryInsufficientStock,
                "Branch on-hand cannot be negative.");
        }

        if (reservedQuantity < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryReservationQuantity,
                "Branch reserved quantity cannot be negative.");
        }

        if (reservedQuantity > onHandQuantity)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryInsufficientStock,
                "Branch reserved quantity cannot exceed on-hand.");
        }

        if (pendingReturnQuantity < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryPendingReturnQuantity,
                "Branch pending return quantity cannot be negative.");
        }

        return new InventoryBranchBalance(
            organizationId,
            branchId,
            productId,
            onHandQuantity,
            reservedQuantity,
            pendingReturnQuantity,
            utcNow);
    }

    public static InventoryBranchBalance Rehydrate(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        CatalogProductId productId,
        decimal onHandQuantity,
        DateTimeOffset updatedAtUtc,
        decimal reservedQuantity = 0m,
        decimal pendingReturnQuantity = 0m) =>
        new(organizationId, branchId, productId, onHandQuantity, reservedQuantity, pendingReturnQuantity, updatedAtUtc);

    public void Apply(decimal signedQuantity, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (signedQuantity == 0m)
        {
            return;
        }

        var next = OnHandQuantity + signedQuantity;
        if (next < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryInsufficientStock,
                "Insufficient branch stock for this movement.");
        }

        if (next < ReservedQuantity)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryInsufficientStock,
                "Stock movement would leave branch reserved quantity uncovered.");
        }

        OnHandQuantity = next;
        UpdatedAtUtc = utcNow;
    }

    public void Reserve(decimal quantity, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsurePositiveReservationQuantity(quantity);
        if (AvailableQuantity < quantity)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryInsufficientStock,
                "Insufficient available branch stock to reserve.");
        }

        ReservedQuantity += quantity;
        UpdatedAtUtc = utcNow;
    }

    public void Release(decimal quantity, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsurePositiveReservationQuantity(quantity);
        if (ReservedQuantity < quantity)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryReservationQuantity,
                "Cannot release more than the reserved branch quantity.");
        }

        ReservedQuantity -= quantity;
        UpdatedAtUtc = utcNow;
    }

    public void ConsumeReservation(decimal quantity, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsurePositiveReservationQuantity(quantity);
        if (ReservedQuantity < quantity)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryReservationQuantity,
                "Cannot consume more than the reserved branch quantity.");
        }

        ReservedQuantity -= quantity;
        Apply(-quantity, utcNow);
    }

    public void IncreasePendingReturn(decimal quantity, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsurePositivePendingReturnQuantity(quantity);
        PendingReturnQuantity += quantity;
        UpdatedAtUtc = utcNow;
    }

    public void DecreasePendingReturn(decimal quantity, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsurePositivePendingReturnQuantity(quantity);
        if (PendingReturnQuantity < quantity)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryPendingReturnQuantity,
                "Cannot decrease more than pending branch return quantity.");
        }

        PendingReturnQuantity -= quantity;
        UpdatedAtUtc = utcNow;
    }

    private static void EnsurePositiveReservationQuantity(decimal quantity)
    {
        if (quantity <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryReservationQuantity,
                "Reservation quantity must be greater than zero.");
        }
    }

    private static void EnsurePositivePendingReturnQuantity(decimal quantity)
    {
        if (quantity <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryPendingReturnQuantity,
                "Pending return quantity must be greater than zero.");
        }
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }
}
