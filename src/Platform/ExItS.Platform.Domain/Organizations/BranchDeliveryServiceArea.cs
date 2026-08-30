using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// City/municipality-level delivery coverage for a branch. Soft-deactivated (IsActive=false) areas
/// no longer count toward fulfillment readiness and free the unique active city slot.
/// </summary>
public sealed class BranchDeliveryServiceArea
{
    public const int MaxCityMunicipalityNameLength = 100;
    public const int MaxRegionOrProvinceNameLength = 100;
    public const int MaxCountryCodeLength = 2;
    public const int MaxExternalAreaCodeLength = 64;

    private static readonly Regex CollapseWhitespace = new(@"\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public BranchDeliveryServiceAreaId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public OrganizationBranchId BranchId { get; }
    public string CountryCode { get; private set; }
    public string? RegionOrProvinceName { get; private set; }
    public string CityMunicipalityName { get; private set; }
    public string NormalizedCityMunicipalityName { get; private set; }
    public string? ExternalAreaCode { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private BranchDeliveryServiceArea(
        BranchDeliveryServiceAreaId id,
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        string countryCode,
        string? regionOrProvinceName,
        string cityMunicipalityName,
        string normalizedCityMunicipalityName,
        string? externalAreaCode,
        bool isActive,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        BranchId = branchId;
        CountryCode = countryCode;
        RegionOrProvinceName = regionOrProvinceName;
        CityMunicipalityName = cityMunicipalityName;
        NormalizedCityMunicipalityName = normalizedCityMunicipalityName;
        ExternalAreaCode = externalAreaCode;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static BranchDeliveryServiceArea Create(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        string countryCode,
        string cityMunicipalityName,
        DateTimeOffset utcNow,
        string? regionOrProvinceName = null,
        string? externalAreaCode = null,
        IEnumerable<BranchDeliveryServiceArea>? existingActiveForBranch = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(branchId);
        DomainTime.EnsureUtc(utcNow);

        var normalizedCountry = NormalizeCountryCode(countryCode);
        var city = NormalizeRequiredName(cityMunicipalityName, MaxCityMunicipalityNameLength, "City/municipality");
        var normalizedCity = NormalizeCityKey(city);
        var region = NormalizeOptionalName(regionOrProvinceName, MaxRegionOrProvinceNameLength);
        var external = NormalizeOptionalName(externalAreaCode, MaxExternalAreaCodeLength);

        if (existingActiveForBranch is not null)
        {
            EnsureNoDuplicateActiveCity(branchId, normalizedCity, existingActiveForBranch);
        }

        return new(
            BranchDeliveryServiceAreaId.New(),
            organizationId,
            branchId,
            normalizedCountry,
            region,
            city,
            normalizedCity,
            external,
            isActive: true,
            utcNow,
            utcNow);
    }

    public static BranchDeliveryServiceArea Rehydrate(
        BranchDeliveryServiceAreaId id,
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        string countryCode,
        string? regionOrProvinceName,
        string cityMunicipalityName,
        string normalizedCityMunicipalityName,
        string? externalAreaCode,
        bool isActive,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(
            id,
            organizationId,
            branchId,
            countryCode,
            regionOrProvinceName,
            cityMunicipalityName,
            normalizedCityMunicipalityName,
            externalAreaCode,
            isActive,
            createdAtUtc,
            updatedAtUtc);

    public void Deactivate(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedAtUtc = utcNow;
    }

    public static string NormalizeCityKey(string cityMunicipalityName)
    {
        if (string.IsNullOrWhiteSpace(cityMunicipalityName))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchDeliveryServiceArea,
                "City/municipality name is required.");
        }

        var collapsed = CollapseWhitespace.Replace(cityMunicipalityName.Trim(), " ");
        return collapsed.ToUpperInvariant();
    }

    private static void EnsureNoDuplicateActiveCity(
        OrganizationBranchId branchId,
        string normalizedCity,
        IEnumerable<BranchDeliveryServiceArea> existingActiveForBranch)
    {
        foreach (var existing in existingActiveForBranch)
        {
            if (!existing.IsActive || existing.BranchId != branchId)
            {
                continue;
            }

            if (string.Equals(existing.NormalizedCityMunicipalityName, normalizedCity, StringComparison.Ordinal))
            {
                throw new DomainException(
                    DomainErrorCodes.BranchDeliveryServiceAreaDuplicate,
                    "An active delivery service area with this city/municipality already exists for the branch.");
            }
        }
    }

    private static string NormalizeCountryCode(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchDeliveryServiceArea,
                "Country code is required.");
        }

        var normalized = countryCode.Trim().ToUpperInvariant();
        if (normalized.Length != MaxCountryCodeLength || !normalized.All(char.IsLetter))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchDeliveryServiceArea,
                "Country code must be a two-letter ISO code.");
        }

        return normalized;
    }

    private static string NormalizeRequiredName(string value, int maxLength, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchDeliveryServiceArea,
                $"{fieldLabel} name is required.");
        }

        var collapsed = CollapseWhitespace.Replace(value.Trim(), " ");
        if (collapsed.Length > maxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchDeliveryServiceArea,
                $"{fieldLabel} name cannot exceed {maxLength} characters.");
        }

        return collapsed;
    }

    private static string? NormalizeOptionalName(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var collapsed = CollapseWhitespace.Replace(value.Trim(), " ");
        if (collapsed.Length > maxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchDeliveryServiceArea,
                $"Value cannot exceed {maxLength} characters.");
        }

        return collapsed;
    }
}
