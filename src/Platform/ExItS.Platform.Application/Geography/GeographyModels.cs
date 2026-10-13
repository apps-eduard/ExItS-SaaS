namespace ExItS.Platform.Application.Geography;

public sealed record AdministrativeAreaDto(
    string Id,
    string CountryCode,
    string Code,
    string Name,
    string Type,
    string? ParentId,
    bool IsActive);

public sealed record GeographyBarangayDto(
    string Id,
    string CityId,
    string Code,
    string Name);

public sealed record GeographyCityDto(
    string Id,
    string CountryCode,
    string AdministrativeAreaId,
    string Code,
    string Name,
    bool IsActive);
