namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

internal sealed class InventoryLotRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? BranchId { get; set; }
    public string? LotNumber { get; set; }
    public string NormalizedLotNumber { get; set; } = string.Empty;
    public DateOnly ExpirationDate { get; set; }
    public decimal QuantityOnHand { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}

internal sealed class InventoryLotMovementRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid LotId { get; set; }
    public Guid ProductId { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public decimal QuantityEffect { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid? SourceId { get; set; }
    public Guid? StockMovementId { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
    public Guid RecordedBy { get; set; }
}
