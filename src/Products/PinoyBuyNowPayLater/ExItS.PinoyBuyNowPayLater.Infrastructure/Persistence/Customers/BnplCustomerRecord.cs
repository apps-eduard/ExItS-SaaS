namespace ExItS.PinoyBuyNowPayLater.Infrastructure.Persistence.Customers;

internal sealed class BnplCustomerRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? NormalizedMobile { get; set; }
    public string? Email { get; set; }
    public string? NormalizedEmail { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? LinkedPersonalPublicUserId { get; set; }
    public Guid? LinkedCommerceCustomerId { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
