using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>Strongly typed identifier for a POS sale. Not a Platform or HealthCare identity.</summary>
public sealed class SaleId : IEquatable<SaleId>
{
    public Guid Value { get; }

    private SaleId(Guid value) => Value = value;

    public static SaleId New() => new(Guid.NewGuid());

    public static SaleId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidSaleId, "SaleId cannot be an empty GUID.");
        }

        return new SaleId(value);
    }

    public bool Equals(SaleId? other) => other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is SaleId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(SaleId? left, SaleId? right) => Equals(left, right);

    public static bool operator !=(SaleId? left, SaleId? right) => !Equals(left, right);
}
