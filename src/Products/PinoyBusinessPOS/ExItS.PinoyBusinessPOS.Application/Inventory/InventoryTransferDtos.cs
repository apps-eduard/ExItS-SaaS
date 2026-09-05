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
    string? Notes = null);

public sealed record InventoryTransferReceiveLineRequest(
    Guid ProductId,
    decimal ReceivedQty,
    string? DiscrepancyReason = null,
    string? DiscrepancyNote = null,
    Guid? LineId = null);

public sealed record ReceiveInventoryTransferRequest(
    IReadOnlyList<InventoryTransferReceiveLineRequest> Lines);

public sealed record InventoryTransferLineDto(
    Guid LineId,
    Guid ProductId,
    string ProductName,
    string UnitOfMeasure,
    int LineNumber,
    decimal SentQty,
    decimal ReceivedQty,
    decimal DifferenceQty,
    string LineStatus,
    string? DiscrepancyReason,
    string? DiscrepancyNote,
    Guid? SourceLotId = null,
    string? LotNumber = null,
    DateOnly? ExpirationDate = null);

public sealed record InventoryTransferDto(
    Guid TransferId,
    Guid OrganizationId,
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
    decimal TotalSentQty,
    decimal TotalReceivedQty,
    decimal TotalDifferenceQty,
    IReadOnlyList<InventoryTransferLineDto> Lines);

public sealed record InventoryTransferListItemDto(
    Guid TransferId,
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
    Guid? CancelledBy = null);

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
    string Message);
