using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

public sealed class OrganizationBranchId : IEquatable<OrganizationBranchId>
{
    public Guid Value { get; }

    private OrganizationBranchId(Guid value) => Value = value;

    public static OrganizationBranchId New() => new(Guid.NewGuid());

    public static OrganizationBranchId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainException(DomainErrorCodes.InvalidOrganizationBranchId, "OrganizationBranchId cannot be an empty GUID.")
            : new OrganizationBranchId(value);

    public bool Equals(OrganizationBranchId? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is OrganizationBranchId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");
    public static bool operator ==(OrganizationBranchId? left, OrganizationBranchId? right) => Equals(left, right);
    public static bool operator !=(OrganizationBranchId? left, OrganizationBranchId? right) => !Equals(left, right);
}
