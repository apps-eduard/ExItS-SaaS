using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

public sealed class OrganizationAreaId : IEquatable<OrganizationAreaId>
{
    public Guid Value { get; }

    private OrganizationAreaId(Guid value) => Value = value;

    public static OrganizationAreaId New() => new(Guid.NewGuid());

    public static OrganizationAreaId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainException(DomainErrorCodes.InvalidOrganizationAreaId, "OrganizationAreaId cannot be an empty GUID.")
            : new OrganizationAreaId(value);

    public bool Equals(OrganizationAreaId? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is OrganizationAreaId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");
    public static bool operator ==(OrganizationAreaId? left, OrganizationAreaId? right) => Equals(left, right);
    public static bool operator !=(OrganizationAreaId? left, OrganizationAreaId? right) => !Equals(left, right);
}
