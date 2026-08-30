using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.SupplierPayables;

/// <summary>Strongly typed identifier for a posted supplier payable payment.</summary>
public sealed class SupplierPayablePaymentId : IEquatable<SupplierPayablePaymentId>
{
    public Guid Value { get; }

    private SupplierPayablePaymentId(Guid value) => Value = value;

    public static SupplierPayablePaymentId New() => new(Guid.NewGuid());

    public static SupplierPayablePaymentId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSupplierPayablePaymentId,
                "SupplierPayablePaymentId cannot be an empty GUID.");
        }

        return new SupplierPayablePaymentId(value);
    }

    public bool Equals(SupplierPayablePaymentId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is SupplierPayablePaymentId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(SupplierPayablePaymentId? left, SupplierPayablePaymentId? right) =>
        Equals(left, right);

    public static bool operator !=(SupplierPayablePaymentId? left, SupplierPayablePaymentId? right) =>
        !Equals(left, right);
}
