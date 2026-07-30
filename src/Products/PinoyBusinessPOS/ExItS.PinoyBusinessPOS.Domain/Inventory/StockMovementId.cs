using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>Strongly typed identifier for an immutable POS stock movement.</summary>
public sealed class StockMovementId : IEquatable<StockMovementId>
{
    public Guid Value { get; }

    private StockMovementId(Guid value) => Value = value;

    public static StockMovementId New() => new(Guid.NewGuid());

    public static StockMovementId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockMovementId,
                "StockMovementId cannot be an empty GUID.");
        }

        return new StockMovementId(value);
    }

    public bool Equals(StockMovementId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is StockMovementId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(StockMovementId? left, StockMovementId? right) => Equals(left, right);

    public static bool operator !=(StockMovementId? left, StockMovementId? right) => !Equals(left, right);
}
