using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>Strongly typed identifier for a POS inventory account. Not a Platform or HealthCare identity.</summary>
public sealed class InventoryAccountId : IEquatable<InventoryAccountId>
{
    public Guid Value { get; }

    private InventoryAccountId(Guid value) => Value = value;

    public static InventoryAccountId New() => new(Guid.NewGuid());

    public static InventoryAccountId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryAccountId,
                "InventoryAccountId cannot be an empty GUID.");
        }

        return new InventoryAccountId(value);
    }

    public bool Equals(InventoryAccountId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is InventoryAccountId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(InventoryAccountId? left, InventoryAccountId? right) => Equals(left, right);

    public static bool operator !=(InventoryAccountId? left, InventoryAccountId? right) => !Equals(left, right);
}
