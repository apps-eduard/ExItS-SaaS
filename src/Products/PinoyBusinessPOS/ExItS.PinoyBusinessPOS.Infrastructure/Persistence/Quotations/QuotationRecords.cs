using ExItS.PinoyBusinessPOS.Domain.Quotations;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Quotations;

internal sealed class QuotationNumberSequenceRecord
{
    public Guid OrganizationId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public long LastValue { get; set; }
}

internal sealed class QuotationRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string? QuotationNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerDisplayNameSnapshot { get; set; } = string.Empty;
    public string? CustomerMobileNumberSnapshot { get; set; }
    public string? CustomerAddressSnapshot { get; set; }
    public string? CustomerNotesSnapshot { get; set; }
    public Guid BranchId { get; set; }
    public Guid PreparedBy { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public string? Terms { get; set; }
    public Guid? ConvertedSaleId { get; set; }
    public bool IsCustomerVisible { get; set; }
    public DateTimeOffset? IssuedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}

internal sealed class QuotationLineRecord
{
    public Guid Id { get; set; }
    public Guid QuotationId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid ProductId { get; set; }
    public int LineNumber { get; set; }
    public string? NameSnapshot { get; set; }
    public string? SkuSnapshot { get; set; }
    public string? UomSnapshot { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
}
