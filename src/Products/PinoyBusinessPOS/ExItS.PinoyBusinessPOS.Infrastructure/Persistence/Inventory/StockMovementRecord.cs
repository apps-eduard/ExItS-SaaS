namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

internal sealed class StockMovementRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProductId { get; set; }
    public Guid InventoryAccountId { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public decimal QuantityEffect { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public Guid? SourceId { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; }
    public Guid RecordedBy { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? InventoryLotId { get; set; }
}
