using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

public sealed class OrganizationMembershipBranchAssignmentId : IEquatable<OrganizationMembershipBranchAssignmentId>
{
    public Guid Value { get; }

    private OrganizationMembershipBranchAssignmentId(Guid value) => Value = value;

    public static OrganizationMembershipBranchAssignmentId New() => new(Guid.NewGuid());

    public static OrganizationMembershipBranchAssignmentId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationMembershipId,
                "OrganizationMembershipBranchAssignmentId cannot be an empty GUID.");
        }

        return new OrganizationMembershipBranchAssignmentId(value);
    }

    public bool Equals(OrganizationMembershipBranchAssignmentId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is OrganizationMembershipBranchAssignmentId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(OrganizationMembershipBranchAssignmentId? left, OrganizationMembershipBranchAssignmentId? right) =>
        Equals(left, right);

    public static bool operator !=(OrganizationMembershipBranchAssignmentId? left, OrganizationMembershipBranchAssignmentId? right) =>
        !Equals(left, right);
}
