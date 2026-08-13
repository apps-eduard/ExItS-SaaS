namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

internal sealed class InventoryTransferRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
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
    public uint Xmin { get; set; }
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
    public string? DiscrepancyReason { get; set; }
    public string? DiscrepancyNote { get; set; }
    public Guid? SourceLotId { get; set; }
    public string? LotNumber { get; set; }
    public DateOnly? ExpirationDate { get; set; }
}

internal sealed class InventoryTransferNumberSequenceRecord
{
    public Guid OrganizationId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public long LastValue { get; set; }
}

internal sealed class InventoryBranchBalanceRecord
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ProductId { get; set; }
    public decimal OnHandQuantity { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
