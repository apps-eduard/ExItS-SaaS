using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Reference;

namespace ExItS.Platform.Application.Reference;

public sealed record PhilippineLocalityDto(
    string PsgcCode,
    string Name,
    string LocalityType,
    string RegionCode,
    string RegionName,
    string? ProvinceCode,
    string? ProvinceName,
    string DisplayLabel);

public sealed class SearchPhilippineLocalities
{
    private readonly IPhilippineLocalityDirectory _directory;

    public SearchPhilippineLocalities(IPhilippineLocalityDirectory directory) => _directory = directory;

    public ApplicationResult<IReadOnlyList<PhilippineLocalityDto>> Execute(string? query, int? limit)
    {
        var capped = limit ?? PhilippineLocalityDirectoryLimits.DefaultLimit;
        if (capped < 1)
        {
            capped = PhilippineLocalityDirectoryLimits.DefaultLimit;
        }

        if (capped > PhilippineLocalityDirectoryLimits.MaxLimit)
        {
            capped = PhilippineLocalityDirectoryLimits.MaxLimit;
        }

        var results = _directory.Search(query ?? string.Empty, capped);
        return ApplicationResult<IReadOnlyList<PhilippineLocalityDto>>.Success(
            results.Select(ToDto).ToList());
    }

    internal static PhilippineLocalityDto ToDto(PhilippineLocality locality) =>
        new(
            locality.PsgcCode,
            locality.Name,
            locality.Type.ToString(),
            locality.RegionCode,
            locality.RegionName,
            locality.ProvinceCode,
            locality.ProvinceName,
            locality.DisplayLabel);
}

/// <summary>Limits mirrored from Infrastructure directory defaults for Application-layer callers.</summary>
public static class PhilippineLocalityDirectoryLimits
{
    public const int DefaultLimit = 20;
    public const int MaxLimit = 50;
}
