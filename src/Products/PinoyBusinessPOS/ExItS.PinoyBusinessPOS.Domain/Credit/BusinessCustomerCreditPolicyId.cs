using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Credit;

/// <summary>Strongly typed identifier for a B2B business-customer credit policy.</summary>
public sealed class BusinessCustomerCreditPolicyId : IEquatable<BusinessCustomerCreditPolicyId>
{
    public Guid Value { get; }

    private BusinessCustomerCreditPolicyId(Guid value) => Value = value;

    public static BusinessCustomerCreditPolicyId New() => new(Guid.NewGuid());

    public static BusinessCustomerCreditPolicyId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBusinessCustomerCreditPolicyId,
                "BusinessCustomerCreditPolicyId cannot be an empty GUID.");
        }

        return new BusinessCustomerCreditPolicyId(value);
    }

    public bool Equals(BusinessCustomerCreditPolicyId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is BusinessCustomerCreditPolicyId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(BusinessCustomerCreditPolicyId? left, BusinessCustomerCreditPolicyId? right) =>
        Equals(left, right);

    public static bool operator !=(BusinessCustomerCreditPolicyId? left, BusinessCustomerCreditPolicyId? right) =>
        !Equals(left, right);
}
