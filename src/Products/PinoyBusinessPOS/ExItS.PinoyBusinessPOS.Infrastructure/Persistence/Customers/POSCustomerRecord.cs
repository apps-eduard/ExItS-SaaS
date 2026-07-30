namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Customers;

internal sealed class POSCustomerRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? MobileNumber { get; set; }
    public string? NormalizedMobile { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
