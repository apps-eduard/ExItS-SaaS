using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

/// <summary>Strongly typed identifier for a customer order. Not a Platform identity.</summary>
public sealed class CustomerOrderId : IEquatable<CustomerOrderId>
{
    public Guid Value { get; }

    private CustomerOrderId(Guid value) => Value = value;

    public static CustomerOrderId New() => new(Guid.NewGuid());

    public static CustomerOrderId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderId,
                "CustomerOrderId cannot be an empty GUID.");
        }

        return new CustomerOrderId(value);
    }

    public bool Equals(CustomerOrderId? other) => other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is CustomerOrderId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(CustomerOrderId? left, CustomerOrderId? right) => Equals(left, right);

    public static bool operator !=(CustomerOrderId? left, CustomerOrderId? right) => !Equals(left, right);
}
