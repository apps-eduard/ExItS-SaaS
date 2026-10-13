namespace ExItS.Platform.Application.Reference;

public interface IPhilippineLocalityDirectory
{
    PhilippineLocalityDirectoryMetadata Metadata { get; }

    IReadOnlyList<PhilippineLocality> Search(string query, int limit = 20);

    IReadOnlyList<PhilippineRegion> ListRegions();

    IReadOnlyList<PhilippineLocality> ListByRegionCode(string regionCode);

    IReadOnlyList<PhilippineProvince> ListProvinces();

    IReadOnlyList<PhilippineLocality> ListByProvinceCode(string provinceCode);

    PhilippineLocality? GetByPsgcCode(string psgcCode);

    bool Contains(string psgcCode);
}
