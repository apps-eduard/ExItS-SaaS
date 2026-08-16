using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>Validated line input for <see cref="DirectPurchaseReceipt.Create"/>.</summary>
public sealed record DirectPurchaseReceiptLineDraft(
    CatalogProductId ProductId,
    string ProductNameSnapshot,
    string? SkuSnapshot,
    UnitOfMeasure UnitOfMeasure,
    decimal Quantity,
    decimal UnitCost,
    SellingMode SellingMode = SellingMode.PerItem,
    DateOnly? ExpiryDate = null,
    string? LotNumber = null);
