using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>Strongly typed identifier for one recorded commercial discount adjustment on a sale.</summary>
public sealed class SaleCommercialDiscountAdjustmentId : IEquatable<SaleCommercialDiscountAdjustmentId>
{
    public Guid Value { get; }

    private SaleCommercialDiscountAdjustmentId(Guid value) => Value = value;

    public static SaleCommercialDiscountAdjustmentId New() => new(Guid.NewGuid());

    public static SaleCommercialDiscountAdjustmentId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleDiscountAdjustmentId,
                "SaleCommercialDiscountAdjustmentId cannot be an empty GUID.");
        }

        return new SaleCommercialDiscountAdjustmentId(value);
    }

    public bool Equals(SaleCommercialDiscountAdjustmentId? other) => other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is SaleCommercialDiscountAdjustmentId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(SaleCommercialDiscountAdjustmentId? left, SaleCommercialDiscountAdjustmentId? right) =>
        Equals(left, right);

    public static bool operator !=(SaleCommercialDiscountAdjustmentId? left, SaleCommercialDiscountAdjustmentId? right) =>
        !Equals(left, right);
}
