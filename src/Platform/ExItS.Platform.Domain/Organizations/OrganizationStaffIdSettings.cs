using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// How an organization numbers its staff. Configured in Platform Admin.
/// </summary>
public sealed class OrganizationStaffIdSettings
{
    public const string DefaultPrefix = "STF-";
    public const int DefaultPadDigits = 4;

    public PlatformOrganizationId OrganizationId { get; }
    public string Prefix { get; private set; }
    public int NextNumber { get; private set; }
    public int PadDigits { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private OrganizationStaffIdSettings(
        PlatformOrganizationId organizationId,
        string prefix,
        int nextNumber,
        int padDigits,
        DateTimeOffset updatedAtUtc)
    {
        OrganizationId = organizationId;
        Prefix = prefix;
        NextNumber = nextNumber;
        PadDigits = padDigits;
        UpdatedAtUtc = updatedAtUtc;
    }

    public static OrganizationStaffIdSettings CreateDefault(
        PlatformOrganizationId organizationId,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        EnsureUtc(utcNow);
        return new OrganizationStaffIdSettings(organizationId, DefaultPrefix, 1, DefaultPadDigits, utcNow);
    }

    public static OrganizationStaffIdSettings Rehydrate(
        PlatformOrganizationId organizationId,
        string prefix,
        int nextNumber,
        int padDigits,
        DateTimeOffset updatedAtUtc) =>
        new(organizationId, prefix, nextNumber, padDigits, updatedAtUtc);

    public void Configure(string prefix, int nextNumber, int padDigits, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        Prefix = NormalizePrefix(prefix);
        if (nextNumber < 1)
        {
            throw new DomainException(DomainErrorCodes.InvalidStaffNumber, "The next staff ID number must be 1 or greater.");
        }

        if (padDigits is < 1 or > 8)
        {
            throw new DomainException(DomainErrorCodes.InvalidStaffNumber, "Staff ID padding must be between 1 and 8 digits.");
        }

        NextNumber = nextNumber;
        PadDigits = padDigits;
        UpdatedAtUtc = utcNow;
    }

    public string AllocateNext(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        var id = Prefix + NextNumber.ToString().PadLeft(PadDigits, '0');
        NextNumber++;
        UpdatedAtUtc = utcNow;
        return id;
    }

    private static string NormalizePrefix(string prefix)
    {
        var trimmed = prefix.Trim();
        if (trimmed.Length is < 1 or > 12
            || trimmed.Any(ch => !char.IsLetterOrDigit(ch) && ch is not '-' and not '_'))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStaffNumber,
                "Staff ID prefix must be 1 to 12 letters, digits, hyphens, or underscores.");
        }

        return trimmed;
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }
}
