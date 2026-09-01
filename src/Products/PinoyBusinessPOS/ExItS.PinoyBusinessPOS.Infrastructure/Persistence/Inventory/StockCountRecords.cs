using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

internal sealed class StockCountRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid? BranchId { get; set; }
    public string? CountNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateOnly CountDate { get; set; }
    public string Title { get; set; } = StockCount.HistoricalTitle;
    public string? Notes { get; set; }
    public DateTimeOffset? StartedAtUtc { get; set; }
    public Guid? StartedBy { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public Guid? CompletedBy { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public Guid? CancelledBy { get; set; }
    public Guid? CreatedBy { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}

internal sealed class StockCountLineRecord
{
    public Guid Id { get; set; }
    public Guid StockCountId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProductId { get; set; }
    public int LineNumber { get; set; }
    public decimal? SystemOnHandSnapshot { get; set; }
    public decimal? CountedQuantity { get; set; }
}

internal sealed class StockCountNumberSequenceRecord
{
    public Guid OrganizationId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public long LastValue { get; set; }
}
