namespace ExItS.Platform.Infrastructure.Persistence.Organizations;

internal sealed class PlatformOrganizationRecord
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? LegalName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? Region { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }
    public string? TimeZoneId { get; set; }
    public string? Locale { get; set; }
    public string? CurrencyCode { get; set; }
    public string? BrandDisplayName { get; set; }
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
    public string? AccentColor { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
