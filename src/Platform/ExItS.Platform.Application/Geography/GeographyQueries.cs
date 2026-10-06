using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Geography;

namespace ExItS.Platform.Application.Geography;

public sealed class ListCountries
{
    private readonly IGeographyDirectory _directory;

    public ListCountries(IGeographyDirectory directory) => _directory = directory;

    public ApplicationResult<IReadOnlyList<Country>> Execute() =>
        ApplicationResult<IReadOnlyList<Country>>.Success(_directory.ListCountries());
}

public sealed class GetCountryAddressConfig
{
    private readonly IGeographyDirectory _directory;

    public GetCountryAddressConfig(IGeographyDirectory directory) => _directory = directory;

    public ApplicationResult<CountryAddressConfig> Execute(string? countryCode) =>
        ApplicationResult<CountryAddressConfig>.Success(_directory.GetConfig(countryCode));
}

public sealed class ListAdministrativeAreas
{
    private readonly IGeographyDirectory _directory;

    public ListAdministrativeAreas(IGeographyDirectory directory) => _directory = directory;

    public ApplicationResult<IReadOnlyList<AdministrativeAreaDto>> Execute(string? countryCode) =>
        ApplicationResult<IReadOnlyList<AdministrativeAreaDto>>.Success(_directory.ListAreas(countryCode));
}

public sealed class ListGeographyBarangays
{
    private readonly IGeographyDirectory _directory;

    public ListGeographyBarangays(IGeographyDirectory directory) => _directory = directory;

    public ApplicationResult<IReadOnlyList<GeographyBarangayDto>> Execute(string? cityId) =>
        ApplicationResult<IReadOnlyList<GeographyBarangayDto>>.Success(_directory.ListBarangays(cityId));
}

public sealed class ListGeographyCities
{
    private readonly IGeographyDirectory _directory;

    public ListGeographyCities(IGeographyDirectory directory) => _directory = directory;

    public ApplicationResult<IReadOnlyList<GeographyCityDto>> Execute(string? areaId) =>
        ApplicationResult<IReadOnlyList<GeographyCityDto>>.Success(_directory.ListCities(areaId));
}
