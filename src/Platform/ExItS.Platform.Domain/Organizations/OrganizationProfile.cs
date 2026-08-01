using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Optional organization contact and locale metadata. Does not grant product-local roles.
/// </summary>
public sealed class OrganizationProfile
{
    private static readonly Regex PhonePattern = new(
        @"^\+?[0-9][0-9 .\-()]{6,30}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex LocalePattern = new(
        @"^[a-z]{2}(?:-[A-Z]{2})?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex CurrencyPattern = new(
        @"^[A-Z]{3}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex CountryPattern = new(
        @"^[A-Z]{2}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public string? LegalName { get; }
    public string? ContactEmail { get; }
    public string? ContactPhone { get; }
    public string? AddressLine1 { get; }
    public string? AddressLine2 { get; }
    public string? City { get; }
    public string? Region { get; }
    public string? PostalCode { get; }
    public string? CountryCode { get; }
    public string? TimeZoneId { get; }
    public string? Locale { get; }
    public string? CurrencyCode { get; }

    private OrganizationProfile(
        string? legalName,
        string? contactEmail,
        string? contactPhone,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? region,
        string? postalCode,
        string? countryCode,
        string? timeZoneId,
        string? locale,
        string? currencyCode)
    {
        LegalName = legalName;
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
        AddressLine1 = addressLine1;
        AddressLine2 = addressLine2;
        City = city;
        Region = region;
        PostalCode = postalCode;
        CountryCode = countryCode;
        TimeZoneId = timeZoneId;
        Locale = locale;
        CurrencyCode = currencyCode;
    }

    public static OrganizationProfile Empty { get; } = new(
        null, null, null, null, null, null, null, null, null, null, null, null);

    public static OrganizationProfile Create(
        string? legalName,
        string? contactEmail,
        string? contactPhone,
        string? addressLine1,
        string? addressLine2,
        string? city,
        string? region,
        string? postalCode,
        string? countryCode,
        string? timeZoneId,
        string? locale,
        string? currencyCode)
    {
        string? legal = null;
        if (!string.IsNullOrWhiteSpace(legalName))
        {
            legal = PlatformOrganization.NormalizeOptionalDisplayName(legalName);
        }

        string? email = null;
        if (!string.IsNullOrWhiteSpace(contactEmail))
        {
            email = PlatformUser.NormalizeEmail(contactEmail);
        }

        return new OrganizationProfile(
            legal,
            email,
            NormalizePhone(contactPhone),
            NormalizeOptionalText(addressLine1, 200),
            NormalizeOptionalText(addressLine2, 200),
            NormalizeOptionalText(city, 100),
            NormalizeOptionalText(region, 100),
            NormalizeOptionalText(postalCode, 32),
            NormalizeCountry(countryCode),
            NormalizeTimeZone(timeZoneId),
            NormalizeLocale(locale),
            NormalizeCurrency(currencyCode));
    }

    private static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var trimmed = phone.Trim();
        if (!PhonePattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationProfile,
                "Contact phone format is invalid.");
        }

        return trimmed;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = Regex.Replace(value.Trim(), @"\s+", " ");
        if (trimmed.Length > maxLength
            || trimmed.Contains('<', StringComparison.Ordinal)
            || trimmed.Contains('>', StringComparison.Ordinal))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationProfile,
                $"Text field must be at most {maxLength} characters without markup.");
        }

        return trimmed;
    }

    private static string? NormalizeCountry(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return null;
        }

        var normalized = countryCode.Trim().ToUpperInvariant();
        if (!CountryPattern.IsMatch(normalized))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationProfile,
                "Country code must be ISO 3166-1 alpha-2.");
        }

        return normalized;
    }

    private static string? NormalizeTimeZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return null;
        }

        var trimmed = timeZoneId.Trim();
        if (!TimeZoneInfo.TryFindSystemTimeZoneById(trimmed, out _))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationProfile,
                "Time zone is not recognized.");
        }

        return trimmed;
    }

    private static string? NormalizeLocale(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return null;
        }

        var trimmed = locale.Trim();
        // Accept en / en-US / fil-PH style; normalize region to upper.
        var parts = trimmed.Split('-', 2);
        var language = parts[0].ToLowerInvariant();
        var candidate = parts.Length == 1
            ? language
            : $"{language}-{parts[1].ToUpperInvariant()}";
        if (!LocalePattern.IsMatch(candidate))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationProfile,
                "Locale must be like en or en-US.");
        }

        return candidate;
    }

    private static string? NormalizeCurrency(string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return null;
        }

        var normalized = currencyCode.Trim().ToUpperInvariant();
        if (!CurrencyPattern.IsMatch(normalized))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationProfile,
                "Currency code must be ISO 4217 (3 letters).");
        }

        return normalized;
    }
}
