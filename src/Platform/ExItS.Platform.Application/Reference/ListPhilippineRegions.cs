using ExItS.Platform.Application.Common;

namespace ExItS.Platform.Application.Reference;

public sealed record PhilippineRegionDto(string RegionCode, string RegionName);

public sealed class ListPhilippineRegions
{
    private readonly IPhilippineLocalityDirectory _directory;

    public ListPhilippineRegions(IPhilippineLocalityDirectory directory) => _directory = directory;

    public ApplicationResult<IReadOnlyList<PhilippineRegionDto>> Execute()
    {
        var regions = _directory.ListRegions()
            .Select(r => new PhilippineRegionDto(r.RegionCode, r.RegionName))
            .ToList();
        return ApplicationResult<IReadOnlyList<PhilippineRegionDto>>.Success(regions);
    }
}
