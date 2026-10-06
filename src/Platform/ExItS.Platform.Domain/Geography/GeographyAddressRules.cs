using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Geography;

/// <summary>
/// Shared address field rules. Persisted addresses stay with their owner; this only normalizes fields.
/// </summary>
public static class GeographyAddressRules
{
    public const string Home = "Home";
    public const string Office = "Office";
    public const string Other = "Other";

    public static string CanonicalCountryName(string? countryOrCode)
    {
        var found = CountryCatalog.Find(countryOrCode);
        if (found is not null)
        {
            return found.Name;
        }

        return countryOrCode?.Trim() ?? string.Empty;
    }

    public static string? NormalizeBarangay(string? countryOrCode, string? barangay)
    {
        if (!CountryAddressConfig.For(countryOrCode).SupportsBarangay)
        {
            return null;
        }

        return string.IsNullOrWhiteSpace(barangay) ? null : barangay.Trim();
    }

    public static void EnsureRequired(string? countryOrCode, string? barangay, string? postalCode)
    {
        var config = CountryAddressConfig.For(countryOrCode);
        if (config.SupportsBarangay && string.IsNullOrWhiteSpace(barangay))
        {
            throw new DomainException(
                DomainErrorCodes.PersonalAddressIncomplete,
                "Barangay is required.");
        }

        if (config.RequiresPostalCode && string.IsNullOrWhiteSpace(postalCode))
        {
            throw new DomainException(
                DomainErrorCodes.PersonalAddressIncomplete,
                $"{config.PostalCodeLabel} is required.");
        }
    }

    public static bool IsComplete(
        string? country,
        string? addressLine1,
        string? barangay,
        string? city,
        string? administrativeArea,
        string? postalCode = null)
    {
        if (string.IsNullOrWhiteSpace(country)
            || string.IsNullOrWhiteSpace(addressLine1)
            || string.IsNullOrWhiteSpace(city)
            || string.IsNullOrWhiteSpace(administrativeArea))
        {
            return false;
        }

        var config = CountryAddressConfig.For(country);
        if (config.SupportsBarangay && string.IsNullOrWhiteSpace(barangay))
        {
            return false;
        }

        return !config.RequiresPostalCode || !string.IsNullOrWhiteSpace(postalCode);
    }
}
