using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Immutable line on a stock-use document. Quantities are snapshotted at create;
/// inventory movements use <see cref="BaseQuantity"/>.
/// </summary>
public sealed class StockUseLine
{
    public const int UnitLabelMaxLength = 64;

    public StockUseLineId Id { get; }
    public StockUseId StockUseId { get; }
    public PosOrganizationId OrganizationId { get; }
    public CatalogProductId ProductId { get; }
    public ProductUnitId? ProductUnitId { get; }
    public int LineNumber { get; }
    public decimal QuantityEntered { get; }
    public decimal MultiplierToBase { get; }
    public decimal BaseQuantity { get; }
    public string NameSnapshot { get; }
    public string UnitLabelSnapshot { get; }
    /// <summary>
    /// Optional acquisition cost per base unit. May be null when no prior acquisition cost
    /// is known (STOCK_USE_COST_SOURCE=DEFERRED). Never derived from selling price.
    /// </summary>
    public decimal? UnitCostSnapshot { get; }
    public decimal? LineCostSnapshot { get; }
    public Guid? InventoryMovementId { get; private set; }

    private StockUseLine(
        StockUseLineId id,
        StockUseId stockUseId,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        ProductUnitId? productUnitId,
        int lineNumber,
        decimal quantityEntered,
        decimal multiplierToBase,
        decimal baseQuantity,
        string nameSnapshot,
        string unitLabelSnapshot,
        decimal? unitCostSnapshot,
        decimal? lineCostSnapshot,
        Guid? inventoryMovementId)
    {
        Id = id;
        StockUseId = stockUseId;
        OrganizationId = organizationId;
        ProductId = productId;
        ProductUnitId = productUnitId;
        LineNumber = lineNumber;
        QuantityEntered = quantityEntered;
        MultiplierToBase = multiplierToBase;
        BaseQuantity = baseQuantity;
        NameSnapshot = nameSnapshot;
        UnitLabelSnapshot = unitLabelSnapshot;
        UnitCostSnapshot = unitCostSnapshot;
        LineCostSnapshot = lineCostSnapshot;
        InventoryMovementId = inventoryMovementId;
    }

    internal static StockUseLine Create(
        StockUseId stockUseId,
        PosOrganizationId organizationId,
        int lineNumber,
        StockUseLineDraft draft,
        StockUseLineId? id = null)
    {
        if (draft.QuantityEntered <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockUseQuantity,
                "Stock use quantity must be greater than zero.");
        }

        ProductUnitConversion.EnsureValidMultiplier(draft.MultiplierToBase);
        if (!SaleMoney.HasAtMostDecimals(draft.QuantityEntered, SaleMoney.MeasuredQuantityDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockUseQuantity,
                $"Stock use quantity must have at most {SaleMoney.MeasuredQuantityDecimals} decimal places.");
        }

        var baseQuantity = ProductUnitConversion.ToBaseQuantity(draft.QuantityEntered, draft.MultiplierToBase);
        if (!SaleMoney.HasAtMostDecimals(baseQuantity, SaleMoney.MeasuredQuantityDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockUseQuantity,
                $"Converted base quantity must have at most {SaleMoney.MeasuredQuantityDecimals} decimal places.");
        }

        var unitCost = NormalizeOptionalUnitCost(draft.UnitCostSnapshot);
        decimal? lineCost = unitCost is null
            ? null
            : SaleMoney.RoundMoney(unitCost.Value * baseQuantity);

        return new StockUseLine(
            id ?? StockUseLineId.New(),
            stockUseId,
            organizationId,
            draft.ProductId,
            draft.ProductUnitId,
            lineNumber,
            draft.QuantityEntered,
            draft.MultiplierToBase,
            baseQuantity,
            NormalizeName(draft.NameSnapshot),
            NormalizeUnitLabel(draft.UnitLabelSnapshot),
            unitCost,
            lineCost,
            inventoryMovementId: null);
    }

    public void AttachInventoryMovement(StockMovementId movementId)
    {
        if (InventoryMovementId is not null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockUseLine,
                "Inventory movement is already linked to this stock use line.");
        }

        InventoryMovementId = movementId.Value;
    }

    public static StockUseLine Rehydrate(
        StockUseLineId id,
        StockUseId stockUseId,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        ProductUnitId? productUnitId,
        int lineNumber,
        decimal quantityEntered,
        decimal multiplierToBase,
        decimal baseQuantity,
        string nameSnapshot,
        string unitLabelSnapshot,
        decimal? unitCostSnapshot,
        decimal? lineCostSnapshot,
        Guid? inventoryMovementId) =>
        new(
            id,
            stockUseId,
            organizationId,
            productId,
            productUnitId,
            lineNumber,
            quantityEntered,
            multiplierToBase,
            baseQuantity,
            nameSnapshot,
            unitLabelSnapshot,
            unitCostSnapshot,
            lineCostSnapshot,
            inventoryMovementId);

    private static decimal? NormalizeOptionalUnitCost(decimal? unitCost)
    {
        if (unitCost is null)
        {
            return null;
        }

        if (unitCost.Value <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockUseUnitCost,
                "Unit cost snapshot must be greater than zero when supplied.");
        }

        if (unitCost.Value > PurchaseOrderLine.MaxUnitPurchaseCost)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockUseUnitCost,
                "Unit cost snapshot is too large.");
        }

        return SaleMoney.RoundMoney(unitCost.Value);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockUseLine,
                "Product name snapshot is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > PurchaseOrderLine.NameSnapshotMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockUseLine,
                $"Product name snapshot must be at most {PurchaseOrderLine.NameSnapshotMaxLength} characters.");
        }

        return trimmed;
    }

    private static string NormalizeUnitLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockUseLine,
                "Unit label snapshot is required.");
        }

        var trimmed = label.Trim();
        if (trimmed.Length > UnitLabelMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockUseLine,
                $"Unit label snapshot must be at most {UnitLabelMaxLength} characters.");
        }

        return trimmed;
    }
}
