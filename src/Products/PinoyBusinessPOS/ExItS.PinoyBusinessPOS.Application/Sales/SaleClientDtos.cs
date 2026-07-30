namespace ExItS.PinoyBusinessPOS.Application.Sales;

public sealed record PosSaleLineDto(
    Guid SaleLineId,
    Guid ProductId,
    int LineNumber,
    string Name,
    string? Sku,
    string? Barcode,
    string UnitOfMeasure,
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
    decimal? CustomerOutstandingAfter = null);

/// <summary>
/// One requested checkout line. Only the product identity and quantity are accepted — name, unit of
/// measure and unit price always come from the live catalog on the server, so any price or name a
/// client sends is ignored rather than trusted.
/// </summary>
public sealed record CheckoutSaleLineRequest(Guid ProductId, decimal Quantity);

/// <summary>
/// Checkout request. The cart itself is never persisted server-side; it exists only in the client
/// session until this single request records the sale. Product-Based Utang supplies CustomerId and
/// optional DueDate / CreditEntryId; Cash and ManualGCash must omit those.
/// </summary>
public sealed record CheckoutSaleRequest(
    List<CheckoutSaleLineRequest> Lines,
    string PaymentMethod,
    decimal? AmountTendered = null,
    string? GCashReference = null,
    Guid? SaleId = null,
    Guid? CustomerId = null,
    DateOnly? DueDate = null,
    Guid? CreditEntryId = null);

public sealed record VoidSaleRequest(string Reason);

public sealed record PosSalePagedResult(
    List<PosSaleDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
