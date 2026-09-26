using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
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
    public const string CustomerOrderFulfillmentReason = "Customer order fulfillment";
    public const string SaleVoidRestorationReason = "Sale void restoration";
    public const string PurchaseReceiptReason = "Purchase receipt";
    public const string DirectPurchaseReceiptReason = "Direct purchase receipt";
    public const string StockCountVarianceReason = "Stock count variance";
    public const string SaleReturnRestockReason = "Sale return restock";
    public const string TransferOutReasonPrefix = "Transfer out";
    public const string TransferInReasonPrefix = "Transfer in";
    public const string TransferCancelRestoreReasonPrefix = "Transfer cancelled";
    public const string StockUseConsumptionReason = "Stock use";
    public const string StockUseVoidRestorationReason = "Stock use void restoration";
    public const string ProductionMaterialConsumptionReason = "Production material used";
    public const string ProductionMaterialRestorationReason = "Production material restored";
    public const string ProductionOutputReason = "Production output";
    public const string ProductionOutputReversalReason = "Production output reversed";
    public const string WasteLossConsumptionReason = "Waste/loss";
    public const string WasteLossVoidRestorationReason = "Waste/loss void restoration";
    public const string PurchaseReceiptReversalReason = "Purchase receipt reversed";
    public const string DirectPurchaseReceiptReversalReason = "Direct purchase reversed";
    public const string ConnectedPurchaseFulfillmentReason = "Connected purchase fulfillment";
    public const string ConnectedPurchaseFulfillmentReconciliationReason =
        "Connected purchase fulfillment reconciliation";
    public const string SaleReturnWriteOffReason = "Sale return write-off";
    public const string ConnectedPoReturnDispatchReason = "Connected PO return dispatched to supplier";
    public const string ConnectedPoReturnRestockReason = "Connected PO return restock";
    public const string ConnectedPoReturnWriteOffReason = "Connected PO return write-off";
    public const string TransferDamageHoldReasonPrefix = "Transfer damage hold";
    public const string TransferDamageRecoveryReasonPrefix = "Transfer damage recovery";
    public const string TransferDamageReturnOutReasonPrefix = "Transfer damage return out";
    public const string TransferDamageReturnInReasonPrefix = "Transfer damage return in";
    public const string TransferDamageWriteOffReasonPrefix = "Transfer damage write-off";
    public const string TransferExceptionHoldReasonPrefix = "Transfer exception hold";
    public const string TransferExceptionExpectedRestoreReasonPrefix = "Transfer exception expected restore";
    public const string TransferExceptionActualOutReasonPrefix = "Transfer exception actual out";
    public const string TransferExceptionReturnOutReasonPrefix = "Transfer exception return out";
    public const string TransferExceptionReturnInReasonPrefix = "Transfer exception return in";
    public const string TransferExceptionReturnRestockReasonPrefix = "Wrong item return received";
    public const string TransferExceptionRecoveryReasonPrefix = "Transfer exception recovery";
    public const string TransferExceptionWriteOffReasonPrefix = "Transfer exception write-off";

    public static string FormatDirectPurchaseReceiptReason(string receiptNumber)
    {
        var normalized = DirectPurchaseReceiptNumbers.Normalize(receiptNumber);
        return $"{DirectPurchaseReceiptReason} {normalized}";
    }

    public static string? TryParseDirectPurchaseReceiptNumberFromReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return null;
        }

        var text = reason.Trim();
        const string prefix = DirectPurchaseReceiptReason + " ";
        if (!text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var candidate = text[prefix.Length..].Trim();
        // Notes may follow the document number.
        var token = candidate.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries)[0];
        try
        {
            return DirectPurchaseReceiptNumbers.Normalize(token);
        }
        catch (DomainException)
        {
            return null;
        }
    }

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
    public Guid? BranchId { get; }
    public InventoryLotId? InventoryLotId { get; }
    /// <summary>
    /// Purchase/acquisition cost per base inventory unit when known
    /// (opening stock, direct purchase, PO goods receipt). Null for corrections and non-purchase movements.
    /// </summary>
    public decimal? UnitCost { get; }

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
        Guid recordedBy,
        Guid? branchId = null,
        InventoryLotId? inventoryLotId = null,
        decimal? unitCost = null)
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
        BranchId = branchId;
        InventoryLotId = inventoryLotId;
        UnitCost = unitCost;
    }

    public static StockMovement OpeningStock(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        decimal? unitCost = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        var normalizedCost = NormalizeOpeningUnitCost(unitCost);
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
            actorId,
            unitCost: normalizedCost);
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
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
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
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
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
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        decimal? unitCost = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (saleId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleId,
                "SaleId cannot be an empty GUID.");
        }

        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        var normalizedCost = unitCost is null
            ? null
            : NormalizeAcquisitionUnitCost(unitCost, allowZero: true);
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
            actorId,
            unitCost: normalizedCost);
    }

    public static StockMovement CustomerOrderDeduction(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid customerOrderId,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (customerOrderId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderId,
                "CustomerOrderId cannot be an empty GUID.");
        }

        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.SaleDeduction,
            -absolute,
            CustomerOrderFulfillmentReason,
            StockMovementSourceType.CustomerOrder,
            customerOrderId,
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
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (saleId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleId,
                "SaleId cannot be an empty GUID.");
        }

        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
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
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        decimal? unitCost = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (goodsReceiptId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGoodsReceiptId,
                "GoodsReceiptId cannot be an empty GUID.");
        }

        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        var normalizedCost = NormalizeAcquisitionUnitCost(unitCost, allowZero: true);
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
            actorId,
            unitCost: normalizedCost);
    }

    public static StockMovement DirectPurchaseReceipt(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid directPurchaseReceiptId,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        decimal? unitCost = null,
        string? receiptNumber = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (directPurchaseReceiptId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDirectPurchaseReceiptId,
                "DirectPurchaseReceiptId cannot be an empty GUID.");
        }

        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        var normalizedCost = NormalizeAcquisitionUnitCost(unitCost, allowZero: false);
        var reason = string.IsNullOrWhiteSpace(receiptNumber)
            ? DirectPurchaseReceiptReason
            : FormatDirectPurchaseReceiptReason(receiptNumber);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.DirectPurchaseReceipt,
            absolute,
            reason,
            StockMovementSourceType.DirectPurchase,
            directPurchaseReceiptId,
            utcNow,
            actorId,
            unitCost: normalizedCost);
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
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (stockCountId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockCountId,
                "StockCountId cannot be an empty GUID.");
        }

        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
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
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (stockCountId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockCountId,
                "StockCountId cannot be an empty GUID.");
        }

        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
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

    public static StockMovement SaleReturnRestock(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid saleReturnId,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (saleReturnId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleReturnId,
                "SaleReturnId cannot be an empty GUID.");
        }

        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.SaleReturnRestock,
            absolute,
            SaleReturnRestockReason,
            StockMovementSourceType.SaleReturn,
            saleReturnId,
            utcNow,
            actorId);
    }

    public static StockMovement TransferOut(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        PosBranchId branchId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid transferId,
        string transferNumber,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        decimal? unitCost = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        EnsureTransferId(transferId);
        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        var normalizedCost = unitCost is null
            ? null
            : NormalizeAcquisitionUnitCost(unitCost, allowZero: true);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.TransferOut,
            -absolute,
            TransferReason(TransferOutReasonPrefix, transferNumber),
            StockMovementSourceType.InventoryTransfer,
            transferId,
            utcNow,
            actorId,
            branchId.Value,
            unitCost: normalizedCost);
    }

    public static StockMovement TransferIn(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        PosBranchId branchId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid transferId,
        string transferNumber,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        decimal? unitCost = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        EnsureTransferId(transferId);
        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        var normalizedCost = unitCost is null
            ? null
            : NormalizeAcquisitionUnitCost(unitCost, allowZero: true);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.TransferIn,
            absolute,
            TransferReason(TransferInReasonPrefix, transferNumber),
            StockMovementSourceType.InventoryTransfer,
            transferId,
            utcNow,
            actorId,
            branchId.Value,
            unitCost: normalizedCost);
    }

    public static StockMovement TransferCancelRestore(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        PosBranchId branchId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid transferId,
        string transferNumber,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        EnsureTransferId(transferId);
        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.TransferCancelRestore,
            absolute,
            TransferReason(TransferCancelRestoreReasonPrefix, transferNumber),
            StockMovementSourceType.InventoryTransfer,
            transferId,
            utcNow,
            actorId,
            branchId.Value);
    }

    public static StockMovement StockUse(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid stockUseId,
        Guid actorId,
        DateTimeOffset utcNow,
        string? reason = null,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        Guid? branchId = null,
        decimal? unitCost = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        EnsureStockUseId(stockUseId);
        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        var movementReason = string.IsNullOrWhiteSpace(reason)
            ? StockUseConsumptionReason
            : NormalizeOptionalReason(reason);
        // Optional acquisition cost snapshot only — never invent from selling price.
        var normalizedCost = unitCost is null
            ? null
            : NormalizeAcquisitionUnitCost(unitCost, allowZero: false);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.StockUse,
            -absolute,
            movementReason,
            StockMovementSourceType.StockUse,
            stockUseId,
            utcNow,
            actorId,
            branchId,
            unitCost: normalizedCost);
    }

    public static StockMovement StockUseVoidRestoration(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid stockUseId,
        Guid actorId,
        DateTimeOffset utcNow,
        string? reason = null,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        Guid? branchId = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        EnsureStockUseId(stockUseId);
        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        var restoredReason = string.IsNullOrWhiteSpace(reason)
            ? StockUseVoidRestorationReason
            : NormalizeOptionalReason(reason);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.StockUseVoidRestoration,
            absolute,
            restoredReason,
            StockMovementSourceType.StockUse,
            stockUseId,
            utcNow,
            actorId,
            branchId);
    }

    public static StockMovement ProductionMaterialConsumption(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid productionRunId,
        Guid actorId,
        DateTimeOffset utcNow,
        string? reason = null,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        Guid? branchId = null,
        decimal? unitCost = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        EnsureProductionRunId(productionRunId);
        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        var movementReason = string.IsNullOrWhiteSpace(reason)
            ? ProductionMaterialConsumptionReason
            : NormalizeOptionalReason(reason);
        var normalizedCost = unitCost is null
            ? null
            : NormalizeAcquisitionUnitCost(unitCost, allowZero: false);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.ProductionMaterialConsumption,
            -absolute,
            movementReason,
            StockMovementSourceType.Production,
            productionRunId,
            utcNow,
            actorId,
            branchId,
            unitCost: normalizedCost);
    }

    public static StockMovement ProductionMaterialRestoration(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid productionRunId,
        Guid actorId,
        DateTimeOffset utcNow,
        string? reason = null,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        Guid? branchId = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        EnsureProductionRunId(productionRunId);
        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        var restoredReason = string.IsNullOrWhiteSpace(reason)
            ? ProductionMaterialRestorationReason
            : NormalizeOptionalReason(reason);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.ProductionMaterialRestoration,
            absolute,
            restoredReason,
            StockMovementSourceType.Production,
            productionRunId,
            utcNow,
            actorId,
            branchId);
    }

    public static StockMovement ProductionOutput(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid productionRunId,
        Guid actorId,
        DateTimeOffset utcNow,
        string? reason = null,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        Guid? branchId = null,
        decimal? unitCost = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        EnsureProductionRunId(productionRunId);
        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        var movementReason = string.IsNullOrWhiteSpace(reason)
            ? ProductionOutputReason
            : NormalizeOptionalReason(reason);
        // MATERIAL_ONLY complete cost may set UnitCost; never invent from SellingPrice.
        var normalizedCost = unitCost is null
            ? null
            : NormalizeAcquisitionUnitCost(unitCost, allowZero: false);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.ProductionOutput,
            absolute,
            movementReason,
            StockMovementSourceType.Production,
            productionRunId,
            utcNow,
            actorId,
            branchId,
            unitCost: normalizedCost);
    }

    public static StockMovement ProductionOutputReversal(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid productionRunId,
        Guid actorId,
        DateTimeOffset utcNow,
        string? reason = null,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        Guid? branchId = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        EnsureProductionRunId(productionRunId);
        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        var reversedReason = string.IsNullOrWhiteSpace(reason)
            ? ProductionOutputReversalReason
            : NormalizeOptionalReason(reason);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.ProductionOutputReversal,
            -absolute,
            reversedReason,
            StockMovementSourceType.Production,
            productionRunId,
            utcNow,
            actorId,
            branchId);
    }

    public static StockMovement WasteLoss(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid wasteLossId,
        Guid actorId,
        DateTimeOffset utcNow,
        string? reason = null,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        Guid? branchId = null,
        decimal? unitCost = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        EnsureWasteLossId(wasteLossId);
        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        var movementReason = string.IsNullOrWhiteSpace(reason)
            ? WasteLossConsumptionReason
            : NormalizeOptionalReason(reason);
        var normalizedCost = unitCost is null
            ? null
            : NormalizeAcquisitionUnitCost(unitCost, allowZero: false);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.WasteLoss,
            -absolute,
            movementReason,
            StockMovementSourceType.WasteLoss,
            wasteLossId,
            utcNow,
            actorId,
            branchId,
            unitCost: normalizedCost);
    }

    public static StockMovement WasteLossVoidRestoration(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid wasteLossId,
        Guid actorId,
        DateTimeOffset utcNow,
        string? reason = null,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        Guid? branchId = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        EnsureWasteLossId(wasteLossId);
        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        var restoredReason = string.IsNullOrWhiteSpace(reason)
            ? WasteLossVoidRestorationReason
            : NormalizeOptionalReason(reason);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.WasteLossVoidRestoration,
            absolute,
            restoredReason,
            StockMovementSourceType.WasteLoss,
            wasteLossId,
            utcNow,
            actorId,
            branchId);
    }

    public static StockMovement PurchaseReceiptReversal(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid goodsReceiptId,
        Guid actorId,
        DateTimeOffset utcNow,
        string? reason = null,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        Guid? branchId = null,
        decimal? unitCost = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (goodsReceiptId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGoodsReceiptId,
                "GoodsReceiptId cannot be an empty GUID.");
        }

        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        var reversedReason = string.IsNullOrWhiteSpace(reason)
            ? PurchaseReceiptReversalReason
            : NormalizeOptionalReason(reason);
        var normalizedCost = unitCost is null
            ? null
            : NormalizeAcquisitionUnitCost(unitCost, allowZero: true);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.PurchaseReceiptReversal,
            -absolute,
            reversedReason,
            StockMovementSourceType.PurchaseReceipt,
            goodsReceiptId,
            utcNow,
            actorId,
            branchId,
            unitCost: normalizedCost);
    }

    public static StockMovement DirectPurchaseReceiptReversal(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid directPurchaseReceiptId,
        Guid actorId,
        DateTimeOffset utcNow,
        string? reason = null,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        Guid? branchId = null,
        decimal? unitCost = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (directPurchaseReceiptId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDirectPurchaseReceiptId,
                "DirectPurchaseReceiptId cannot be an empty GUID.");
        }

        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        var reversedReason = string.IsNullOrWhiteSpace(reason)
            ? DirectPurchaseReceiptReversalReason
            : NormalizeOptionalReason(reason);
        var normalizedCost = unitCost is null
            ? null
            : NormalizeAcquisitionUnitCost(unitCost, allowZero: false);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.DirectPurchaseReceiptReversal,
            -absolute,
            reversedReason,
            StockMovementSourceType.DirectPurchase,
            directPurchaseReceiptId,
            utcNow,
            actorId,
            branchId,
            unitCost: normalizedCost);
    }

    public static StockMovement ConnectedPurchaseFulfillment(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid connectedPurchaseOrderId,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        Guid? branchId = null,
        Guid? fulfillmentSourceId = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        EnsureConnectedPurchaseOrderId(connectedPurchaseOrderId);
        if (fulfillmentSourceId is Guid waveSource && waveSource == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockMovementId,
                "Fulfillment source id must not be empty.");
        }

        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.ConnectedPurchaseFulfillment,
            -absolute,
            ConnectedPurchaseFulfillmentReason,
            StockMovementSourceType.ConnectedPurchaseOrder,
            fulfillmentSourceId ?? connectedPurchaseOrderId,
            utcNow,
            actorId,
            branchId);
    }

    /// <summary>
    /// Compensating +qty seller movement for FoundAtSeller / NeverShipped receiving-issue resolutions.
    /// <paramref name="receivingIssueLineId"/> is the idempotency SourceId; original fulfillment wave is in the reason.
    /// </summary>
    public static StockMovement ConnectedPurchaseFulfillmentReconciliation(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid receivingIssueLineId,
        Guid originalFulfillmentSourceId,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        Guid? branchId = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (receivingIssueLineId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReceivingIssueLineId,
                "Receiving issue line id cannot be an empty GUID.");
        }

        if (originalFulfillmentSourceId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReceivingIssueFulfillmentSource,
                "Original fulfillment source id cannot be an empty GUID.");
        }

        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        var reason =
            $"{ConnectedPurchaseFulfillmentReconciliationReason} (fulfillmentSource={originalFulfillmentSourceId:D})";
        if (reason.Length > ReasonMaxLength)
        {
            reason = reason[..ReasonMaxLength];
        }

        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.ConnectedPurchaseFulfillmentReconciliation,
            absolute,
            reason,
            StockMovementSourceType.ConnectedPurchaseOrder,
            receivingIssueLineId,
            utcNow,
            actorId,
            branchId);
    }

    public static StockMovement SaleReturnWriteOff(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid saleReturnId,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (saleReturnId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleReturnId,
                "SaleReturnId cannot be an empty GUID.");
        }

        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            StockMovementType.SaleReturnWriteOff,
            -absolute,
            SaleReturnWriteOffReason,
            StockMovementSourceType.SaleReturn,
            saleReturnId,
            utcNow,
            actorId);
    }

    /// <summary>
    /// Connected-PO return movement. Dispatch decreases buyer on-hand; restock increases seller on-hand;
    /// write-off is an audit-only decrease for the damaged portion (seller on-hand is never increased for it).
    /// </summary>
    public static StockMovement ConnectedPoReturn(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        StockMovementType movementType,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid returnBatchId,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        Guid? branchId = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (returnBatchId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidReturnBatchId,
                "ReturnBatchId cannot be an empty GUID.");
        }

        var (reason, sign) = movementType switch
        {
            StockMovementType.ConnectedPoReturnDispatch => (ConnectedPoReturnDispatchReason, -1m),
            StockMovementType.ConnectedPoReturnRestock => (ConnectedPoReturnRestockReason, 1m),
            StockMovementType.ConnectedPoReturnWriteOff => (ConnectedPoReturnWriteOffReason, -1m),
            _ => throw new DomainException(
                DomainErrorCodes.InvalidInventoryMovementType,
                "Movement type is not a connected purchase-order return movement.")
        };

        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            movementType,
            sign * absolute,
            reason,
            StockMovementSourceType.ReturnBatch,
            returnBatchId,
            utcNow,
            actorId,
            branchId);
    }

    public StockMovement WithLot(InventoryLotId lotId) =>
        new(
            Id,
            OrganizationId,
            ProductId,
            InventoryAccountId,
            MovementType,
            QuantityEffect,
            Reason,
            SourceType,
            SourceId,
            RecordedAtUtc,
            RecordedBy,
            BranchId,
            lotId,
            UnitCost);

    /// <summary>
    /// Transfer damage custody ledger movement. Quantity effect is signed for audit;
    /// callers decide whether org <see cref="InventoryAccount"/> sellable is updated.
    /// </summary>
    public static StockMovement TransferDamageCustody(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        PosBranchId branchId,
        StockMovementType movementType,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid custodyOrReceiptLineId,
        string transferNumber,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        string? decisionDetail = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (custodyOrReceiptLineId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferDamageCustodyId,
                "Damage custody / receipt line source id must be a non-empty GUID.");
        }

        var (reasonPrefix, sign) = movementType switch
        {
            StockMovementType.TransferDamageHold => (TransferDamageHoldReasonPrefix, 1m),
            StockMovementType.TransferDamageRecovery => (TransferDamageRecoveryReasonPrefix, 1m),
            StockMovementType.TransferDamageReturnOut => (TransferDamageReturnOutReasonPrefix, -1m),
            StockMovementType.TransferDamageReturnIn => (TransferDamageReturnInReasonPrefix, 1m),
            StockMovementType.TransferDamageWriteOff => (TransferDamageWriteOffReasonPrefix, -1m),
            _ => throw new DomainException(
                DomainErrorCodes.InvalidInventoryMovementType,
                "Movement type is not a transfer damage custody movement.")
        };

        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        var reason = TransferReason(reasonPrefix, transferNumber);
        if (!string.IsNullOrWhiteSpace(decisionDetail))
        {
            reason = $"{reason} · {decisionDetail.Trim()}";
        }

        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            movementType,
            sign * absolute,
            reason,
            StockMovementSourceType.InventoryTransfer,
            custodyOrReceiptLineId,
            utcNow,
            actorId,
            branchId.Value);
    }

    /// <summary>
    /// Transfer exception ("Other") custody ledger movement. Quantity effect is signed for audit;
    /// callers decide whether org <see cref="InventoryAccount"/> sellable is updated.
    /// </summary>
    public static StockMovement TransferExceptionCustody(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        InventoryAccountId inventoryAccountId,
        PosBranchId branchId,
        StockMovementType movementType,
        decimal quantity,
        UnitOfMeasure unitOfMeasure,
        Guid custodyOrReceiptLineId,
        string transferNumber,
        Guid actorId,
        DateTimeOffset utcNow,
        StockMovementId? id = null,
        SellingMode sellingMode = SellingMode.PerItem,
        string? decisionDetail = null)
    {
        EnsureUtc(utcNow);
        EnsureActor(actorId);
        if (custodyOrReceiptLineId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferExceptionCustodyId,
                "Exception custody / receipt line source id must be a non-empty GUID.");
        }

        var (reasonPrefix, sign) = movementType switch
        {
            StockMovementType.TransferExceptionHold => (TransferExceptionHoldReasonPrefix, 1m),
            StockMovementType.TransferExceptionExpectedRestore => (TransferExceptionExpectedRestoreReasonPrefix, 1m),
            StockMovementType.TransferExceptionReturnIn => (TransferExceptionReturnInReasonPrefix, 1m),
            StockMovementType.TransferExceptionReturnRestock => (TransferExceptionReturnRestockReasonPrefix, 1m),
            StockMovementType.TransferExceptionRecovery => (TransferExceptionRecoveryReasonPrefix, 1m),
            StockMovementType.TransferExceptionActualOut => (TransferExceptionActualOutReasonPrefix, -1m),
            StockMovementType.TransferExceptionReturnOut => (TransferExceptionReturnOutReasonPrefix, -1m),
            StockMovementType.TransferExceptionWriteOff => (TransferExceptionWriteOffReasonPrefix, -1m),
            _ => throw new DomainException(
                DomainErrorCodes.InvalidInventoryMovementType,
                "Movement type is not a transfer exception custody movement.")
        };

        var absolute = SaleLine.NormalizeQuantity(quantity, unitOfMeasure, sellingMode);
        var reason = TransferReason(reasonPrefix, transferNumber);
        if (!string.IsNullOrWhiteSpace(decisionDetail))
        {
            reason = $"{reason} · {decisionDetail.Trim()}";
        }

        return new StockMovement(
            id ?? StockMovementId.New(),
            organizationId,
            productId,
            inventoryAccountId,
            movementType,
            sign * absolute,
            reason,
            StockMovementSourceType.InventoryTransfer,
            custodyOrReceiptLineId,
            utcNow,
            actorId,
            branchId.Value);
    }

    public StockMovement WithBranch(Guid? branchId) =>
        new(
            Id,
            OrganizationId,
            ProductId,
            InventoryAccountId,
            MovementType,
            QuantityEffect,
            Reason,
            SourceType,
            SourceId,
            RecordedAtUtc,
            RecordedBy,
            branchId,
            InventoryLotId,
            UnitCost);

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
        Guid recordedBy,
        Guid? branchId = null,
        InventoryLotId? inventoryLotId = null,
        decimal? unitCost = null) =>
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
            recordedBy,
            branchId,
            inventoryLotId,
            unitCost);

    /// <summary>Opening stock requires a positive unit purchase cost when supplied.</summary>
    public static decimal? NormalizeOpeningUnitCost(decimal? unitCost) =>
        NormalizeAcquisitionUnitCost(unitCost, allowZero: false);

    /// <summary>
    /// Normalizes acquisition cost per base inventory unit.
    /// When <paramref name="allowZero"/> is false (opening / direct buy), cost must be &gt; 0.
    /// When true (PO receipt), zero is preserved as a known free-goods cost; null means unknown/not set.
    /// </summary>
    public static decimal? NormalizeAcquisitionUnitCost(decimal? unitCost, bool allowZero = false)
    {
        if (unitCost is null)
        {
            return null;
        }

        if (allowZero)
        {
            if (unitCost.Value < 0m)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPurchaseUnitCost,
                    "Unit purchase cost cannot be negative.");
            }
        }
        else if (unitCost.Value <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryOpeningUnitCost,
                "Unit cost must be greater than zero.");
        }

        if (unitCost.Value > PurchaseOrderLine.MaxUnitPurchaseCost)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryOpeningUnitCost,
                "Unit cost is too large.");
        }

        return SaleMoney.RoundMoney(unitCost.Value);
    }

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

    private static void EnsureTransferId(Guid transferId)
    {
        if (transferId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferId,
                "TransferId cannot be an empty GUID.");
        }
    }

    private static void EnsureStockUseId(Guid stockUseId)
    {
        if (stockUseId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockUseId,
                "StockUseId cannot be an empty GUID.");
        }
    }

    private static void EnsureWasteLossId(Guid wasteLossId)
    {
        if (wasteLossId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWasteLossId,
                "WasteLossId cannot be an empty GUID.");
        }
    }

    private static void EnsureConnectedPurchaseOrderId(Guid connectedPurchaseOrderId)
    {
        if (connectedPurchaseOrderId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPurchaseOrderId,
                "ConnectedPurchaseOrderId cannot be an empty GUID.");
        }
    }

    private static void EnsureProductionRunId(Guid productionRunId)
    {
        if (productionRunId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionRunId,
                "ProductionRunId cannot be an empty GUID.");
        }
    }

    private static string TransferReason(string prefix, string transferNumber)
    {
        var number = InventoryTransferNumbers.Normalize(transferNumber);
        return $"{prefix} {number}";
    }
}
