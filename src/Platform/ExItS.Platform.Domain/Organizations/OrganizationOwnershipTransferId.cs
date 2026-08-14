using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>Strongly typed identifier for an organization ownership transfer.</summary>
public sealed class OrganizationOwnershipTransferId : IEquatable<OrganizationOwnershipTransferId>
{
    public Guid Value { get; }

    private OrganizationOwnershipTransferId(Guid value) => Value = value;

    public static OrganizationOwnershipTransferId New() => new(Guid.NewGuid());

    public static OrganizationOwnershipTransferId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationOwnershipTransferId,
                "OrganizationOwnershipTransferId cannot be an empty GUID.");
        }

        return new OrganizationOwnershipTransferId(value);
    }

    public bool Equals(OrganizationOwnershipTransferId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is OrganizationOwnershipTransferId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(OrganizationOwnershipTransferId? left, OrganizationOwnershipTransferId? right) =>
        Equals(left, right);

    public static bool operator !=(OrganizationOwnershipTransferId? left, OrganizationOwnershipTransferId? right) =>
        !Equals(left, right);
}
