namespace ExItS.PinoyBusinessPOS.Application.Returns;

public sealed record PosSaleReturnLineDto(
    Guid SaleReturnLineId,
    Guid SaleLineId,
    Guid ProductId,
    string ProductNameSnapshot,
    string UnitOfMeasure,
    decimal QuantityReturned,
    decimal UnitPriceSnapshot,
    decimal RefundAmount,
    string RestockDisposition,
    string? LineReason,
    Guid? InventoryMovementId);

public sealed record PosSaleReturnDto(
    Guid ReturnId,
    Guid OrganizationId,
    string ReturnNumber,
    Guid SaleId,
    string RefundMethod,
    string Status,
    DateOnly ReturnDate,
    string Reason,
    string? Notes,
    decimal TotalRefundAmount,
    DateTimeOffset CreatedAtUtc,
    Guid CreatedBy,
    DateTimeOffset CompletedAtUtc,
    Guid? CashierShiftId,
    IReadOnlyList<PosSaleReturnLineDto> Lines);

public sealed record PosRefundableSaleLineDto(
    Guid SaleLineId,
    Guid ProductId,
    string ProductNameSnapshot,
    string UnitOfMeasure,
    string SellingMode,
    decimal OriginalQuantity,
    decimal UnitPriceSnapshot,
    decimal OriginalLineTotal,
    decimal PreviouslyReturnedQuantity,
    decimal RefundableQuantity,
    decimal PreviouslyRefundedAmount,
    decimal RefundableAmount);

public sealed record PosRefundableSaleDto(
    Guid SaleId,
    string SaleNumber,
    string PaymentMethod,
    string Status,
    IReadOnlyList<PosRefundableSaleLineDto> Lines);

public sealed record CreateSaleReturnLineRequest(
    Guid SaleLineId,
    decimal Quantity,
    string RestockDisposition,
    string? LineReason = null);

public sealed record CreateSaleReturnRequest(
    Guid SaleId,
    string Reason,
    IReadOnlyList<CreateSaleReturnLineRequest> Lines,
    string? Notes = null,
    Guid? ReturnId = null);

public sealed record PosSaleReturnPagedResult(
    List<PosSaleReturnDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
