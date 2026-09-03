using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

public enum OrganizationAreaStatus
{
    Active,
    Archived
}

/// <summary>
/// Organizational grouping of branches for access, navigation, and reporting.
/// An Area holds no operational authority: no stock, no reservations, no registers,
/// no shifts, no sales, and no receiving. Physical operations remain on
/// <see cref="OrganizationBranch"/>.
/// </summary>
public sealed class OrganizationArea
{
    private static readonly Regex CodePattern = new("^[A-Z0-9][A-Z0-9-]{0,30}[A-Z0-9]$", RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public OrganizationAreaId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public string Name { get; private set; }
    public string? Code { get; private set; }
    public OrganizationAreaStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public bool IsActive => Status == OrganizationAreaStatus.Active;

    private OrganizationArea(
        OrganizationAreaId id,
        PlatformOrganizationId organizationId,
        string name,
        string? code,
        OrganizationAreaStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        Id = id;
        OrganizationId = organizationId;
        Name = name;
        Code = code;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static OrganizationArea Create(
        PlatformOrganizationId organizationId,
        string name,
        DateTimeOffset utcNow,
        string? code = null,
        OrganizationAreaId? id = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        DomainTime.EnsureUtc(utcNow);
        return new OrganizationArea(
            id ?? OrganizationAreaId.New(),
            organizationId,
            DomainTime.NormalizeDisplayName(name),
            NormalizeCode(code),
            OrganizationAreaStatus.Active,
            utcNow,
            utcNow);
    }

    public static OrganizationArea Rehydrate(
        OrganizationAreaId id,
        PlatformOrganizationId organizationId,
        string name,
        string? code,
        OrganizationAreaStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc) =>
        new(id, organizationId, name, code, status, createdAtUtc, updatedAtUtc);

    public void Update(string name, string? code, DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        Name = DomainTime.NormalizeDisplayName(name);
        Code = NormalizeCode(code);
        UpdatedAtUtc = utcNow;
    }

    /// <summary>
    /// Archives the Area. Callers must first clear branch assignments and staff area grants;
    /// archiving never cascades to branches and never moves stock.
    /// </summary>
    public void Archive(DateTimeOffset utcNow)
    {
        EnsureMutable(utcNow);
        Status = OrganizationAreaStatus.Archived;
        UpdatedAtUtc = utcNow;
    }

    public void EnsureActive()
    {
        if (Status != OrganizationAreaStatus.Active)
        {
            throw new DomainException(DomainErrorCodes.OrganizationAreaNotActive, "Organization area is not active.");
        }
    }

    /// <summary>Optional area code. Follows the branch code shape when supplied.</summary>
    public static string? NormalizeCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalized = code.Trim().ToUpperInvariant();
        if (normalized.Length is < 2 or > 32 || !CodePattern.IsMatch(normalized))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationAreaCode,
                "Area code must be 2–32 uppercase alphanumeric characters or hyphens.");
        }

        return normalized;
    }

    private void EnsureMutable(DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == OrganizationAreaStatus.Archived)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationAreaStatusTransition,
                "An archived area cannot be changed.");
        }
    }
}
