using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Opaque Platform <c>OrganizationBranchId</c> stored by POS. Not a POS branches table and not a
/// cross-database foreign key.
/// </summary>
public sealed class PosBranchId : IEquatable<PosBranchId>
{
    public Guid Value { get; }

    private PosBranchId(Guid value) => Value = value;

    public static PosBranchId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBranchId,
                "BranchId cannot be an empty GUID.");
        }

        return new PosBranchId(value);
    }

    public bool Equals(PosBranchId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is PosBranchId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(PosBranchId? left, PosBranchId? right) => Equals(left, right);

    public static bool operator !=(PosBranchId? left, PosBranchId? right) => !Equals(left, right);
}
