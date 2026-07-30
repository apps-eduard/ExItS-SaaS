using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public sealed record StockCountLineDraft(CatalogProductId ProductId, decimal? CountedQuantity);

/// <summary>One product line on a stock count. Variance is derived when counted quantity is set.</summary>
public sealed class StockCountLine
{
    public StockCountLineId Id { get; }
    public StockCountId StockCountId { get; }
    public PosOrganizationId OrganizationId { get; }
    public CatalogProductId ProductId { get; }
    public int LineNumber { get; }
    public decimal? SystemOnHandSnapshot { get; private set; }
    public decimal? CountedQuantity { get; private set; }

    public decimal? Variance =>
        SystemOnHandSnapshot is not null && CountedQuantity is not null
            ? CountedQuantity.Value - SystemOnHandSnapshot.Value
            : null;

    private StockCountLine(
        StockCountLineId id,
        StockCountId stockCountId,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        int lineNumber,
        decimal? systemOnHandSnapshot,
        decimal? countedQuantity)
    {
        Id = id;
        StockCountId = stockCountId;
        OrganizationId = organizationId;
        ProductId = productId;
        LineNumber = lineNumber;
        SystemOnHandSnapshot = systemOnHandSnapshot;
        CountedQuantity = countedQuantity;
    }

    public static StockCountLine CreateDraft(
        StockCountId stockCountId,
        PosOrganizationId organizationId,
        int lineNumber,
        CatalogProductId productId,
        StockCountLineId? id = null) =>
        new(
            id ?? StockCountLineId.New(),
            stockCountId,
            organizationId,
            productId,
            lineNumber,
            systemOnHandSnapshot: null,
            countedQuantity: null);

    public void ApplySnapshot(decimal onHandQuantity) => SystemOnHandSnapshot = onHandQuantity;

    public void SetCountedQuantity(decimal quantity, UnitOfMeasure unitOfMeasure)
    {
        CountedQuantity = SaleLine.NormalizeQuantity(quantity, unitOfMeasure);
    }

    public static StockCountLine Rehydrate(
        StockCountLineId id,
        StockCountId stockCountId,
        PosOrganizationId organizationId,
        CatalogProductId productId,
        int lineNumber,
        decimal? systemOnHandSnapshot,
        decimal? countedQuantity) =>
        new(id, stockCountId, organizationId, productId, lineNumber, systemOnHandSnapshot, countedQuantity);
}
