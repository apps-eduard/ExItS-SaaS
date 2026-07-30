namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

internal sealed class InventoryReorderChangeRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid InventoryAccountId { get; set; }
    public Guid ProductId { get; set; }
    public decimal? PreviousReorderLevel { get; set; }
    public decimal? NewReorderLevel { get; set; }
    public decimal? PreviousReorderQuantity { get; set; }
    public decimal? NewReorderQuantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid ChangedBy { get; set; }
    public DateTimeOffset ChangedAtUtc { get; set; }
}
