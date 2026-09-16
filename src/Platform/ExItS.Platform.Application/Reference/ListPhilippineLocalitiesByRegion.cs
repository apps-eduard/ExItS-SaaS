using ExItS.Platform.Application.Common;

namespace ExItS.Platform.Application.Reference;

public sealed class ListPhilippineLocalitiesByRegion
{
    private readonly IPhilippineLocalityDirectory _directory;

    public ListPhilippineLocalitiesByRegion(IPhilippineLocalityDirectory directory) =>
        _directory = directory;

    public ApplicationResult<IReadOnlyList<PhilippineLocalityDto>> Execute(string? regionCode)
    {
        var results = _directory.ListByRegionCode(regionCode ?? string.Empty);
        return ApplicationResult<IReadOnlyList<PhilippineLocalityDto>>.Success(
            results.Select(SearchPhilippineLocalities.ToDto).ToList());
    }
}
