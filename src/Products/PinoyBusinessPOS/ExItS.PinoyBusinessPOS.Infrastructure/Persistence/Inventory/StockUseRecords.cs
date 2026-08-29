namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

internal sealed class StockUseRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public string StockUseNumber { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public DateTimeOffset OccurredAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid CreatedByUserId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? VoidedByUserId { get; set; }
    public DateTimeOffset? VoidedAtUtc { get; set; }
    public string? IdempotencyKey { get; set; }
}

internal sealed class StockUseLineRecord
{
    public Guid Id { get; set; }
    public Guid StockUseId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProductId { get; set; }
    public Guid? ProductUnitId { get; set; }
    public int LineNumber { get; set; }
    public decimal QuantityEntered { get; set; }
    public decimal MultiplierToBase { get; set; }
    public decimal BaseQuantity { get; set; }
    public string NameSnapshot { get; set; } = string.Empty;
    public string UnitLabelSnapshot { get; set; } = string.Empty;
    public decimal? UnitCostSnapshot { get; set; }
    public decimal? LineCostSnapshot { get; set; }
    public Guid? InventoryMovementId { get; set; }
}

internal sealed class StockUseNumberSequenceRecord
{
    public Guid OrganizationId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public long LastValue { get; set; }
}
