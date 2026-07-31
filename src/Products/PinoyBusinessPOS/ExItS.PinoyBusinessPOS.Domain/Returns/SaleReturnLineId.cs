using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Returns;

/// <summary>Strongly typed identifier for a line on a sale return.</summary>
public sealed class SaleReturnLineId : IEquatable<SaleReturnLineId>
{
    public Guid Value { get; }

    private SaleReturnLineId(Guid value) => Value = value;

    public static SaleReturnLineId New() => new(Guid.NewGuid());

    public static SaleReturnLineId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleReturnLineId,
                "SaleReturnLineId cannot be an empty GUID.");
        }

        return new SaleReturnLineId(value);
    }

    public bool Equals(SaleReturnLineId? other) => other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is SaleReturnLineId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(SaleReturnLineId? left, SaleReturnLineId? right) => Equals(left, right);

    public static bool operator !=(SaleReturnLineId? left, SaleReturnLineId? right) => !Equals(left, right);
}
