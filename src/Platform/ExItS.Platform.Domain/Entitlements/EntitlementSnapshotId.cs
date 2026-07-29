using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Entitlements;

public sealed class EntitlementSnapshotId : IEquatable<EntitlementSnapshotId>
{
    public Guid Value { get; }

    private EntitlementSnapshotId(Guid value) => Value = value;

    public static EntitlementSnapshotId New() => new(Guid.NewGuid());

    public static EntitlementSnapshotId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidEntitlementSnapshotId,
                "EntitlementSnapshotId cannot be an empty GUID.");
        }

        return new EntitlementSnapshotId(value);
    }

    public bool Equals(EntitlementSnapshotId? other) => other is not null && Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is EntitlementSnapshotId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");
    public static bool operator ==(EntitlementSnapshotId? left, EntitlementSnapshotId? right) => Equals(left, right);
    public static bool operator !=(EntitlementSnapshotId? left, EntitlementSnapshotId? right) => !Equals(left, right);
}
