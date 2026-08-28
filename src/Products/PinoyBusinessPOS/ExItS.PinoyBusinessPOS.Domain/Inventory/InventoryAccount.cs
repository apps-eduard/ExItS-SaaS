using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// One inventory account per organization/product. On-hand is a denormalized projection of
/// immutable stock movements, updated only through <see cref="ApplyMovementEffect"/>.
/// Reserved quantity reduces available stock for customer-order holds without changing on-hand
/// until consumption.
/// </summary>
public sealed class InventoryAccount
{
    public InventoryAccountId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public CatalogProductId ProductId { get; }
    public bool IsTracked { get; private set; }
    public decimal? ReorderLevel { get; private set; }
    public decimal? ReorderQuantity { get; private set; }
    public decimal OnHandQuantity { get; private set; }
    public decimal ReservedQuantity { get; private set; }
    public decimal AvailableQuantity => OnHandQuantity - ReservedQuantity;
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private InventoryAccount(
        InventoryAccountId id,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        bool isTracked,
        decimal? reorderLevel,
        decimal? reorderQuantity,
        decimal onHandQuantity,
        decimal reservedQuantity,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        ProductId = productId;
        IsTracked = isTracked;
        ReorderLevel = reorderLevel;
        ReorderQuantity = reorderQuantity;
        OnHandQuantity = onHandQuantity;
        ReservedQuantity = reservedQuantity;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>
    /// Creates an untracked account shell for a catalog product. Tracking starts via <see cref="Enable"/>.
    /// </summary>
    public static InventoryAccount CreateUntracked(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        DateTimeOffset utcNow,
        InventoryAccountId? id = null)
    {
        EnsureUtc(utcNow);
        return new InventoryAccount(
            id ?? InventoryAccountId.New(),
            organizationId,
            productId,
            isTracked: false,
            reorderLevel: null,
            reorderQuantity: null,
            onHandQuantity: 0m,
            reservedQuantity: 0m,
            utcNow,
            utcNow);
    }

    public static InventoryAccount Rehydrate(
        InventoryAccountId id,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        bool isTracked,
        decimal? reorderLevel,
        decimal? reorderQuantity,
        decimal onHandQuantity,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        decimal reservedQuantity = 0m) =>
        new(
            id,
            organizationId,
            productId,
            isTracked,
            reorderLevel,
            reorderQuantity,
            onHandQuantity,
            reservedQuantity,
            createdAtUtc,
            updatedAtUtc);

    /// <summary>
    /// Enables tracking. When already tracked this is a no-op (no second opening).
    /// Opening stock is applied only when <paramref name="hasOpeningStockAlready"/> is false and
    /// <paramref name="openingQuantity"/> is greater than zero.
    /// </summary>
    public StockMovement? Enable(
        decimal? openingQuantity,
        UnitOfMeasure unitOfMeasure,
        Guid actorId,
        DateTimeOffset utcNow,
        bool hasOpeningStockAlready,
        SellingMode sellingMode = SellingMode.PerItem,
        decimal? openingUnitCost = null)
    {
        EnsureUtc(utcNow);

        if (IsTracked)
        {
            return null;
        }

        IsTracked = true;
        UpdatedAtUtc = utcNow;

        if (openingQuantity is null || openingQuantity.Value == 0m)
        {
            return null;
        }

        if (hasOpeningStockAlready)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryOpeningDuplicate,
                "Opening stock has already been recorded for this product.");
        }

        var opening = StockMovement.OpeningStock(
            OrganizationId,
            ProductId,
            Id,
            openingQuantity.Value,
            unitOfMeasure,
            actorId,
            utcNow,
            sellingMode: sellingMode,
            unitCost: openingUnitCost);
        ApplyMovementEffect(opening.QuantityEffect);
        return opening;
    }

    /// <summary>
    /// Records opening stock on an already tracked account with zero on-hand and no prior opening movement.
    /// </summary>
    public StockMovement RecordOpeningStock(
        decimal openingQuantity,
        UnitOfMeasure unitOfMeasure,
        Guid actorId,
        DateTimeOffset utcNow,
        bool hasOpeningStockAlready,
        SellingMode sellingMode = SellingMode.PerItem,
        decimal? openingUnitCost = null)
    {
        EnsureUtc(utcNow);

        if (!IsTracked)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryNotTracked,
                "Inventory is not tracked for this product.");
        }

        if (hasOpeningStockAlready)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryOpeningDuplicate,
                "Opening stock has already been recorded for this product.");
        }

        if (OnHandQuantity != 0m || ReservedQuantity != 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryOpeningRequiresZeroOnHand,
                "Opening stock can only be added when on-hand and reserved quantities are zero.");
        }

        if (openingQuantity <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryQuantity,
                "Opening stock quantity must be greater than zero.");
        }

        UpdatedAtUtc = utcNow;

        var opening = StockMovement.OpeningStock(
            OrganizationId,
            ProductId,
            Id,
            openingQuantity,
            unitOfMeasure,
            actorId,
            utcNow,
            sellingMode: sellingMode,
            unitCost: openingUnitCost);
        ApplyMovementEffect(opening.QuantityEffect);
        return opening;
    }

    /// <summary>Disables tracking only when currently tracked and on-hand is exactly zero.</summary>
    public void Disable(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);

        if (!IsTracked)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryNotTracked,
                "Inventory is not tracked for this product.");
        }

        if (OnHandQuantity != 0m || ReservedQuantity != 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryDisableRequiresZero,
                "Disable tracking only when on-hand and reserved quantities are zero.");
        }

        IsTracked = false;
        UpdatedAtUtc = utcNow;
    }

    public void ApplyMovementEffect(decimal signedQuantity)
    {
        if (signedQuantity == 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryQuantity,
                "Stock movement quantity effect cannot be zero.");
        }

        var next = OnHandQuantity + signedQuantity;
        if (next < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryInsufficientStock,
                "Insufficient stock for this movement.");
        }

        if (next < ReservedQuantity)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryInsufficientStock,
                "Stock movement would leave reserved quantity uncovered.");
        }

        OnHandQuantity = next;
        // UpdatedAtUtc is set by callers that own the wall-clock (Enable/Disable/SetReorderLevel/sale hooks).
    }

    /// <summary>
    /// Holds quantity for a customer order. Untracked accounts treat reservation as a no-op success.
    /// </summary>
    public void Reserve(decimal quantity)
    {
        EnsurePositiveReservationQuantity(quantity);

        if (!IsTracked)
        {
            return;
        }

        if (AvailableQuantity < quantity)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryInsufficientStock,
                "Insufficient available stock to reserve.");
        }

        ReservedQuantity += quantity;
    }

    /// <summary>
    /// Releases a prior reservation. Untracked accounts treat release as a no-op success.
    /// </summary>
    public void Release(decimal quantity)
    {
        EnsurePositiveReservationQuantity(quantity);

        if (!IsTracked)
        {
            return;
        }

        if (ReservedQuantity < quantity)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryReservationQuantity,
                "Cannot release more than the reserved quantity.");
        }

        ReservedQuantity -= quantity;
    }

    /// <summary>
    /// Converts a reservation into an on-hand deduction (sale/fulfillment). Untracked accounts are a no-op.
    /// </summary>
    public void ConsumeReservation(decimal quantity)
    {
        EnsurePositiveReservationQuantity(quantity);

        if (!IsTracked)
        {
            return;
        }

        if (ReservedQuantity < quantity)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryReservationQuantity,
                "Cannot consume more than the reserved quantity.");
        }

        ReservedQuantity -= quantity;
        ApplyMovementEffect(-quantity);
    }

    public void Touch(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        UpdatedAtUtc = utcNow;
    }

    public void SetReorderLevel(decimal? reorderLevel, UnitOfMeasure unitOfMeasure, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        ReorderLevel = NormalizeReorderLevel(reorderLevel, unitOfMeasure);
        UpdatedAtUtc = utcNow;
    }

    public void SetReorderConfiguration(
        decimal? reorderLevel,
        decimal? reorderQuantity,
        UnitOfMeasure unitOfMeasure,
        DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);

        if (!IsTracked)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryNotTracked,
                "Inventory is not tracked for this product.");
        }

        ReorderLevel = NormalizeReorderLevel(reorderLevel, unitOfMeasure);
        ReorderQuantity = NormalizeReorderQuantity(reorderQuantity, unitOfMeasure);
        UpdatedAtUtc = utcNow;
    }

    public InventoryStockStatus StockStatus =>
        InventoryStockStatuses.Derive(IsTracked, OnHandQuantity, ReorderLevel);

    public bool IsLowStock =>
        IsTracked
        && ReorderLevel is not null
        && OnHandQuantity > 0m
        && OnHandQuantity <= ReorderLevel.Value;

    public bool IsReorderSuggested =>
        IsTracked && InventoryStockStatuses.IsReorderSuggested(OnHandQuantity, ReorderLevel);

    public decimal? SuggestedOrderQuantity =>
        IsTracked
            ? InventoryStockStatuses.SuggestedOrderQuantity(OnHandQuantity, ReorderLevel, ReorderQuantity)
            : null;

    public static decimal? NormalizeReorderLevel(decimal? reorderLevel, UnitOfMeasure unitOfMeasure)
    {
        if (reorderLevel is null)
        {
            return null;
        }

        if (reorderLevel.Value < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryReorderLevelInvalid,
                "Reorder level cannot be negative.");
        }

        if (reorderLevel.Value == 0m)
        {
            return 0m;
        }

        var maxDecimals = SaleMoney.MaxQuantityDecimals(unitOfMeasure);
        if (!SaleMoney.HasAtMostDecimals(reorderLevel.Value, maxDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InventoryReorderLevelInvalid,
                maxDecimals == 0
                    ? $"{unitOfMeasure} reorder levels must be whole numbers."
                    : $"{unitOfMeasure} reorder levels may have at most {maxDecimals} decimal places.");
        }

        return reorderLevel.Value;
    }

    public static decimal? NormalizeReorderQuantity(decimal? reorderQuantity, UnitOfMeasure unitOfMeasure)
    {
        if (reorderQuantity is null)
        {
            return null;
        }

        if (reorderQuantity.Value <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryReorderQuantityInvalid,
                "Reorder quantity must be greater than zero when set.");
        }

        var maxDecimals = SaleMoney.MaxQuantityDecimals(unitOfMeasure);
        if (!SaleMoney.HasAtMostDecimals(reorderQuantity.Value, maxDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InventoryReorderQuantityInvalid,
                maxDecimals == 0
                    ? $"{unitOfMeasure} reorder quantities must be whole numbers."
                    : $"{unitOfMeasure} reorder quantities may have at most {maxDecimals} decimal places.");
        }

        return reorderQuantity.Value;
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

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }
}
