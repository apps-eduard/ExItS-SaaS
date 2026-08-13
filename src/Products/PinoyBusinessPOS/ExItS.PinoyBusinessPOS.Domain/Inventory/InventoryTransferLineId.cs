using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public sealed class InventoryTransferLineId : IEquatable<InventoryTransferLineId>
{
    public Guid Value { get; }

    private InventoryTransferLineId(Guid value) => Value = value;

    public static InventoryTransferLineId New() => new(Guid.NewGuid());

    public static InventoryTransferLineId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferLineId,
                "InventoryTransferLineId cannot be an empty GUID.");
        }

        return new InventoryTransferLineId(value);
    }

    public bool Equals(InventoryTransferLineId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is InventoryTransferLineId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(InventoryTransferLineId? left, InventoryTransferLineId? right) =>
        Equals(left, right);

    public static bool operator !=(InventoryTransferLineId? left, InventoryTransferLineId? right) =>
        !Equals(left, right);
}
