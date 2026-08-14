namespace ExItS.PinoyBusinessPOS.Application.Sales;

public sealed record PosSaleLineDto(
    Guid SaleLineId,
    Guid ProductId,
    int LineNumber,
    string Name,
    string? Sku,
    string? Barcode,
    string UnitOfMeasure,
    string SellingMode,
    decimal UnitPrice,
    decimal Quantity,
    decimal LineTotal);

public sealed record PosSaleDto(
    Guid SaleId,
    Guid OrganizationId,
    string SaleNumber,
    string Status,
    string PaymentMethod,
    decimal Subtotal,
    decimal Total,
    decimal TaxAmount,
    decimal? AmountTendered,
    decimal? ChangeAmount,
    string? GCashReference,
    DateTimeOffset RecordedAtUtc,
    Guid RecordedBy,
    DateTimeOffset? VoidedAtUtc,
    Guid? VoidedBy,
    string? VoidReason,
    DateTimeOffset UpdatedAtUtc,
    List<PosSaleLineDto> Lines,
    Guid? CustomerId = null,
    Guid? LinkedCreditEntryId = null,
    string? CustomerDisplayName = null,
    DateOnly? LinkedCreditDueDate = null,
    decimal? CustomerOutstandingAfter = null,
    Guid? ShiftId = null,
    string? ShiftNumber = null,
    Guid? RegisterId = null,
    string? RegisterCode = null,
    string? RegisterName = null,
    string? StoreDisplayName = null,
    string? CurrencyCode = null,
    string? TaxPricingMode = null,
    string? ReceiptHeader = null,
    string? ReceiptFooter = null,
    string? BusinessAddress = null,
    string? ContactPhone = null,
    string BuyerPartyKind = "WalkIn",
    string? BuyerDisplayNameSnapshot = null,
    string? BuyerPersonalPublicUserId = null,
    Guid? BuyerOrganizationId = null,
    string? BuyerPublicOrganizationId = null);

/// <summary>
/// One requested checkout line.
/// Online carts may send ProductId + Quantity only; the server then prices from the live catalog.
/// Offline cash sync (payload_version ≥ 2) must also send immutable snapshots so the server
/// validates arithmetic without replacing UnitPrice / UOM / SellingMode from the live catalog.
/// </summary>
public sealed record CheckoutSaleLineRequest(
    Guid ProductId,
    decimal Quantity,
    decimal? UnitPriceSnapshot = null,
    string? UnitOfMeasure = null,
    string? SellingMode = null,
    decimal? LineTotal = null,
    string? NameSnapshot = null,
    string? SkuSnapshot = null,
    string? BarcodeSnapshot = null,
    Guid? SellingUnitId = null,
    decimal? EnteredQuantity = null);

/// <summary>
/// Checkout request. The cart itself is never persisted server-side; it exists only in the client
/// session until this single request records the sale. Product-Based Utang supplies CustomerId and
/// optional DueDate / CreditEntryId. Settled Cash/Card/GCash/ManualGCash may optionally supply
/// CustomerId for linked-merchant projection; they must omit DueDate / CreditEntryId.
/// Buyer party fields identify the counterparty only — seller OrganizationId owns the sale.
/// </summary>
public sealed record CheckoutSaleRequest(
    List<CheckoutSaleLineRequest> Lines,
    string PaymentMethod,
    decimal? AmountTendered = null,
    string? GCashReference = null,
    Guid? SaleId = null,
    Guid? CustomerId = null,
    DateOnly? DueDate = null,
    Guid? CreditEntryId = null,
    Guid? ShiftId = null,
    string? BuyerPartyKind = null,
    string? BuyerDisplayNameSnapshot = null,
    string? BuyerPersonalPublicUserId = null,
    Guid? BuyerOrganizationId = null,
    string? BuyerPublicOrganizationId = null);

public sealed record VoidSaleRequest(string Reason);

public sealed record PosSalePagedResult(
    List<PosSaleDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
