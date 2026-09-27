namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

internal sealed class InventoryLotIdentityCorrectionRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid ProductId { get; set; }
    public Guid InventoryLotId { get; set; }
    public DateOnly OldExpirationDate { get; set; }
    public DateOnly NewExpirationDate { get; set; }
    public string? OldLotNumber { get; set; }
    public string? NewLotNumber { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid CorrectedBy { get; set; }
    public DateTimeOffset CorrectedAtUtc { get; set; }
}
