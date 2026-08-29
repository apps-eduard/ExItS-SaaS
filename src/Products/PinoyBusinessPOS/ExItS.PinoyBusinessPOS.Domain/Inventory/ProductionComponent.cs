using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>Validated inputs used to build a <see cref="ProductionComponent"/>.</summary>
public sealed record ProductionComponentDraft(
    CatalogProductId MaterialProductId,
    decimal QuantityEntered,
    decimal MultiplierToBase,
    ProductUnitId? ProductUnitId = null,
    int? SortOrder = null);

/// <summary>Material line on a production definition (recipe / BOM).</summary>
public sealed class ProductionComponent
{
    public ProductionComponentId Id { get; }
    public ProductionDefinitionId ProductionDefinitionId { get; }
    public PosOrganizationId OrganizationId { get; }
    public CatalogProductId MaterialProductId { get; }
    public ProductUnitId? ProductUnitId { get; }
    public int SortOrder { get; }
    public decimal QuantityEntered { get; }
    public decimal MultiplierToBase { get; }
    public decimal BaseQuantity { get; }

    private ProductionComponent(
        ProductionComponentId id,
        ProductionDefinitionId productionDefinitionId,
        PosOrganizationId organizationId,
        CatalogProductId materialProductId,
        ProductUnitId? productUnitId,
        int sortOrder,
        decimal quantityEntered,
        decimal multiplierToBase,
        decimal baseQuantity)
    {
        Id = id;
        ProductionDefinitionId = productionDefinitionId;
        OrganizationId = organizationId;
        MaterialProductId = materialProductId;
        ProductUnitId = productUnitId;
        SortOrder = sortOrder;
        QuantityEntered = quantityEntered;
        MultiplierToBase = multiplierToBase;
        BaseQuantity = baseQuantity;
    }

    internal static ProductionComponent Create(
        ProductionDefinitionId definitionId,
        PosOrganizationId organizationId,
        int sortOrder,
        ProductionComponentDraft draft,
        ProductionComponentId? id = null)
    {
        if (draft.QuantityEntered <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionQuantity,
                "Production component quantity must be greater than zero.");
        }

        ProductUnitConversion.EnsureValidMultiplier(draft.MultiplierToBase);
        if (!SaleMoney.HasAtMostDecimals(draft.QuantityEntered, SaleMoney.MeasuredQuantityDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionQuantity,
                $"Production component quantity must have at most {SaleMoney.MeasuredQuantityDecimals} decimal places.");
        }

        var baseQuantity = ProductUnitConversion.ToBaseQuantity(draft.QuantityEntered, draft.MultiplierToBase);
        if (!SaleMoney.HasAtMostDecimals(baseQuantity, SaleMoney.MeasuredQuantityDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductionQuantity,
                $"Converted component base quantity must have at most {SaleMoney.MeasuredQuantityDecimals} decimal places.");
        }

        return new ProductionComponent(
            id ?? ProductionComponentId.New(),
            definitionId,
            organizationId,
            draft.MaterialProductId,
            draft.ProductUnitId,
            draft.SortOrder ?? sortOrder,
            draft.QuantityEntered,
            draft.MultiplierToBase,
            baseQuantity);
    }

    public static ProductionComponent Rehydrate(
        ProductionComponentId id,
        ProductionDefinitionId productionDefinitionId,
        PosOrganizationId organizationId,
        CatalogProductId materialProductId,
        ProductUnitId? productUnitId,
        int sortOrder,
        decimal quantityEntered,
        decimal multiplierToBase,
        decimal baseQuantity) =>
        new(
            id,
            productionDefinitionId,
            organizationId,
            materialProductId,
            productUnitId,
            sortOrder,
            quantityEntered,
            multiplierToBase,
            baseQuantity);
}
