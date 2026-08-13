namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.OperationalSetup;

internal sealed class OperationalSetupRecord
{
    public Guid OrganizationId { get; set; }
    public string StoreDisplayName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "PHP";
    public string TaxPricingMode { get; set; } = string.Empty;
    public decimal TaxRatePercent { get; set; }
    public string? ReceiptHeader { get; set; }
    public string? ReceiptFooter { get; set; }
    public string? BusinessAddress { get; set; }
    public string? ContactPhone { get; set; }
    public Guid? DefaultRegisterId { get; set; }
    public string CashCountMode { get; set; } = "Required";
    public bool IsCompleted { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Guid UpdatedBy { get; set; }
    public uint Xmin { get; set; }
}
