using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>Strongly typed identifier for an expiration-aware inventory lot. Not a Platform identity.</summary>
public sealed class InventoryLotId : IEquatable<InventoryLotId>
{
    public Guid Value { get; }

    private InventoryLotId(Guid value) => Value = value;

    public static InventoryLotId New() => new(Guid.NewGuid());

    public static InventoryLotId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryLotId,
                "InventoryLotId cannot be an empty GUID.");
        }

        return new InventoryLotId(value);
    }

    public bool Equals(InventoryLotId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is InventoryLotId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(InventoryLotId? left, InventoryLotId? right) => Equals(left, right);

    public static bool operator !=(InventoryLotId? left, InventoryLotId? right) => !Equals(left, right);
}
