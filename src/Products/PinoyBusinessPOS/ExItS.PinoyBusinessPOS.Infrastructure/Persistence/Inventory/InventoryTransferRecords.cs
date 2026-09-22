namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

internal sealed class InventoryTransferRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? StockRequestId { get; set; }
    public string? TransferNumber { get; set; }
    public Guid SourceBranchId { get; set; }
    public Guid DestinationBranchId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? DispatchedAtUtc { get; set; }
    public Guid? DispatchedBy { get; set; }
    public DateTimeOffset? ReceivedAtUtc { get; set; }
    public Guid? ReceivedBy { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public Guid? CancelledBy { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public Guid? ClosedBy { get; set; }
    public Guid? RootTransferId { get; set; }
    public int? ReplacementSequence { get; set; }
    public string? ReplacementReason { get; set; }
    public string DamageHandlingPolicy { get; set; } = "ReceiverMayDecide";
    public uint Xmin { get; set; }
}

internal sealed class SupplyRouteRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid SourceLocationId { get; set; }
    public Guid DestinationLocationId { get; set; }
    public bool IsPreferred { get; set; }
    public bool IsActive { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}

internal sealed class StockRequestRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid DestinationLocationId { get; set; }
    public Guid RequestedSourceLocationId { get; set; }
    public string? RequestNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public Guid RequestedBy { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Guid? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAtUtc { get; set; }
    public Guid? PreparingStartedBy { get; set; }
    public DateTimeOffset? PreparingStartedAtUtc { get; set; }
    public Guid? DispatchedBy { get; set; }
    public DateTimeOffset? DispatchedAtUtc { get; set; }
    public Guid? LinkedInventoryTransferId { get; set; }
    public Guid? RejectedBy { get; set; }
    public DateTimeOffset? RejectedAtUtc { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? CancelledBy { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public uint Xmin { get; set; }
}

internal sealed class StockRequestLineRecord
{
    public Guid Id { get; set; }
    public Guid StockRequestId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProductId { get; set; }
    public int LineNumber { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal? ApprovedQuantity { get; set; }
    public string NameSnapshot { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
}

internal sealed class StockRequestNumberSequenceRecord
{
    public Guid OrganizationId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public long LastValue { get; set; }
}

internal sealed class InventoryTransferLineRecord
{
    public Guid Id { get; set; }
    public Guid TransferId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProductId { get; set; }
    public int LineNumber { get; set; }
    public string NameSnapshot { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal SentQty { get; set; }
    public decimal ReceivedQty { get; set; }
    public decimal ClosedQty { get; set; }
    public decimal WaivedQty { get; set; }
    public string? DiscrepancyReason { get; set; }
    public string? DiscrepancyNote { get; set; }
    public Guid? SourceLotId { get; set; }
    public string? LotNumber { get; set; }
    public DateOnly? ExpirationDate { get; set; }
    public decimal? UnitCostSnapshot { get; set; }
}

internal sealed class InventoryTransferNumberSequenceRecord
{
    public Guid OrganizationId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public long LastValue { get; set; }
}

internal sealed class InventoryTransferReceiptRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TransferId { get; set; }
    public int Sequence { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public Guid ReceivedBy { get; set; }
}

internal sealed class InventoryTransferReceiptLineRecord
{
    public Guid Id { get; set; }
    public Guid ReceiptId { get; set; }
    public Guid TransferLineId { get; set; }
    public Guid ProductId { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityDamaged { get; set; }
    public decimal QuantityMissing { get; set; }
    public decimal QuantityOther { get; set; }
    public string? OtherReasonCode { get; set; }
    public string? OtherReasonNote { get; set; }
    public string? MissingDisposition { get; set; }
    public string? DamagedFollowUp { get; set; }
    public string? OtherFollowUp { get; set; }
    public decimal QuantityWaived { get; set; }
    public string? Note { get; set; }
}

internal sealed class InventoryBranchBalanceRecord
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ProductId { get; set; }
    public decimal OnHandQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal PendingReturnQuantity { get; set; }
    public decimal InspectionHoldQuantity { get; set; }
    public decimal DamagedQuantity { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

internal sealed class InventoryTransferDamageCustodyRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid TransferId { get; set; }
    public Guid RootTransferId { get; set; }
    public Guid ReceiptLineId { get; set; }
    public Guid ProductId { get; set; }
    public decimal Quantity { get; set; }
    public string Decision { get; set; } = string.Empty;
    public string FollowUpIntent { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid HeldBranchId { get; set; }
    public decimal RecoveredSellableQty { get; set; }
    public decimal ConfirmedDamagedQty { get; set; }
    public decimal WaivedQty { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public DateTimeOffset? ReturnDispatchedAtUtc { get; set; }
    public Guid? ReturnDispatchedBy { get; set; }
    public DateTimeOffset? ReturnReceivedAtUtc { get; set; }
    public Guid? ReturnReceivedBy { get; set; }
    public DateTimeOffset? InspectedAtUtc { get; set; }
    public Guid? InspectedBy { get; set; }
}

internal sealed class InventoryBranchReorderSettingRecord
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ProductId { get; set; }
    public decimal? ReorderLevel { get; set; }
    public decimal? ReorderQuantity { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Guid UpdatedBy { get; set; }
    public uint Xmin { get; set; }
}

internal sealed class InventoryBranchReorderDefaultRecord
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public decimal? ReorderLevel { get; set; }
    public decimal? ReorderQuantity { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Guid UpdatedBy { get; set; }
    public uint Xmin { get; set; }
}
