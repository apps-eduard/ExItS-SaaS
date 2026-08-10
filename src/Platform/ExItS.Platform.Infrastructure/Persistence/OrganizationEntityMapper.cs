using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Infrastructure.Persistence.Organizations;

namespace ExItS.Platform.Infrastructure.Persistence;

internal static class OrganizationEntityMapper
{
    public static PlatformOrganization ToDomain(PlatformOrganizationRecord record) =>
        PlatformOrganization.Rehydrate(
            PlatformOrganizationId.From(record.Id),
            record.DisplayName,
            record.Slug,
            record.PublicOrganizationId,
            record.PrimaryBusinessTypeId is null ? null : BusinessTypeId.From(record.PrimaryBusinessTypeId.Value),
            Enum.Parse<OrganizationStatus>(record.Status),
            OrganizationProfile.Create(
                record.LegalName,
                record.ContactEmail,
                record.ContactPhone,
                record.AddressLine1,
                record.AddressLine2,
                record.City,
                record.Region,
                record.PostalCode,
                record.CountryCode,
                record.TimeZoneId,
                record.Locale,
                record.CurrencyCode),
            OrganizationBranding.Create(
                record.BrandDisplayName,
                record.LogoUrl,
                record.PrimaryColor,
                record.AccentColor),
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    public static PlatformOrganizationRecord ToRecord(PlatformOrganization organization) =>
        new()
        {
            Id = organization.Id.Value,
            DisplayName = organization.DisplayName,
            Slug = organization.Slug,
            PublicOrganizationId = organization.PublicOrganizationId,
            PrimaryBusinessTypeId = organization.PrimaryBusinessTypeId?.Value,
            Status = organization.Status.ToString(),
            LegalName = organization.Profile.LegalName,
            ContactEmail = organization.Profile.ContactEmail,
            ContactPhone = organization.Profile.ContactPhone,
            AddressLine1 = organization.Profile.AddressLine1,
            AddressLine2 = organization.Profile.AddressLine2,
            City = organization.Profile.City,
            Region = organization.Profile.Region,
            PostalCode = organization.Profile.PostalCode,
            CountryCode = organization.Profile.CountryCode,
            TimeZoneId = organization.Profile.TimeZoneId,
            Locale = organization.Profile.Locale,
            CurrencyCode = organization.Profile.CurrencyCode,
            BrandDisplayName = organization.Branding.BrandDisplayName,
            LogoUrl = organization.Branding.LogoUrl,
            PrimaryColor = organization.Branding.PrimaryColor,
            AccentColor = organization.Branding.AccentColor,
            CreatedAtUtc = organization.CreatedAtUtc,
            UpdatedAtUtc = organization.UpdatedAtUtc
        };

    public static void ApplyToRecord(PlatformOrganization organization, PlatformOrganizationRecord record)
    {
        record.DisplayName = organization.DisplayName;
        record.Slug = organization.Slug;
        record.PublicOrganizationId = organization.PublicOrganizationId;
        record.PrimaryBusinessTypeId = organization.PrimaryBusinessTypeId?.Value;
        record.Status = organization.Status.ToString();
        record.LegalName = organization.Profile.LegalName;
        record.ContactEmail = organization.Profile.ContactEmail;
        record.ContactPhone = organization.Profile.ContactPhone;
        record.AddressLine1 = organization.Profile.AddressLine1;
        record.AddressLine2 = organization.Profile.AddressLine2;
        record.City = organization.Profile.City;
        record.Region = organization.Profile.Region;
        record.PostalCode = organization.Profile.PostalCode;
        record.CountryCode = organization.Profile.CountryCode;
        record.TimeZoneId = organization.Profile.TimeZoneId;
        record.Locale = organization.Profile.Locale;
        record.CurrencyCode = organization.Profile.CurrencyCode;
        record.BrandDisplayName = organization.Branding.BrandDisplayName;
        record.LogoUrl = organization.Branding.LogoUrl;
        record.PrimaryColor = organization.Branding.PrimaryColor;
        record.AccentColor = organization.Branding.AccentColor;
        record.UpdatedAtUtc = organization.UpdatedAtUtc;
    }
}
