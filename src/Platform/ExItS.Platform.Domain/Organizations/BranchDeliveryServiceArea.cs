using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// City/municipality-level delivery coverage for a branch.
/// Authoritative geographic identity is the Philippine PSGC code (<see cref="PsgcCode"/>).
/// Soft-deactivated (IsActive=false) areas no longer count toward fulfillment readiness
/// and free the unique active PSGC slot.
/// Legacy free-text rows may have a null <see cref="PsgcCode"/> and are unverified.
/// </summary>
public sealed class BranchDeliveryServiceArea
{
    public const int MaxCityMunicipalityNameLength = 100;
    public const int MaxRegionOrProvinceNameLength = 100;
    public const int MaxCountryCodeLength = 2;
    public const int MaxPsgcCodeLength = 64;
    public const string PhilippinesCountryCode = "PH";

    private static readonly Regex CollapseWhitespace = new(@"\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex PsgcDigits = new(@"^\d{10}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public BranchDeliveryServiceAreaId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public OrganizationBranchId BranchId { get; }
    public string CountryCode { get; private set; }
    public string? RegionOrProvinceName { get; private set; }
    public string CityMunicipalityName { get; private set; }
    public string NormalizedCityMunicipalityName { get; private set; }

    /// <summary>
    /// Philippine Standard Geographic Code for this City/Municipality.
    /// Persisted as column <c>external_area_code</c>. Null for legacy unverified free-text areas.
    /// </summary>
    public string? PsgcCode { get; private set; }

    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public bool IsPsgcVerified => !string.IsNullOrWhiteSpace(PsgcCode);

    private BranchDeliveryServiceArea(
        BranchDeliveryServiceAreaId id,
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        string countryCode,
        string? regionOrProvinceName,
        string cityMunicipalityName,
        string normalizedCityMunicipalityName,
        string? psgcCode,
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
        PsgcCode = psgcCode;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>
    /// Creates a PSGC-backed delivery area. Country is forced to PH.
    /// Canonical locality name/region/province must already be resolved by the application layer.
    /// </summary>
    public static BranchDeliveryServiceArea CreateFromPsgc(
        PlatformOrganizationId organizationId,
        OrganizationBranchId branchId,
        string psgcCode,
        string cityMunicipalityName,
        DateTimeOffset utcNow,
        string? regionOrProvinceName = null,
        IEnumerable<BranchDeliveryServiceArea>? existingActiveForBranch = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        ArgumentNullException.ThrowIfNull(branchId);
        DomainTime.EnsureUtc(utcNow);

        var normalizedPsgc = NormalizePsgcCode(psgcCode);
        var city = NormalizeRequiredName(cityMunicipalityName, MaxCityMunicipalityNameLength, "City/municipality");
        var normalizedCity = NormalizeCityKey(city);
        var region = NormalizeOptionalName(regionOrProvinceName, MaxRegionOrProvinceNameLength);

        if (existingActiveForBranch is not null)
        {
            EnsureNoDuplicateActivePsgc(branchId, normalizedPsgc, existingActiveForBranch);
        }

        return new(
            BranchDeliveryServiceAreaId.New(),
            organizationId,
            branchId,
            PhilippinesCountryCode,
            region,
            city,
            normalizedCity,
            normalizedPsgc,
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
        string? psgcCode,
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
            psgcCode,
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

    public static string NormalizePsgcCode(string psgcCode)
    {
        if (string.IsNullOrWhiteSpace(psgcCode))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchDeliveryServiceArea,
                "PSGC code is required.");
        }

        var trimmed = psgcCode.Trim();
        if (trimmed.Length > MaxPsgcCodeLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchDeliveryServiceArea,
                $"PSGC code cannot exceed {MaxPsgcCodeLength} characters.");
        }

        if (!PsgcDigits.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchDeliveryServiceArea,
                "PSGC code must be exactly 10 digits.");
        }

        return trimmed;
    }

    private static void EnsureNoDuplicateActivePsgc(
        OrganizationBranchId branchId,
        string psgcCode,
        IEnumerable<BranchDeliveryServiceArea> existingActiveForBranch)
    {
        foreach (var existing in existingActiveForBranch)
        {
            if (!existing.IsActive || existing.BranchId != branchId)
            {
                continue;
            }

            if (string.Equals(existing.PsgcCode, psgcCode, StringComparison.Ordinal))
            {
                throw new DomainException(
                    DomainErrorCodes.BranchDeliveryServiceAreaDuplicate,
                    "An active delivery service area with this PSGC locality already exists for the branch.");
            }
        }
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
