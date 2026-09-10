using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Credit;

/// <summary>Strongly typed identifier for an append-only customer credit policy change.</summary>
public sealed class CustomerCreditPolicyChangeId : IEquatable<CustomerCreditPolicyChangeId>
{
    public Guid Value { get; }

    private CustomerCreditPolicyChangeId(Guid value) => Value = value;

    public static CustomerCreditPolicyChangeId New() => new(Guid.NewGuid());

    public static CustomerCreditPolicyChangeId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerCreditPolicyChangeId,
                "CustomerCreditPolicyChangeId cannot be an empty GUID.");
        }

        return new CustomerCreditPolicyChangeId(value);
    }

    public bool Equals(CustomerCreditPolicyChangeId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is CustomerCreditPolicyChangeId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(CustomerCreditPolicyChangeId? left, CustomerCreditPolicyChangeId? right) =>
        Equals(left, right);

    public static bool operator !=(CustomerCreditPolicyChangeId? left, CustomerCreditPolicyChangeId? right) =>
        !Equals(left, right);
}
