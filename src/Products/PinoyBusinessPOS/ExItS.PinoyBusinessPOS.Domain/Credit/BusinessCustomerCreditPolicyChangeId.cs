using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Credit;

/// <summary>Strongly typed identifier for an append-only B2B credit policy change.</summary>
public sealed class BusinessCustomerCreditPolicyChangeId : IEquatable<BusinessCustomerCreditPolicyChangeId>
{
    public Guid Value { get; }

    private BusinessCustomerCreditPolicyChangeId(Guid value) => Value = value;

    public static BusinessCustomerCreditPolicyChangeId New() => new(Guid.NewGuid());

    public static BusinessCustomerCreditPolicyChangeId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBusinessCustomerCreditPolicyChangeId,
                "BusinessCustomerCreditPolicyChangeId cannot be an empty GUID.");
        }

        return new BusinessCustomerCreditPolicyChangeId(value);
    }

    public bool Equals(BusinessCustomerCreditPolicyChangeId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is BusinessCustomerCreditPolicyChangeId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(
        BusinessCustomerCreditPolicyChangeId? left,
        BusinessCustomerCreditPolicyChangeId? right) =>
        Equals(left, right);

    public static bool operator !=(
        BusinessCustomerCreditPolicyChangeId? left,
        BusinessCustomerCreditPolicyChangeId? right) =>
        !Equals(left, right);
}
