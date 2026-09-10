using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Credit;

/// <summary>Strongly typed identifier for a customer credit policy.</summary>
public sealed class CustomerCreditPolicyId : IEquatable<CustomerCreditPolicyId>
{
    public Guid Value { get; }

    private CustomerCreditPolicyId(Guid value) => Value = value;

    public static CustomerCreditPolicyId New() => new(Guid.NewGuid());

    public static CustomerCreditPolicyId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerCreditPolicyId,
                "CustomerCreditPolicyId cannot be an empty GUID.");
        }

        return new CustomerCreditPolicyId(value);
    }

    public bool Equals(CustomerCreditPolicyId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is CustomerCreditPolicyId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(CustomerCreditPolicyId? left, CustomerCreditPolicyId? right) =>
        Equals(left, right);

    public static bool operator !=(CustomerCreditPolicyId? left, CustomerCreditPolicyId? right) =>
        !Equals(left, right);
}
