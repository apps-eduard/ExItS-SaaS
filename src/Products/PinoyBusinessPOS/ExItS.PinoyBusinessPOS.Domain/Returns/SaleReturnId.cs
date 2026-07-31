using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Returns;

/// <summary>Strongly typed identifier for a POS sale return.</summary>
public sealed class SaleReturnId : IEquatable<SaleReturnId>
{
    public Guid Value { get; }

    private SaleReturnId(Guid value) => Value = value;

    public static SaleReturnId New() => new(Guid.NewGuid());

    public static SaleReturnId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidSaleReturnId, "SaleReturnId cannot be an empty GUID.");
        }

        return new SaleReturnId(value);
    }

    public bool Equals(SaleReturnId? other) => other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is SaleReturnId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(SaleReturnId? left, SaleReturnId? right) => Equals(left, right);

    public static bool operator !=(SaleReturnId? left, SaleReturnId? right) => !Equals(left, right);
}
