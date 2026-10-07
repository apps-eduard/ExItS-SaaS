using ExItS.Platform.Application.Geography;
using ExItS.Platform.Application.Reference;
using ExItS.Platform.Domain.Geography;

namespace ExItS.Platform.Infrastructure.Geography;

/// <summary>
/// Platform geography. Countries come from <see cref="CountryCatalog"/>.
/// Philippine provinces and cities come from the existing PSGC locality directory.
/// </summary>
public sealed class PlatformGeographyDirectory : IGeographyDirectory
{
    private const string PhilippinesCode = "PH";
    private readonly IPhilippineLocalityDirectory _localities;
    private readonly PhilippineBarangayDirectory _barangays = new();

    public PlatformGeographyDirectory(IPhilippineLocalityDirectory localities) => _localities = localities;

    public IReadOnlyList<Country> ListCountries() => CountryCatalog.All;

    public CountryAddressConfig GetConfig(string? countryCode) => CountryAddressConfig.For(countryCode);

    public IReadOnlyList<AdministrativeAreaDto> ListAreas(string? countryCode)
    {
        if (!CountryCatalog.IsPhilippines(countryCode))
        {
            return [];
        }

        return _localities.ListProvinces()
            .Select(province => new AdministrativeAreaDto(
                province.ProvinceCode,
                PhilippinesCode,
                province.ProvinceCode,
                province.ProvinceName,
                "Province",
                null,
                true))
            .ToList();
    }

    public IReadOnlyList<GeographyCityDto> ListCities(string? areaId)
    {
        if (string.IsNullOrWhiteSpace(areaId))
        {
            return [];
        }

        return _localities.ListByProvinceCode(areaId)
            .Select(locality => new GeographyCityDto(
                locality.PsgcCode,
                PhilippinesCode,
                areaId.Trim(),
                locality.PsgcCode,
                PhilippineLocality.FriendlyName(locality.Name),
                true))
            .ToList();
    }

    public IReadOnlyList<GeographyBarangayDto> ListBarangays(string? cityId) =>
        _barangays.ListByCity(cityId);
}
