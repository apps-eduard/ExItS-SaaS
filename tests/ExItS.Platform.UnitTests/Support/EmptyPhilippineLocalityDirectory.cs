using ExItS.Platform.Application.Reference;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class EmptyPhilippineLocalityDirectory : IPhilippineLocalityDirectory
{
    public PhilippineLocalityDirectoryMetadata Metadata { get; } =
        new("test", "test", "2026-06-30", "2Q 2026", "PH", "test", 0);

    public IReadOnlyList<PhilippineLocality> Search(string query, int limit = 20) =>
        Array.Empty<PhilippineLocality>();

    public PhilippineLocality? GetByPsgcCode(string psgcCode) => null;

    public bool Contains(string psgcCode) => false;
}
