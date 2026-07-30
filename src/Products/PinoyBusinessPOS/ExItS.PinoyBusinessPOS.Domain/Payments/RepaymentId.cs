using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Payments;

/// <summary>Strongly typed identifier for a customer Utang repayment. Not a SaaS payment.</summary>
public sealed class RepaymentId : IEquatable<RepaymentId>
{
    public Guid Value { get; }

    private RepaymentId(Guid value) => Value = value;

    public static RepaymentId New() => new(Guid.NewGuid());

    public static RepaymentId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidRepaymentId,
                "RepaymentId cannot be an empty GUID.");
        }

        return new RepaymentId(value);
    }

    public bool Equals(RepaymentId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is RepaymentId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(RepaymentId? left, RepaymentId? right) => Equals(left, right);

    public static bool operator !=(RepaymentId? left, RepaymentId? right) => !Equals(left, right);
}
