using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public sealed class InventoryTransferReceiptId : IEquatable<InventoryTransferReceiptId>
{
    public Guid Value { get; }

    private InventoryTransferReceiptId(Guid value) => Value = value;

    public static InventoryTransferReceiptId New() => new(Guid.NewGuid());

    public static InventoryTransferReceiptId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferReceiptId,
                "InventoryTransferReceiptId cannot be an empty GUID.");
        }

        return new InventoryTransferReceiptId(value);
    }

    public bool Equals(InventoryTransferReceiptId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is InventoryTransferReceiptId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(InventoryTransferReceiptId? left, InventoryTransferReceiptId? right) =>
        Equals(left, right);

    public static bool operator !=(InventoryTransferReceiptId? left, InventoryTransferReceiptId? right) =>
        !Equals(left, right);
}
