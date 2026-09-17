using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Credit;

/// <summary>Strongly typed identifier for a B2B business credit entry. Not a SaaS payment.</summary>
public sealed class BusinessCreditEntryId : IEquatable<BusinessCreditEntryId>
{
    public Guid Value { get; }

    private BusinessCreditEntryId(Guid value) => Value = value;

    public static BusinessCreditEntryId New() => new(Guid.NewGuid());

    public static BusinessCreditEntryId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidBusinessCreditEntryId,
                "BusinessCreditEntryId cannot be an empty GUID.");
        }

        return new BusinessCreditEntryId(value);
    }

    public bool Equals(BusinessCreditEntryId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is BusinessCreditEntryId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(BusinessCreditEntryId? left, BusinessCreditEntryId? right) => Equals(left, right);

    public static bool operator !=(BusinessCreditEntryId? left, BusinessCreditEntryId? right) => !Equals(left, right);
}
