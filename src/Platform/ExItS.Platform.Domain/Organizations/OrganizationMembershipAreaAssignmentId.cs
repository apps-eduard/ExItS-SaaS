using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

public sealed class OrganizationMembershipAreaAssignmentId : IEquatable<OrganizationMembershipAreaAssignmentId>
{
    public Guid Value { get; }

    private OrganizationMembershipAreaAssignmentId(Guid value) => Value = value;

    public static OrganizationMembershipAreaAssignmentId New() => new(Guid.NewGuid());

    public static OrganizationMembershipAreaAssignmentId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainException(
                DomainErrorCodes.InvalidOrganizationMembershipAreaAssignmentId,
                "OrganizationMembershipAreaAssignmentId cannot be an empty GUID.")
            : new OrganizationMembershipAreaAssignmentId(value);

    public bool Equals(OrganizationMembershipAreaAssignmentId? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is OrganizationMembershipAreaAssignmentId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");

    public static bool operator ==(OrganizationMembershipAreaAssignmentId? left, OrganizationMembershipAreaAssignmentId? right) =>
        Equals(left, right);

    public static bool operator !=(OrganizationMembershipAreaAssignmentId? left, OrganizationMembershipAreaAssignmentId? right) =>
        !Equals(left, right);
}
