using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public sealed record InventoryTransferLineRequest(
    Guid ProductId,
    decimal Quantity,
    Guid? SourceLotId = null);

public sealed record CreateInventoryTransferRequest(
    Guid SourceBranchId,
    Guid DestinationBranchId,
    IReadOnlyList<InventoryTransferLineRequest> Lines,
    string? Notes = null,
    Guid? StockRequestId = null,
    Guid? RootTransferId = null,
    int? ReplacementSequence = null,
    string? ReplacementReason = null,
    string? DamageHandlingPolicy = null);

public sealed record InventoryTransferReceiveLineRequest(
    Guid ProductId,
    decimal ReceivedQty = 0,
    string? DiscrepancyReason = null,
    string? DiscrepancyNote = null,
    Guid? LineId = null,
    decimal GoodQty = 0,
    decimal DamagedQty = 0,
    decimal MissingQty = 0,
    decimal OtherQty = 0,
    string? OtherReasonCode = null,
    string? OtherReasonNote = null,
    string? MissingDisposition = null,
    string? DamagedFollowUp = null,
    string? OtherFollowUp = null,
    string? DamagedCustodyDecision = null,
    string? OtherCustodyDecision = null,
    Guid? ActualReceivedProductId = null);

public sealed record ReceiveInventoryTransferRequest(
    IReadOnlyList<InventoryTransferReceiveLineRequest> Lines);

public sealed record CloseRemainderInventoryTransferLineRequest(
    string DiscrepancyReason,
    string? DiscrepancyNote = null,
    Guid? LineId = null,
    Guid? ProductId = null);

public sealed record CloseRemainderInventoryTransferRequest(
    IReadOnlyList<CloseRemainderInventoryTransferLineRequest>? Lines = null,
    string? DiscrepancyReason = null,
    string? DiscrepancyNote = null);

public sealed record InventoryTransferReceiptLineDto(
    Guid ReceiptLineId,
    Guid LineId,
    Guid ProductId,
    decimal QuantityReceived,
    decimal QuantityDamaged = 0,
    decimal QuantityMissing = 0,
    decimal QuantityOther = 0,
    string? OtherReasonCode = null,
    string? OtherReasonNote = null,
    string? MissingDisposition = null,
    string? DamagedFollowUp = null,
    string? OtherFollowUp = null,
    decimal QuantityWaived = 0,
    string? Note = null,
    Guid? ActualReceivedProductId = null,
    string? OtherCustodyDecision = null);

public sealed record InventoryTransferReceiptDto(
    Guid ReceiptId,
    int Sequence,
    DateTimeOffset ReceivedAtUtc,
    Guid ReceivedBy,
    IReadOnlyList<InventoryTransferReceiptLineDto> Lines);

public sealed record InventoryTransferLineDto(
    Guid LineId,
    Guid ProductId,
    string ProductName,
    string UnitOfMeasure,
    int LineNumber,
    decimal SentQty,
    decimal ReceivedQty,
    decimal OutstandingQty,
    decimal ClosedQty,
    decimal WaivedQty,
    decimal DifferenceQty,
    string LineStatus,
    string? DiscrepancyReason,
    string? DiscrepancyNote,
    Guid? SourceLotId = null,
    string? LotNumber = null,
    DateOnly? ExpirationDate = null,
    decimal? UnitCostSnapshot = null,
    string? Sku = null);

public sealed record InventoryTransferDto(
    Guid TransferId,
    Guid OrganizationId,
    Guid? StockRequestId,
    string? TransferNumber,
    Guid SourceBranchId,
    string? SourceBranchName,
    Guid DestinationBranchId,
    string? DestinationBranchName,
    string Status,
    string? Notes,
    Guid CreatedBy,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? DispatchedAtUtc,
    Guid? DispatchedBy,
    DateTimeOffset? ReceivedAtUtc,
    Guid? ReceivedBy,
    DateTimeOffset? CancelledAtUtc,
    Guid? CancelledBy,
    DateTimeOffset? ClosedAtUtc,
    Guid? ClosedBy,
    decimal TotalSentQty,
    decimal TotalReceivedQty,
    decimal TotalClosedQty,
    decimal TotalOutstandingQty,
    decimal TotalDifferenceQty,
    int ReceiptCount,
    DateTimeOffset? LastReceiptAtUtc,
    IReadOnlyList<InventoryTransferReceiptDto> Receipts,
    IReadOnlyList<InventoryTransferLineDto> Lines,
    Guid? RootTransferId = null,
    int? ReplacementSequence = null,
    string? ReplacementReason = null,
    string DamageHandlingPolicy = nameof(InventoryTransferDamageHandlingPolicy.ReceiverMayDecide),
    IReadOnlyList<InventoryTransferFamilyMemberDto>? FamilyMembers = null,
    IReadOnlyList<InventoryTransferDamageCustodyDto>? DamageCustodies = null,
    IReadOnlyList<InventoryTransferExceptionCustodyDto>? ExceptionCustodies = null,
    decimal SatisfiedAtDestinationQty = 0,
    decimal OpenInTransitQty = 0,
    decimal RemainingToDispatchQty = 0,
    decimal WaivedQty = 0);

public sealed record InventoryTransferFamilyMemberDto(
    Guid TransferId,
    string? TransferNumber,
    string Status,
    int? ReplacementSequence,
    bool IsRoot,
    decimal TotalSentQty,
    decimal TotalReceivedQty,
    decimal TotalOutstandingQty,
    decimal TotalDamagedQty = 0,
    decimal TotalMissingQty = 0,
    decimal TotalOtherQty = 0);

public sealed record InventoryTransferDamageCustodyDto(
    Guid CustodyId,
    Guid TransferId,
    Guid RootTransferId,
    Guid ReceiptLineId,
    Guid ProductId,
    decimal Quantity,
    string Decision,
    string FollowUpIntent,
    string Status,
    Guid HeldBranchId,
    decimal RecoveredSellableQty,
    decimal ConfirmedDamagedQty,
    decimal WaivedQty,
    decimal DestinationRecoveredSellableQty,
    decimal ReplacementDemandQty,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ReturnDispatchedAtUtc = null,
    DateTimeOffset? ReturnReceivedAtUtc = null,
    DateTimeOffset? InspectedAtUtc = null);

public sealed record InspectInventoryTransferDamageCustodyRequest(
    decimal RecoveredSellableQty,
    decimal ConfirmedDamagedQty,
    string? FollowUpOverride = null);

public sealed record InventoryTransferExceptionCustodyDto(
    Guid CustodyId,
    Guid TransferId,
    Guid RootTransferId,
    Guid ReceiptLineId,
    Guid ExpectedProductId,
    Guid ActualProductId,
    decimal Quantity,
    string ReasonCode,
    string Decision,
    string FollowUpIntent,
    string Status,
    Guid HeldBranchId,
    decimal RecoveredSellableQty,
    decimal ConfirmedNonSellableQty,
    decimal ReplacementDemandQty,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? ReturnDispatchedAtUtc = null,
    DateTimeOffset? ReturnReceivedAtUtc = null,
    DateTimeOffset? InspectedAtUtc = null,
    string? ExpectedProductName = null,
    string? ActualProductName = null);

public sealed record InspectInventoryTransferExceptionCustodyRequest(
    decimal RecoveredSellableQty,
    decimal ConfirmedNonSellableQty);

public sealed record InventoryTransferListItemDto(
    Guid TransferId,
    Guid? StockRequestId,
    string? TransferNumber,
    Guid SourceBranchId,
    string? SourceBranchName,
    Guid DestinationBranchId,
    string? DestinationBranchName,
    string Status,
    int LineCount,
    decimal TotalSentQty,
    decimal TotalReceivedQty,
    decimal TotalDifferenceQty,
    DateTimeOffset UpdatedAtUtc,
    Guid CreatedBy,
    Guid? DispatchedBy = null,
    Guid? ReceivedBy = null,
    Guid? CancelledBy = null,
    DateTimeOffset? ClosedAtUtc = null,
    Guid? ClosedBy = null);

public sealed record InventoryTransferFilter(
    string? Status = null,
    string? TransferNumber = null,
    Guid? SourceBranchId = null,
    Guid? DestinationBranchId = null,
    string? Direction = null,
    Guid? ActingBranchId = null);

public sealed record InventoryTransferAlert(
    string Kind,
    Guid OrganizationId,
    Guid TargetBranchId,
    Guid TransferId,
    string TransferNumber,
    string Message,
    Guid? StockRequestId = null);
