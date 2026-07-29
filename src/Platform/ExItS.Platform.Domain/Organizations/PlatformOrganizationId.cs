using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Strongly typed identifier for a Platform Organization (SaaS customer/account boundary).
/// Not a Clinic, Store, Branch, or Register.
/// </summary>
public sealed class PlatformOrganizationId : IEquatable<PlatformOrganizationId>
{
    public Guid Value { get; }

    private PlatformOrganizationId(Guid value) => Value = value;

    public static PlatformOrganizationId New() => new(Guid.NewGuid());

    public static PlatformOrganizationId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlatformOrganizationId,
                "PlatformOrganizationId cannot be an empty GUID.");
        }

        return new PlatformOrganizationId(value);
    }

    public bool Equals(PlatformOrganizationId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is PlatformOrganizationId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(PlatformOrganizationId? left, PlatformOrganizationId? right) =>
        Equals(left, right);

    public static bool operator !=(PlatformOrganizationId? left, PlatformOrganizationId? right) =>
        !Equals(left, right);
}
