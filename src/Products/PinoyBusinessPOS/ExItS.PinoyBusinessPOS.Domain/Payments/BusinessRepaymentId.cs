using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Payments;

/// <summary>Strongly typed identifier for a B2B business Utang repayment.</summary>
public sealed class BusinessRepaymentId : IEquatable<BusinessRepaymentId>
{
    public Guid Value { get; }

    private BusinessRepaymentId(Guid value) => Value = value;

    public static BusinessRepaymentId New() => new(Guid.NewGuid());

    public static BusinessRepaymentId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBusinessRepaymentId,
                "BusinessRepaymentId cannot be an empty GUID.");
        }

        return new BusinessRepaymentId(value);
    }

    public bool Equals(BusinessRepaymentId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is BusinessRepaymentId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(BusinessRepaymentId? left, BusinessRepaymentId? right) => Equals(left, right);

    public static bool operator !=(BusinessRepaymentId? left, BusinessRepaymentId? right) => !Equals(left, right);
}
