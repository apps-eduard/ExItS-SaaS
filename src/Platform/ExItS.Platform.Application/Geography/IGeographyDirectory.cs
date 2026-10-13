using ExItS.Platform.Domain.Geography;

namespace ExItS.Platform.Application.Geography;

/// <summary>
/// Single geography lookup for every product. Philippine areas and cities come from the PSGC snapshot.
/// </summary>
public interface IGeographyDirectory
{
    IReadOnlyList<Country> ListCountries();

    CountryAddressConfig GetConfig(string? countryCode);

    IReadOnlyList<AdministrativeAreaDto> ListAreas(string? countryCode);

    IReadOnlyList<GeographyCityDto> ListCities(string? areaId);

    IReadOnlyList<GeographyBarangayDto> ListBarangays(string? cityId);
}
