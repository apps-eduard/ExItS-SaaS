namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

internal sealed class InventoryAccountRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProductId { get; set; }
    public bool IsTracked { get; set; }
    public decimal? ReorderLevel { get; set; }
    public decimal OnHandQuantity { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
