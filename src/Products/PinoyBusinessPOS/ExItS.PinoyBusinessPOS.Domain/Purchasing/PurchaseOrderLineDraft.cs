using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

/// <summary>
/// Draft line input before submit.
/// Connected unlinked lines may omit <see cref="ProductId"/> and carry <see cref="SupplierProductId"/>
/// with supplier name/UOM snapshots until the buyer binds a local catalog product.
/// </summary>
public sealed record PurchaseOrderLineDraft(
    CatalogProductId? ProductId,
    decimal OrderedQty,
    decimal UnitPurchaseCost,
    string? LineNotes = null,
    ProductUnitId? PurchaseUnitId = null,
    string? PurchaseUnitNameSnapshot = null,
    decimal MultiplierToBaseSnapshot = 1m,
    CatalogProductId? SupplierProductId = null,
    string? NameSnapshot = null,
    UnitOfMeasure? UomSnapshot = null,
    string? SkuSnapshot = null);

/// <summary>Snapshot input used when freezing catalog values on submit.</summary>
public sealed record PurchaseOrderLineSnapshotInput(
    CatalogProductId? ProductId,
    string NameSnapshot,
    UnitOfMeasure UomSnapshot,
    decimal OrderedQty,
    decimal UnitPurchaseCost,
    string? LineNotes = null,
    SellingMode SellingMode = SellingMode.PerItem,
    ProductUnitId? PurchaseUnitId = null,
    string? PurchaseUnitNameSnapshot = null,
    decimal MultiplierToBaseSnapshot = 1m,
    CatalogProductId? SupplierProductId = null,
    string? SkuSnapshot = null);

/// <summary>Receive quantities for one PO line during goods receipt. Only GoodQty enters usable inventory.</summary>
public sealed record PurchaseOrderReceiveLineDraft(
    CatalogProductId ProductId,
    decimal ReceiveQty,
    SellingMode SellingMode = SellingMode.PerItem,
    decimal DamagedQty = 0m,
    decimal RejectedQty = 0m,
    decimal ShortClosedQty = 0m,
    ConnectedPoReceivingDiscrepancyKind DiscrepancyKind = ConnectedPoReceivingDiscrepancyKind.None,
    string? DiscrepancyNote = null,
    DateOnly? ExpiryDate = null,
    string? LotNumber = null);
