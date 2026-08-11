using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Customers;

/// <summary>Strongly typed identifier for a POS customer. Not a Platform User identity.</summary>
public sealed class POSCustomerId : IEquatable<POSCustomerId>
{
    public Guid Value { get; }

    private POSCustomerId(Guid value) => Value = value;

    public static POSCustomerId New() => new(Guid.NewGuid());

    public static POSCustomerId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerId,
                "POSCustomerId cannot be an empty GUID.");
        }

        return new POSCustomerId(value);
    }

    public bool Equals(POSCustomerId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is POSCustomerId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(POSCustomerId? left, POSCustomerId? right) => Equals(left, right);

    public static bool operator !=(POSCustomerId? left, POSCustomerId? right) => !Equals(left, right);
}
