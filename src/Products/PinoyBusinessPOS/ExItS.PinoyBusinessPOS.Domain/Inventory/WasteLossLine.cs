using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Immutable line on a waste/loss document. Quantities are snapshotted at create;
/// inventory movements use <see cref="BaseQuantity"/>. Expiration-tracked products carry an
/// explicit <see cref="InventoryLotId"/> (ConsumeSpecific only — never FEFO).
/// </summary>
public sealed class WasteLossLine
{
    public const int UnitLabelMaxLength = 64;

    public WasteLossLineId Id { get; }
    public WasteLossId WasteLossId { get; }
    public PosOrganizationId OrganizationId { get; }
    public CatalogProductId ProductId { get; }
    public ProductUnitId? ProductUnitId { get; }
    public InventoryLotId? InventoryLotId { get; }
    public int LineNumber { get; }
    public decimal QuantityEntered { get; }
    public decimal MultiplierToBase { get; }
    public decimal BaseQuantity { get; }
    public string NameSnapshot { get; }
    public string UnitLabelSnapshot { get; }
    /// <summary>
    /// Optional acquisition cost per base unit from GetLatestAcquisitionUnitCostAsync only.
    /// Never derived from selling price.
    /// </summary>
    public decimal? UnitCostSnapshot { get; }
    public decimal? LineCostSnapshot { get; }
    public Guid? InventoryMovementId { get; private set; }

    private WasteLossLine(
        WasteLossLineId id,
        WasteLossId wasteLossId,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        ProductUnitId? productUnitId,
        InventoryLotId? inventoryLotId,
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
        WasteLossId = wasteLossId;
        OrganizationId = organizationId;
        ProductId = productId;
        ProductUnitId = productUnitId;
        InventoryLotId = inventoryLotId;
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

    internal static WasteLossLine Create(
        WasteLossId wasteLossId,
        PosOrganizationId organizationId,
        int lineNumber,
        WasteLossLineDraft draft,
        WasteLossLineId? id = null)
    {
        if (draft.QuantityEntered <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWasteLossQuantity,
                "Waste/loss quantity must be greater than zero.");
        }

        ProductUnitConversion.EnsureValidMultiplier(draft.MultiplierToBase);
        if (!SaleMoney.HasAtMostDecimals(draft.QuantityEntered, SaleMoney.MeasuredQuantityDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWasteLossQuantity,
                $"Waste/loss quantity must have at most {SaleMoney.MeasuredQuantityDecimals} decimal places.");
        }

        var baseQuantity = ProductUnitConversion.ToBaseQuantity(draft.QuantityEntered, draft.MultiplierToBase);
        if (!SaleMoney.HasAtMostDecimals(baseQuantity, SaleMoney.MeasuredQuantityDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWasteLossQuantity,
                $"Converted base quantity must have at most {SaleMoney.MeasuredQuantityDecimals} decimal places.");
        }

        var unitCost = NormalizeOptionalUnitCost(draft.UnitCostSnapshot);
        decimal? lineCost = unitCost is null
            ? null
            : SaleMoney.RoundMoney(unitCost.Value * baseQuantity);

        return new WasteLossLine(
            id ?? WasteLossLineId.New(),
            wasteLossId,
            organizationId,
            draft.ProductId,
            draft.ProductUnitId,
            draft.InventoryLotId,
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
                DomainErrorCodes.InvalidWasteLossLine,
                "Inventory movement is already linked to this waste/loss line.");
        }

        InventoryMovementId = movementId.Value;
    }

    public static WasteLossLine Rehydrate(
        WasteLossLineId id,
        WasteLossId wasteLossId,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        ProductUnitId? productUnitId,
        InventoryLotId? inventoryLotId,
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
            wasteLossId,
            organizationId,
            productId,
            productUnitId,
            inventoryLotId,
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
                DomainErrorCodes.InvalidWasteLossUnitCost,
                "Unit cost snapshot must be greater than zero when supplied.");
        }

        if (unitCost.Value > PurchaseOrderLine.MaxUnitPurchaseCost)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWasteLossUnitCost,
                "Unit cost snapshot is too large.");
        }

        return SaleMoney.RoundMoney(unitCost.Value);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWasteLossLine,
                "Product name snapshot is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > PurchaseOrderLine.NameSnapshotMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWasteLossLine,
                $"Product name snapshot must be at most {PurchaseOrderLine.NameSnapshotMaxLength} characters.");
        }

        return trimmed;
    }

    private static string NormalizeUnitLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWasteLossLine,
                "Unit label snapshot is required.");
        }

        var trimmed = label.Trim();
        if (trimmed.Length > UnitLabelMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidWasteLossLine,
                $"Unit label snapshot must be at most {UnitLabelMaxLength} characters.");
        }

        return trimmed;
    }
}
