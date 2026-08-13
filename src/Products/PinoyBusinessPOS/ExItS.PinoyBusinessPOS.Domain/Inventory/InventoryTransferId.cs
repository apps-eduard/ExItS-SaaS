using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public sealed class InventoryTransferId : IEquatable<InventoryTransferId>
{
    public Guid Value { get; }

    private InventoryTransferId(Guid value) => Value = value;

    public static InventoryTransferId New() => new(Guid.NewGuid());

    public static InventoryTransferId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferId,
                "InventoryTransferId cannot be an empty GUID.");
        }

        return new InventoryTransferId(value);
    }

    public bool Equals(InventoryTransferId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is InventoryTransferId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(InventoryTransferId? left, InventoryTransferId? right) => Equals(left, right);

    public static bool operator !=(InventoryTransferId? left, InventoryTransferId? right) => !Equals(left, right);
}
