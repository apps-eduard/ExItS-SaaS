using ExItS.Platform.Application.Common;

namespace ExItS.Platform.Application.Reference;

public sealed record PhilippineProvinceDto(string ProvinceCode, string ProvinceName);

public sealed class ListPhilippineProvinces
{
    private readonly IPhilippineLocalityDirectory _directory;

    public ListPhilippineProvinces(IPhilippineLocalityDirectory directory) => _directory = directory;

    public ApplicationResult<IReadOnlyList<PhilippineProvinceDto>> Execute() =>
        ApplicationResult<IReadOnlyList<PhilippineProvinceDto>>.Success(
            _directory.ListProvinces()
                .Select(province => new PhilippineProvinceDto(province.ProvinceCode, province.ProvinceName))
                .ToList());
}

public sealed class ListPhilippineLocalitiesByProvince
{
    private readonly IPhilippineLocalityDirectory _directory;

    public ListPhilippineLocalitiesByProvince(IPhilippineLocalityDirectory directory) =>
        _directory = directory;

    public ApplicationResult<IReadOnlyList<PhilippineLocalityDto>> Execute(string? provinceCode)
    {
        var results = _directory.ListByProvinceCode(provinceCode ?? string.Empty);
        return ApplicationResult<IReadOnlyList<PhilippineLocalityDto>>.Success(
            results.Select(SearchPhilippineLocalities.ToDto).ToList());
    }
}
