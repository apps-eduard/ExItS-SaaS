namespace ExItS.PinoyBusinessPOS.Application.Returns;

public sealed record ReturnBatchLineDto(
    Guid ReturnBatchLineId,
    Guid? SaleLineId,
    Guid ProductId,
    string ProductNameSnapshot,
    string UnitOfMeasure,
    decimal UnitPriceSnapshot,
    decimal LineTotalSnapshot,
    decimal AcceptedQuantity,
    decimal RefundAmountSnapshot,
    decimal? SellableQuantity,
    decimal? DamagedQuantity,
    string? InspectionNote,
    DateTimeOffset? ClassifiedAtUtc,
    Guid? ClassifiedBy,
    Guid? PurchaseOrderLineId = null,
    Guid? SupplierProductId = null);

public sealed record ReturnBatchRefundDto(
    Guid ReturnBatchRefundId,
    decimal Amount,
    string Method,
    string? Reference,
    string? Note,
    string? ClientRefundId,
    DateTimeOffset CreatedAtUtc,
    Guid CreatedBy);

public sealed record ReturnBatchTimelineEventDto(
    Guid ReturnBatchAuditEventId,
    string EventType,
    string PayloadJson,
    DateTimeOffset CreatedAtUtc,
    Guid CreatedBy);

public sealed record ReturnBatchFinancialSummaryDto(
    decimal OriginalSaleTotal,
    decimal AcceptedReturnValue,
    decimal AmountPreviouslyPaid,
    decimal ObligationReduced,
    decimal RemainingAmountDue,
    decimal RefundDue,
    decimal Refunded,
    decimal RefundRemaining,
    decimal ReturnedQuantity,
    decimal SellableQuantity,
    decimal DamagedQuantity);

public sealed record ReturnBatchDto(
    Guid ReturnBatchId,
    Guid OrganizationId,
    Guid? SaleId,
    Guid? BranchId,
    string BatchNumber,
    string Status,
    string RefundStatus,
    decimal AcceptedReturnValue,
    decimal RefundDueAmount,
    decimal RefundedAmount,
    Guid? SaleReturnId,
    string Reason,
    string? Notes,
    DateTimeOffset CreatedAtUtc,
    Guid CreatedBy,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? FinalizedAtUtc,
    Guid? FinalizedBy,
    ReturnBatchFinancialSummaryDto FinancialSummary,
    IReadOnlyList<ReturnBatchLineDto> Lines,
    IReadOnlyList<ReturnBatchRefundDto> Refunds,
    string SourceType = "Sale",
    Guid? PurchaseOrderId = null,
    Guid? ConnectedPurchaseOrderId = null,
    Guid? BuyerOrganizationId = null,
    Guid? SellerOrganizationId = null,
    Guid? BuyerBranchId = null,
    Guid? SellerBranchId = null,
    string? PaymentTiming = null,
    string? PoNumberSnapshot = null,
    DateTimeOffset? SellerReceivedAtUtc = null,
    Guid? SellerReceivedBy = null);

public sealed record ReturnBatchReviewPreviewDto(
    Guid ReturnBatchId,
    string BatchNumber,
    string Status,
    string RefundStatus,
    decimal AcceptedReturnValue,
    decimal RefundDueAmount,
    decimal RefundedAmount,
    ReturnBatchFinancialSummaryDto FinancialSummary,
    IReadOnlyList<ReturnBatchLineDto> Lines);

public sealed record AcceptReturnBatchLineRequest(
    Guid SaleLineId,
    decimal AcceptedQuantity);

public sealed record AcceptReturnBatchRequest(
    Guid SaleId,
    string Reason,
    IReadOnlyList<AcceptReturnBatchLineRequest> Lines,
    string? Notes = null,
    Guid? ReturnBatchId = null);

public sealed record ClassifyReturnBatchLineRequest(
    decimal SellableQuantity,
    decimal DamagedQuantity,
    string? InspectionNote = null,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);

public sealed record FinalizeReturnBatchRequest(
    DateTimeOffset ExpectedUpdatedAtUtc);

public sealed record RecordReturnBatchRefundRequest(
    decimal Amount,
    string Method,
    string? Reference = null,
    string? Note = null,
    string? ClientRefundId = null);

public sealed record ConnectedPoReturnableLineDto(
    Guid PurchaseOrderLineId,
    Guid? ProductId,
    Guid? SupplierProductId,
    string ProductName,
    string UnitOfMeasure,
    decimal UnitPurchaseCost,
    decimal ReceivedQuantity,
    decimal AlreadyReturnedQuantity,
    decimal ReturnableQuantity);

public sealed record ConnectedPoReturnEligibilityDto(
    Guid PurchaseOrderId,
    Guid? ConnectedPurchaseOrderId,
    string? PoNumber,
    string PurchaseOrderStatus,
    bool CanRequestReturn,
    string? BlockedReason,
    Guid? BuyerOrganizationId,
    Guid? SellerOrganizationId,
    string? PaymentTiming,
    decimal GoodReceivedValue,
    decimal AmountPaid,
    IReadOnlyList<ConnectedPoReturnableLineDto> Lines,
    IReadOnlyList<ReturnBatchDto> Returns);

public sealed record RequestConnectedPoReturnLineRequest(
    Guid PurchaseOrderLineId,
    decimal Quantity);

public sealed record RequestConnectedPoReturnBatchRequest(
    Guid PurchaseOrderId,
    string Reason,
    IReadOnlyList<RequestConnectedPoReturnLineRequest> Lines,
    string? Notes = null,
    Guid? ReturnBatchId = null);

public sealed record ReceiveConnectedPoReturnBatchRequest(
    DateTimeOffset? ExpectedUpdatedAtUtc = null);
