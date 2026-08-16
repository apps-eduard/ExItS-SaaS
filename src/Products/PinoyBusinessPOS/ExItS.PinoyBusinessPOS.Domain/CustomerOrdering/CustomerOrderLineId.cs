using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

/// <summary>Strongly typed identifier for a single line of a customer order.</summary>
public sealed class CustomerOrderLineId : IEquatable<CustomerOrderLineId>
{
    public Guid Value { get; }

    private CustomerOrderLineId(Guid value) => Value = value;

    public static CustomerOrderLineId New() => new(Guid.NewGuid());

    public static CustomerOrderLineId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderLineId,
                "CustomerOrderLineId cannot be an empty GUID.");
        }

        return new CustomerOrderLineId(value);
    }

    public bool Equals(CustomerOrderLineId? other) => other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is CustomerOrderLineId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(CustomerOrderLineId? left, CustomerOrderLineId? right) => Equals(left, right);

    public static bool operator !=(CustomerOrderLineId? left, CustomerOrderLineId? right) => !Equals(left, right);
}
