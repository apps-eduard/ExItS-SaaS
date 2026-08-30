using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

public sealed class BranchDeliveryServiceAreaId : IEquatable<BranchDeliveryServiceAreaId>
{
    public Guid Value { get; }

    private BranchDeliveryServiceAreaId(Guid value) => Value = value;

    public static BranchDeliveryServiceAreaId New() => new(Guid.NewGuid());

    public static BranchDeliveryServiceAreaId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainException(
                DomainErrorCodes.InvalidBranchDeliveryServiceArea,
                "BranchDeliveryServiceAreaId cannot be an empty GUID.")
            : new BranchDeliveryServiceAreaId(value);

    public bool Equals(BranchDeliveryServiceAreaId? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is BranchDeliveryServiceAreaId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");
    public static bool operator ==(BranchDeliveryServiceAreaId? left, BranchDeliveryServiceAreaId? right) => Equals(left, right);
    public static bool operator !=(BranchDeliveryServiceAreaId? left, BranchDeliveryServiceAreaId? right) => !Equals(left, right);
}
