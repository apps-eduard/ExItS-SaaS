using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Immutable stock movement. Absolute quantities are validated with the same UOM rules as sale lines;
/// <see cref="QuantityEffect"/> is signed (+ in, − out).
/// </summary>
public sealed class StockMovement
{
    public const int ReasonMaxLength = 512;
    public const string OpeningStockReason = "Opening stock";
    public const string SaleDeductionReason = "Sale deduction";
    public const string SaleVoidRestorationReason = "Sale void restoration";
    public const string PurchaseReceiptReason = "Purchase receipt";
    public const string StockCountVarianceReason = "Stock count variance";

    public StockMovementId Id { get; }
    public PosOrganizationId OrganizationId { get; }
    public CatalogProductId ProductId { get; }
    public InventoryAccountId InventoryAccountId { get; }
    public StockMovementType MovementType { get; }
    public decimal QuantityEffect { get; }
    public string Reason { get; }
    public StockMovementSourceType SourceType { get; }
    public Guid? SourceId { get; }
    public DateTimeOffset RecordedAtUtc { get; }
    public Guid RecordedBy { get; }

    private StockMovement(
        StockMovementId id,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        StockMovementType movementType,
        decimal quantityEffect,
        string reason,
        StockMovementSourceType sourceType,
        Guid? sourceId,
        DateTimeOffset recordedAtUtc,
        Guid recordedBy)
    {
        Id = id;
        OrganizationId = organizationId;
        ProductId = productId;
        InventoryAccountId = inventoryAccountId;
        MovementType = movementType;
        QuantityEffect = quantityEffect;
        Reason = reason;
        SourceType = sourceType;
        SourceId = sourceId;
        RecordedAtUtc = recordedAtUtc;
        RecordedBy = recordedBy;
    }

    public static StockMovement OpeningStock(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementId? id = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.OpeningStock,
            absolute,
            OpeningStockReason,
            StockMovementSourceType.Opening,
            sourceId: null,
            utcNow,
            actorId);
    }

    public static StockMovement ManualIncrease(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        string reason,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementId? id = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.ManualIncrease,
            absolute,
            NormalizeAdjustmentReason(reason),
            StockMovementSourceType.Manual,
            sourceId: null,
            utcNow,
            actorId);
    }

    public static StockMovement ManualDecrease(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        string reason,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementId? id = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.ManualDecrease,
            -absolute,
            NormalizeAdjustmentReason(reason),
            StockMovementSourceType.Manual,
            sourceId: null,
            utcNow,
            actorId);
    }

    public static StockMovement SaleDeduction(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid saleId,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementId? id = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (saleId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleId,
                "SaleId cannot be an empty GUID.");
        }

        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.SaleDeduction,
            -absolute,
            SaleDeductionReason,
            StockMovementSourceType.Sale,
            saleId,
            utcNow,
            actorId);
    }

    public static StockMovement SaleVoidRestoration(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid saleId,
        Guid actorId,
        DateTimeOffset utcNow,
        string? reason = null,
        StockMovementId? id = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (saleId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleId,
                "SaleId cannot be an empty GUID.");
        }

        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure);
        var restoredReason = string.IsNullOrWhiteSpace(reason)
            ? SaleVoidRestorationReason
            : NormalizeOptionalReason(reason);

        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.SaleVoidRestoration,
            absolute,
            restoredReason,
            StockMovementSourceType.Sale,
            saleId,
            utcNow,
            actorId);
    }

    public static StockMovement PurchaseReceipt(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid goodsReceiptId,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementId? id = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (goodsReceiptId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGoodsReceiptId,
                "GoodsReceiptId cannot be an empty GUID.");
        }

        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.PurchaseReceipt,
            absolute,
            PurchaseReceiptReason,
            StockMovementSourceType.PurchaseReceipt,
            goodsReceiptId,
            utcNow,
            actorId);
    }

    public static StockMovement StockCountVarianceIncrease(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid stockCountId,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementId? id = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (stockCountId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockCountId,
                "StockCountId cannot be an empty GUID.");
        }

        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.StockCountVarianceIncrease,
            absolute,
            StockCountVarianceReason,
            StockMovementSourceType.StockCount,
            stockCountId,
            utcNow,
            actorId);
    }

    public static StockMovement StockCountVarianceDecrease(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid stockCountId,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementId? id = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (stockCountId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockCountId,
                "StockCountId cannot be an empty GUID.");
        }

        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.StockCountVarianceDecrease,
            -absolute,
            StockCountVarianceReason,
            StockMovementSourceType.StockCount,
            stockCountId,
            utcNow,
            actorId);
    }

    public static StockMovement Rehydrate(
        StockMovementId id,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        StockMovementType movementType,
        decimal quantityEffect,
        string reason,
        StockMovementSourceType sourceType,
        Guid? sourceId,
        DateTimeOffset recordedAtUtc,
        Guid recordedBy) =>
        new(
            id,
            organizationId,
            productId,
            inventoryAccountId,
            movementType,
            quantityEffect,
            reason,
            sourceType,
            sourceId,
            recordedAtUtc,
            recordedBy);

    private static string NormalizeAdjustmentReason(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.InventoryAdjustmentReasonRequired,
                "A reason is required for manual stock adjustments.");
        }

        return NormalizeOptionalReason(reason);
    }

    private static string NormalizeOptionalReason(string reason)
    {
        var trimmed = reason.Trim();
        if (trimmed.Length > ReasonMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InventoryAdjustmentReasonRequired,
                $"Reason must be at most {ReasonMaxLength} characters.");
        }

        return trimmed;
    }

    private static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }

    private static void EnsureActor(Guid actorId)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleActor,
                "A non-empty actor identifier is required for stock movements.");
        }
    }
}
