namespace ExItS.Platform.Application.Reference;

public interface IPhilippineLocalityDirectory
{
    PhilippineLocalityDirectoryMetadata Metadata { get; }

    IReadOnlyList<PhilippineLocality> Search(string query, int limit = 20);

    IReadOnlyList<PhilippineRegion> ListRegions();

    IReadOnlyList<PhilippineLocality> ListByRegionCode(string regionCode);

    PhilippineLocality? GetByPsgcCode(string psgcCode);

    bool Contains(string psgcCode);
}
