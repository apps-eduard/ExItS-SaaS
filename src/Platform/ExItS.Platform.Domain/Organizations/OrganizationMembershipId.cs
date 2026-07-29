using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>
/// Strongly typed identifier for an organization membership linking one Platform User to one Platform Organization.
/// </summary>
public sealed class OrganizationMembershipId : IEquatable<OrganizationMembershipId>
{
    public Guid Value { get; }

    private OrganizationMembershipId(Guid value) => Value = value;

    public static OrganizationMembershipId New() => new(Guid.NewGuid());

    public static OrganizationMembershipId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationMembershipId,
                "OrganizationMembershipId cannot be an empty GUID.");
        }

        return new OrganizationMembershipId(value);
    }

    public bool Equals(OrganizationMembershipId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is OrganizationMembershipId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(OrganizationMembershipId? left, OrganizationMembershipId? right) =>
        Equals(left, right);

    public static bool operator !=(OrganizationMembershipId? left, OrganizationMembershipId? right) =>
        !Equals(left, right);
}
