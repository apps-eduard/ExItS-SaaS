using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>Strongly typed identifier for a single line of a POS sale.</summary>
public sealed class SaleLineId : IEquatable<SaleLineId>
{
    public Guid Value { get; }

    private SaleLineId(Guid value) => Value = value;

    public static SaleLineId New() => new(Guid.NewGuid());

    public static SaleLineId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidSaleLineId, "SaleLineId cannot be an empty GUID.");
        }

        return new SaleLineId(value);
    }

    public bool Equals(SaleLineId? other) => other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is SaleLineId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(SaleLineId? left, SaleLineId? right) => Equals(left, right);

    public static bool operator !=(SaleLineId? left, SaleLineId? right) => !Equals(left, right);
}
