using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.OperationalSetup;

public sealed class OrganizationCashDenominationId : IEquatable<OrganizationCashDenominationId>
{
    public Guid Value { get; }

    private OrganizationCashDenominationId(Guid value) => Value = value;

    public static OrganizationCashDenominationId New() => new(Guid.NewGuid());

    public static OrganizationCashDenominationId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCashDenominationId,
                "Cash denomination id cannot be an empty GUID.");
        }

        return new OrganizationCashDenominationId(value);
    }

    public bool Equals(OrganizationCashDenominationId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is OrganizationCashDenominationId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(OrganizationCashDenominationId? left, OrganizationCashDenominationId? right) =>
        Equals(left, right);

    public static bool operator !=(OrganizationCashDenominationId? left, OrganizationCashDenominationId? right) =>
        !Equals(left, right);
}
