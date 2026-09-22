using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

public sealed class InventoryTransferReceiptLineId : IEquatable<InventoryTransferReceiptLineId>
{
    public Guid Value { get; }

    private InventoryTransferReceiptLineId(Guid value) => Value = value;

    public static InventoryTransferReceiptLineId New() => new(Guid.NewGuid());

    public static InventoryTransferReceiptLineId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferReceiptLineId,
                "InventoryTransferReceiptLineId cannot be an empty GUID.");
        }

        return new InventoryTransferReceiptLineId(value);
    }

    public bool Equals(InventoryTransferReceiptLineId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is InventoryTransferReceiptLineId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(InventoryTransferReceiptLineId? left, InventoryTransferReceiptLineId? right) =>
        Equals(left, right);

    public static bool operator !=(InventoryTransferReceiptLineId? left, InventoryTransferReceiptLineId? right) =>
        !Equals(left, right);
}
