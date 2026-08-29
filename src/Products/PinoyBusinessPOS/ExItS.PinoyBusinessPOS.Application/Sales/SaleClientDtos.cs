namespace ExItS.PinoyBusinessPOS.Application.Sales;

/// <summary>
/// One recorded sale line. <c>LineTotal</c> is the net amount after commercial discounts, so the
/// existing client contract is unchanged; the gross and discount fields are additive and read zero
/// for undiscounted and legacy sales.
/// </summary>
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
    decimal LineTotal,
    decimal GrossLineTotal = 0m,
    decimal LineDiscountAmount = 0m,
    decimal SaleDiscountAllocatedAmount = 0m,
    decimal? UnitCostSnapshot = null,
    decimal? LineCostSnapshot = null);

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
    string? BuyerPublicOrganizationId = null,
    string DocumentKind = "TransactionSummary",
    Guid? BranchId = null,
    decimal GrossSubtotal = 0m,
    decimal LineDiscountTotal = 0m,
    decimal SaleDiscountTotal = 0m,
    decimal DiscountTotal = 0m,
    List<PosSaleQuotePriceOverrideDto>? PriceOverrides = null,
    string? CostStatus = null,
    decimal? TotalCostSnapshot = null,
    decimal? GrossProfit = null,
    decimal? GrossMarginPercent = null);

/// <summary>
/// A server-signed offline price lease presented back at checkout.
/// The client stores and replays it verbatim; it cannot mint or edit one, because
/// <c>Signature</c> covers every field here plus the organization, branch and product binding.
/// </summary>
public sealed record OfflinePriceAuthorityToken(
    Guid AuthorityId,
    Guid OrganizationId,
    Guid ProductId,
    string Signature,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    decimal UnitPrice,
    string UnitOfMeasure,
    string SellingMode,
    Guid? BranchId = null,
    Guid? SellingUnitId = null);

/// <summary>
/// One requested checkout line.
/// Online carts may send ProductId + Quantity only; the server then prices from the live catalog.
///
/// Offline React Cash sync sends <see cref="OfflinePriceAuthority"/>: a lease the server itself
/// signed, so the recorded price is server-authoritative even though the sale happened offline.
/// The legacy MAUI offline path (payload_version ≥ 2) instead sends immutable client snapshots,
/// whose arithmetic is validated but whose price the server has to take on trust.
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
    decimal? EnteredQuantity = null,
    OfflinePriceAuthorityToken? OfflinePriceAuthority = null);

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
    string? BuyerPublicOrganizationId = null,
    List<CommercialDiscountIntentRequest>? Discounts = null,
    List<SalePriceOverrideIntentRequest>? PriceOverrides = null);

/// <summary>
/// One requested manual commercial discount. The client sends intent only — scope, method, value and
/// reason. Every peso is computed server-side; a client-supplied amount is never trusted.
/// A line-scoped intent identifies its line by <c>LineNumber</c> (1-based) or, when the product
/// appears exactly once in the cart, by <c>ProductId</c>.
///
/// Scope: "Line" or "Sale". Method: "Percentage" or "FixedAmount".
/// </summary>
public sealed record CommercialDiscountIntentRequest(
    string Scope,
    string Method,
    decimal Value,
    string Reason,
    Guid? ProductId = null,
    int? LineNumber = null);

/// <summary>
/// One requested per-sale unit-price override. The client sends the requested unit price and reason;
/// the server resolves the baseline from the live catalog (or trusted offline snapshot path, which
/// rejects overrides) and never rewrites catalog SellingPrice. Optional
/// <c>ExpectedBaselineUnitPrice</c> fails closed on stale catalog prices.
/// </summary>
public sealed record SalePriceOverrideIntentRequest(
    decimal RequestedUnitPrice,
    string Reason,
    Guid? ProductId = null,
    int? LineNumber = null,
    decimal? ExpectedBaselineUnitPrice = null);

/// <summary>Per-line breakdown of a non-persisted checkout quote.</summary>
public sealed record PosSaleQuoteLineDto(
    int LineNumber,
    Guid ProductId,
    string Name,
    string UnitOfMeasure,
    string SellingMode,
    decimal UnitPrice,
    decimal Quantity,
    decimal GrossLineTotal,
    decimal LineDiscountAmount,
    decimal SaleDiscountAllocatedAmount,
    decimal LineTotal,
    decimal? BaselineUnitPrice = null);

/// <summary>One applied discount in a quote: what was asked for and what it came to in pesos.</summary>
public sealed record PosSaleQuoteDiscountDto(
    string Scope,
    string Method,
    decimal RequestedValue,
    decimal CalculatedAmount,
    string Reason,
    int? LineNumber);

/// <summary>One applied price override in a quote: baseline vs applied unit price.</summary>
public sealed record PosSaleQuotePriceOverrideDto(
    int LineNumber,
    decimal BaselineUnitPrice,
    decimal AppliedUnitPrice,
    string Reason);

/// <summary>
/// Non-persisted checkout preview. Nothing is recorded, no stock moves, no sale number is allocated.
/// Checkout revalidates independently, so a quote is never authorization to record these amounts.
/// </summary>
public sealed record PosSaleQuoteDto(
    decimal GrossSubtotal,
    decimal LineDiscountTotal,
    decimal SaleDiscountTotal,
    decimal DiscountTotal,
    decimal Subtotal,
    decimal TaxAmount,
    decimal Total,
    string? TaxPricingMode,
    List<PosSaleQuoteLineDto> Lines,
    List<PosSaleQuoteDiscountDto> Discounts,
    List<PosSaleQuotePriceOverrideDto>? PriceOverrides = null);

public sealed record VoidSaleRequest(string Reason);

public sealed record PosSalePagedResult(
    List<PosSaleDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
