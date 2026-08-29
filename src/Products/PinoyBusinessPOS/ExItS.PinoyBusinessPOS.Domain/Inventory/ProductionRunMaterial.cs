using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>Validated inputs used to build a <see cref="ProductionRunMaterial"/>.</summary>
public sealed record ProductionRunMaterialDraft(
    CatalogProductId MaterialProductId,
    decimal ExpectedQuantityEntered,
    decimal ActualQuantityEntered,
    decimal MultiplierToBase,
    string NameSnapshot,
    string UnitLabelSnapshot,
    ProductUnitId? ProductUnitId = null,
    decimal? UnitCostSnapshot = null);

/// <summary>
/// Immutable material line on a production run. Expected quantities are snapshotted from the
/// definition (scaled); Actual drives inventory consumption.
/// </summary>
public sealed class ProductionRunMaterial
{
    public const int UnitLabelMaxLength = 64;

    public ProductionRunMaterialId Id { get; }
    public ProductionRunId ProductionRunId { get; }
    public PosOrganizationId OrganizationId { get; }
    public CatalogProductId MaterialProductId { get; }
    public ProductUnitId? ProductUnitId { get; }
    public int LineNumber { get; }
    public decimal ExpectedQuantityEntered { get; }
    public decimal ActualQuantityEntered { get; }
    public decimal MultiplierToBase { get; }
    public decimal ExpectedBaseQuantity { get; }
    public decimal ActualBaseQuantity { get; }
    public string NameSnapshot { get; }
    public string UnitLabelSnapshot { get; }
    /// <summary>
    /// Optional acquisition cost per base unit (MATERIAL_ONLY). Never from SellingPrice.
    /// </summary>
    public decimal? UnitCostSnapshot { get; }
    public decimal? LineCostSnapshot { get; }
    public Guid? InventoryMovementId { get; private set; }

    private ProductionRunMaterial(
        ProductionRunMaterialId id,
        ProductionRunId productionRunId,
        PosOrganizationId organizationId,
        CatalogProductId materialProductId,
        ProductUnitId? productUnitId,
        int lineNumber,
        decimal expectedQuantityEntered,
        decimal actualQuantityEntered,
        decimal multiplierToBase,
        decimal expectedBaseQuantity,
        decimal actualBaseQuantity,
        string nameSnapshot,
        string unitLabelSnapshot,
        decimal? unitCostSnapshot,
        decimal? lineCostSnapshot,
        Guid? inventoryMovementId)
    {
        Id = id;
        ProductionRunId = productionRunId;
        OrganizationId = organizationId;
        MaterialProductId = materialProductId;
        ProductUnitId = productUnitId;
        LineNumber = lineNumber;
        ExpectedQuantityEntered = expectedQuantityEntered;
        ActualQuantityEntered = actualQuantityEntered;
        MultiplierToBase = multiplierToBase;
        ExpectedBaseQuantity = expectedBaseQuantity;
        ActualBaseQuantity = actualBaseQuantity;
        NameSnapshot = nameSnapshot;
        UnitLabelSnapshot = unitLabelSnapshot;
        UnitCostSnapshot = unitCostSnapshot;
        LineCostSnapshot = lineCostSnapshot;
        InventoryMovementId = inventoryMovementId;
    }

    internal static ProductionRunMaterial Create(
        ProductionRunId runId,
        PosOrganizationId organizationId,
        int lineNumber,
        ProductionRunMaterialDraft draft,
        ProductionRunMaterialId? id = null)
    {
        if (draft.ExpectedQuantityEntered < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionQuantity,
                "Expected material quantity cannot be negative.");
        }

        if (draft.ActualQuantityEntered <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionQuantity,
                "Actual material quantity must be greater than zero.");
        }

        ProductUnitConversion.EnsureValidMultiplier(draft.MultiplierToBase);
        EnsureQuantityDecimals(draft.ExpectedQuantityEntered, "Expected material quantity");
        EnsureQuantityDecimals(draft.ActualQuantityEntered, "Actual material quantity");

        var expectedBase = ProductUnitConversion.ToBaseQuantity(draft.ExpectedQuantityEntered, draft.MultiplierToBase);
        var actualBase = ProductUnitConversion.ToBaseQuantity(draft.ActualQuantityEntered, draft.MultiplierToBase);
        EnsureQuantityDecimals(expectedBase, "Expected material base quantity");
        EnsureQuantityDecimals(actualBase, "Actual material base quantity");

        var unitCost = NormalizeOptionalUnitCost(draft.UnitCostSnapshot);
        decimal? lineCost = unitCost is null
            ? null
            : SaleMoney.RoundMoney(unitCost.Value * actualBase);

        return new ProductionRunMaterial(
            id ?? ProductionRunMaterialId.New(),
            runId,
            organizationId,
            draft.MaterialProductId,
            draft.ProductUnitId,
            lineNumber,
            draft.ExpectedQuantityEntered,
            draft.ActualQuantityEntered,
            draft.MultiplierToBase,
            expectedBase,
            actualBase,
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
                DomainErrorCodes.InvalidProductionRunMaterial,
                "Inventory movement is already linked to this production run material.");
        }

        InventoryMovementId = movementId.Value;
    }

    public static ProductionRunMaterial Rehydrate(
        ProductionRunMaterialId id,
        ProductionRunId productionRunId,
        PosOrganizationId organizationId,
        CatalogProductId materialProductId,
        ProductUnitId? productUnitId,
        int lineNumber,
        decimal expectedQuantityEntered,
        decimal actualQuantityEntered,
        decimal multiplierToBase,
        decimal expectedBaseQuantity,
        decimal actualBaseQuantity,
        string nameSnapshot,
        string unitLabelSnapshot,
        decimal? unitCostSnapshot,
        decimal? lineCostSnapshot,
        Guid? inventoryMovementId) =>
        new(
            id,
            productionRunId,
            organizationId,
            materialProductId,
            productUnitId,
            lineNumber,
            expectedQuantityEntered,
            actualQuantityEntered,
            multiplierToBase,
            expectedBaseQuantity,
            actualBaseQuantity,
            nameSnapshot,
            unitLabelSnapshot,
            unitCostSnapshot,
            lineCostSnapshot,
            inventoryMovementId);

    private static void EnsureQuantityDecimals(decimal value, string label)
    {
        if (!SaleMoney.HasAtMostDecimals(value, SaleMoney.MeasuredQuantityDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionQuantity,
                $"{label} must have at most {SaleMoney.MeasuredQuantityDecimals} decimal places.");
        }
    }

    private static decimal? NormalizeOptionalUnitCost(decimal? unitCost)
    {
        if (unitCost is null)
        {
            return null;
        }

        if (unitCost.Value <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionUnitCost,
                "Unit cost snapshot must be greater than zero when supplied.");
        }

        if (unitCost.Value > PurchaseOrderLine.MaxUnitPurchaseCost)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionUnitCost,
                "Unit cost snapshot is too large.");
        }

        return SaleMoney.RoundMoney(unitCost.Value);
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionRunMaterial,
                "Product name snapshot is required.");
        }

        var trimmed = name.Trim();
        if (trimmed.Length > PurchaseOrderLine.NameSnapshotMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionRunMaterial,
                $"Product name snapshot must be at most {PurchaseOrderLine.NameSnapshotMaxLength} characters.");
        }

        return trimmed;
    }

    private static string NormalizeUnitLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionRunMaterial,
                "Unit label snapshot is required.");
        }

        var trimmed = label.Trim();
        if (trimmed.Length > UnitLabelMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionRunMaterial,
                $"Unit label snapshot must be at most {UnitLabelMaxLength} characters.");
        }

        return trimmed;
    }
}
