using ExItS.PinoyBusinessPOS.Domain.Catalog;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>Validated inputs used to build a <see cref="StockUseLine"/> during create.</summary>
public sealed record StockUseLineDraft(
    CatalogProductId ProductId,
    decimal QuantityEntered,
    decimal MultiplierToBase,
    string NameSnapshot,
    string UnitLabelSnapshot,
    ProductUnitId? ProductUnitId = null,
    decimal? UnitCostSnapshot = null);
