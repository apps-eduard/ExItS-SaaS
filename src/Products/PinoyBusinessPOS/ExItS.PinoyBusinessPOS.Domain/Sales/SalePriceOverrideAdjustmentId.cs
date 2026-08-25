using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>Strongly typed identifier for one recorded sale-line unit-price override adjustment.</summary>
public sealed class SalePriceOverrideAdjustmentId : IEquatable<SalePriceOverrideAdjustmentId>
{
    public Guid Value { get; }

    private SalePriceOverrideAdjustmentId(Guid value) => Value = value;

    public static SalePriceOverrideAdjustmentId New() => new(Guid.NewGuid());

    public static SalePriceOverrideAdjustmentId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSalePriceOverrideAdjustmentId,
                "SalePriceOverrideAdjustmentId cannot be an empty GUID.");
        }

        return new SalePriceOverrideAdjustmentId(value);
    }

    public bool Equals(SalePriceOverrideAdjustmentId? other) => other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is SalePriceOverrideAdjustmentId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(SalePriceOverrideAdjustmentId? left, SalePriceOverrideAdjustmentId? right) =>
        Equals(left, right);

    public static bool operator !=(SalePriceOverrideAdjustmentId? left, SalePriceOverrideAdjustmentId? right) =>
        !Equals(left, right);
}
