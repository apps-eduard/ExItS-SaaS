using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

/// <summary>Draft line input before submit. Product name and UOM are resolved at submit.</summary>
public sealed record PurchaseOrderLineDraft(
    CatalogProductId ProductId,
    decimal OrderedQty,
    decimal UnitPurchaseCost,
    string? LineNotes = null);

/// <summary>Snapshot input used when freezing catalog values on submit.</summary>
public sealed record PurchaseOrderLineSnapshotInput(
    CatalogProductId ProductId,
    string NameSnapshot,
    UnitOfMeasure UomSnapshot,
    decimal OrderedQty,
    decimal UnitPurchaseCost,
    string? LineNotes = null);

/// <summary>Receive quantity for one PO line during goods receipt.</summary>
public sealed record PurchaseOrderReceiveLineDraft(
    CatalogProductId ProductId,
    decimal ReceiveQty);
