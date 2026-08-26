namespace ExItS.Platform.Infrastructure.Persistence.Settings;

internal sealed class PlatformSettingsRecord
{
    public int Id { get; set; }
    public string? PlatformDisplayName { get; set; }
    public string? SupportEmail { get; set; }
    public string? BrandingLogoUrl { get; set; }
    public string? BrandingPrimaryColor { get; set; }
    public string? BrandingAccentColor { get; set; }
    public string EmailProviderMode { get; set; } = "Smtp";
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }
    public string? ProtectedSmtpPassword { get; set; }
    public bool SmtpPasswordConfigured { get; set; }
    public string? FromDisplayName { get; set; }
    public string? FromAddress { get; set; }
    public string SmtpSecurityMode { get; set; } = "None";
    public string? AdminPublicBaseUrl { get; set; }
    public string? DefaultTimeZoneId { get; set; }
    public string? DefaultLocale { get; set; }
    public string? DefaultCurrencyCode { get; set; }
    public string? DefaultCountryCode { get; set; }
    public string? DateFormat { get; set; }
    public string? TimeFormat { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public string? UpdatedByActorId { get; set; }
    public int Version { get; set; }
    public uint Xmin { get; set; }
}
