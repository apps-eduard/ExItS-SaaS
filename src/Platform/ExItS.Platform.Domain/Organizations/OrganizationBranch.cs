using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

public enum OrganizationBranchStatus
{
    Active,
    Inactive,
    Archived
}

public sealed class OrganizationBranch
{
    private static readonly Regex CodePattern = new("^[A-Z0-9][A-Z0-9-]{0,30}[A-Z0-9]$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public OrganizationBranchId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public string Code { get; }
    public string Name { get; private set; }
    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? City { get; private set; }
    public string? Region { get; private set; }
    public string? PostalCode { get; private set; }
    public string? CountryCode { get; private set; }
    public bool IsPrimary { get; }
    public OrganizationBranchStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private OrganizationBranch(OrganizationBranchId id, PlatformOrganizationId organizationId, string code, string name,
        string? addressLine1, string? addressLine2, string? city, string? region, string? postalCode, string? countryCode,
        bool isPrimary, OrganizationBranchStatus status, DateTimeOffset createdAtUtc, DateTimeOffset updatedAtUtc)
    {
        Id = id; OrganizationId = organizationId; Code = code; Name = name;
        AddressLine1 = addressLine1; AddressLine2 = addressLine2; City = city; Region = region; PostalCode = postalCode; CountryCode = countryCode;
        IsPrimary = isPrimary; Status = status; CreatedAtUtc = createdAtUtc; UpdatedAtUtc = updatedAtUtc;
    }

    public static OrganizationBranch CreateMainBranch(PlatformOrganizationId organizationId, DateTimeOffset utcNow) =>
        CreateInternal(organizationId, "MAIN", "Main Branch", null, null, null, null, null, null, true, OrganizationBranchStatus.Active, utcNow, null);

    public static OrganizationBranch Create(PlatformOrganizationId organizationId, string code, string name, DateTimeOffset utcNow,
        string? addressLine1 = null, string? addressLine2 = null, string? city = null, string? region = null, string? postalCode = null,
        string? countryCode = null, OrganizationBranchId? id = null) =>
        CreateInternal(organizationId, code, name, addressLine1, addressLine2, city, region, postalCode, countryCode, false, OrganizationBranchStatus.Active, utcNow, id);

    internal static OrganizationBranch Rehydrate(OrganizationBranchId id, PlatformOrganizationId organizationId, string code, string name,
        string? addressLine1, string? addressLine2, string? city, string? region, string? postalCode, string? countryCode, bool isPrimary,
        OrganizationBranchStatus status, DateTimeOffset createdAtUtc, DateTimeOffset updatedAtUtc) =>
        new(id, organizationId, NormalizeCode(code), NormalizeName(name), NormalizeOptional(addressLine1, 200), NormalizeOptional(addressLine2, 200),
            NormalizeOptional(city, 100), NormalizeOptional(region, 100), NormalizeOptional(postalCode, 32), NormalizeCountryCode(countryCode),
            isPrimary, status, createdAtUtc, updatedAtUtc);

    public void Rename(string name, DateTimeOffset utcNow) { EnsureMutable(utcNow); Name = NormalizeName(name); UpdatedAtUtc = utcNow; }
    public void UpdateAddress(string? addressLine1, string? addressLine2, string? city, string? region, string? postalCode, string? countryCode, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        AddressLine1 = NormalizeOptional(addressLine1, 200); AddressLine2 = NormalizeOptional(addressLine2, 200); City = NormalizeOptional(city, 100);
        Region = NormalizeOptional(region, 100); PostalCode = NormalizeOptional(postalCode, 32); CountryCode = NormalizeCountryCode(countryCode); UpdatedAtUtc = utcNow;
    }
    public void Activate(DateTimeOffset utcNow) => TransitionTo(OrganizationBranchStatus.Active, utcNow);
    public void Deactivate(DateTimeOffset utcNow) => TransitionTo(OrganizationBranchStatus.Inactive, utcNow);
    public void Archive(DateTimeOffset utcNow) => TransitionTo(OrganizationBranchStatus.Archived, utcNow);
    public void EnsureActive()
    {
        if (Status != OrganizationBranchStatus.Active) throw new DomainException(DomainErrorCodes.OrganizationBranchNotActive, "Organization branch is not active.");
    }

    public static string NormalizeCode(string code)
    {
        var normalized = code?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalized.Length is < 2 or > 32 || !CodePattern.IsMatch(normalized))
            throw new DomainException(DomainErrorCodes.InvalidOrganizationBranchCode, "Branch code must be 2–32 uppercase alphanumeric characters or hyphens.");
        return normalized;
    }

    private static OrganizationBranch CreateInternal(PlatformOrganizationId organizationId, string code, string name, string? addressLine1, string? addressLine2,
        string? city, string? region, string? postalCode, string? countryCode, bool isPrimary, OrganizationBranchStatus status, DateTimeOffset utcNow, OrganizationBranchId? id)
    {
        ArgumentNullException.ThrowIfNull(organizationId); DomainTime.EnsureUtc(utcNow);
        return new(id ?? OrganizationBranchId.New(), organizationId, NormalizeCode(code), NormalizeName(name), NormalizeOptional(addressLine1, 200),
            NormalizeOptional(addressLine2, 200), NormalizeOptional(city, 100), NormalizeOptional(region, 100), NormalizeOptional(postalCode, 32),
            NormalizeCountryCode(countryCode), isPrimary, status, utcNow, utcNow);
    }
    private void EnsureMutable(DateTimeOffset utcNow) { DomainTime.EnsureUtc(utcNow); if (Status == OrganizationBranchStatus.Archived) throw new DomainException(DomainErrorCodes.InvalidOrganizationBranchStatusTransition, "An archived branch cannot be changed."); }
    private void TransitionTo(OrganizationBranchStatus target, DateTimeOffset utcNow) { EnsureMutable(utcNow); if (Status != target) { Status = target; UpdatedAtUtc = utcNow; } }
    private static string NormalizeName(string value) => DomainTime.NormalizeDisplayName(value);
    private static string? NormalizeOptional(string? value, int maximum) => string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, maximum)];
    private static string? NormalizeCountryCode(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant() is var normalized && normalized.Length == 2 && normalized.All(char.IsLetter) ? normalized : throw new DomainException(DomainErrorCodes.InvalidOrganizationBranchCode, "Country code must be a two-letter ISO code.");
}
