namespace ExItS.Platform.Domain.Geography;

/// <summary>
/// Country-specific address labels and requirements. Products use this instead of country branches.
/// </summary>
public sealed record CountryAddressConfig(
    string CountryCode,
    string AdministrativeAreaLabel,
    string CityLabel,
    string PostalCodeLabel,
    bool RequiresAdministrativeArea,
    bool RequiresCity,
    bool RequiresPostalCode,
    bool SupportsBarangay)
{
    public static CountryAddressConfig For(string? countryOrCode)
    {
        var country = CountryCatalog.Find(countryOrCode);
        return country?.Code switch
        {
            "PH" => Philippines,
            "US" => UnitedStates,
            _ => International(country?.Code ?? string.Empty),
        };
    }

    public static CountryAddressConfig Philippines { get; } = new(
        "PH",
        "Province",
        "City / Municipality",
        "Postal Code",
        RequiresAdministrativeArea: true,
        RequiresCity: true,
        RequiresPostalCode: false,
        SupportsBarangay: true);

    public static CountryAddressConfig UnitedStates { get; } = new(
        "US",
        "State",
        "City",
        "ZIP Code",
        RequiresAdministrativeArea: true,
        RequiresCity: true,
        RequiresPostalCode: false,
        SupportsBarangay: false);

    public static CountryAddressConfig International(string countryCode) => new(
        countryCode,
        "State / Province",
        "City",
        "Postal / ZIP",
        RequiresAdministrativeArea: true,
        RequiresCity: true,
        RequiresPostalCode: false,
        SupportsBarangay: false);
}
