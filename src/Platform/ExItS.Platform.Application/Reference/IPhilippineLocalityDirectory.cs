namespace ExItS.Platform.Application.Reference;

public interface IPhilippineLocalityDirectory
{
    PhilippineLocalityDirectoryMetadata Metadata { get; }

    IReadOnlyList<PhilippineLocality> Search(string query, int limit = 20);

    PhilippineLocality? GetByPsgcCode(string psgcCode);

    bool Contains(string psgcCode);
}
