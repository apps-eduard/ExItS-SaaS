using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Credit;

/// <summary>Strongly typed identifier for a remarks-based credit entry. Not a SaaS payment.</summary>
public sealed class CreditEntryId : IEquatable<CreditEntryId>
{
    public Guid Value { get; }

    private CreditEntryId(Guid value) => Value = value;

    public static CreditEntryId New() => new(Guid.NewGuid());

    public static CreditEntryId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCreditEntryId,
                "CreditEntryId cannot be an empty GUID.");
        }

        return new CreditEntryId(value);
    }

    public bool Equals(CreditEntryId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is CreditEntryId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(CreditEntryId? left, CreditEntryId? right) => Equals(left, right);

    public static bool operator !=(CreditEntryId? left, CreditEntryId? right) => !Equals(left, right);
}
