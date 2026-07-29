namespace ExItS.Platform.Application.Contracts;

/// <summary>Positive major contract schema version. Unknown majors fail closed.</summary>
public readonly struct ContractVersion : IEquatable<ContractVersion>, IComparable<ContractVersion>
{
    public int Major { get; }
    public int Minor { get; }

    private ContractVersion(int major, int minor)
    {
        Major = major;
        Minor = minor;
    }

    public static ContractVersion Create(int major, int minor = 0)
    {
        if (major < 1)
        {
            throw new ContractException(
                ContractErrorCodes.InvalidContractVersion,
                "Contract major version must be positive.");
        }

        if (minor < 0)
        {
            throw new ContractException(
                ContractErrorCodes.InvalidContractVersion,
                "Contract minor version cannot be negative.");
        }

        return new ContractVersion(major, minor);
    }

    public static ContractVersion V1 => Create(1, 0);

    public bool IsCompatibleWith(ContractVersion supportedMaxMajorInclusive) =>
        Major <= supportedMaxMajorInclusive.Major;

    public bool Equals(ContractVersion other) => Major == other.Major && Minor == other.Minor;

    public override bool Equals(object? obj) => obj is ContractVersion other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Major, Minor);

    public int CompareTo(ContractVersion other)
    {
        var major = Major.CompareTo(other.Major);
        return major != 0 ? major : Minor.CompareTo(other.Minor);
    }

    public override string ToString() => $"{Major}.{Minor}";

    public static bool operator ==(ContractVersion left, ContractVersion right) => left.Equals(right);
    public static bool operator !=(ContractVersion left, ContractVersion right) => !left.Equals(right);
}
