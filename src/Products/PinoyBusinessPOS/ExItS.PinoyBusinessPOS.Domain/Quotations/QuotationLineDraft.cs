using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Quotations;

/// <summary>Draft line input before Issue. Catalog product only — no ad-hoc / free-text lines.</summary>
public sealed record QuotationLineDraft(
    CatalogProductId ProductId,
    decimal Quantity,
    decimal UnitPrice,
    decimal? DiscountAmount = null,
    string? NameSnapshot = null,
    string? SkuSnapshot = null,
    UnitOfMeasure? UomSnapshot = null);

/// <summary>Snapshot input used when freezing catalog values on Issue.</summary>
public sealed record QuotationLineSnapshotInput(
    CatalogProductId ProductId,
    string NameSnapshot,
    UnitOfMeasure UomSnapshot,
    decimal Quantity,
    decimal UnitPrice,
    string? SkuSnapshot = null,
    decimal? DiscountAmount = null);
