using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// One inventory account per organization/product. On-hand is a denormalized projection of
/// immutable stock movements, updated only through <see cref="ApplyMovementEffect"/>.
/// </summary>
public sealed class InventoryAccount
{
    public InventoryAccountId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public CatalogProductId ProductId { get; }
    public bool IsTracked { get; private set; }
    public decimal? ReorderLevel { get; private set; }
    public decimal OnHandQuantity { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private InventoryAccount(
        InventoryAccountId id,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        bool isTracked,
        decimal? reorderLevel,
        decimal onHandQuantity,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        ProductId = productId;
        IsTracked = isTracked;
        ReorderLevel = reorderLevel;
        OnHandQuantity = onHandQuantity;
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
            onHandQuantity: 0m,
            utcNow,
            utcNow);
    }

    public static InventoryAccount Rehydrate(
        InventoryAccountId id,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        bool isTracked,
        decimal? reorderLevel,
        decimal onHandQuantity,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            organizationId,
            productId,
            isTracked,
            reorderLevel,
            onHandQuantity,
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
        bool hasOpeningStockAlready)
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
            utcNow);
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

        if (OnHandQuantity != 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryDisableRequiresZero,
                "Disable tracking only when on-hand quantity is zero.");
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

        OnHandQuantity = next;
        // UpdatedAtUtc is set by callers that own the wall-clock (Enable/Disable/SetReorderLevel/sale hooks).
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

    public bool IsLowStock =>
        IsTracked && ReorderLevel is not null && OnHandQuantity <= ReorderLevel.Value;

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

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }
}
