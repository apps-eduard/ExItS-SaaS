using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Credit;

public sealed class CreditDueDateChangeId : IEquatable<CreditDueDateChangeId>
{
    public Guid Value { get; }

    private CreditDueDateChangeId(Guid value) => Value = value;

    public static CreditDueDateChangeId New() => new(Guid.NewGuid());

    public static CreditDueDateChangeId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCreditDueDateChangeId,
                "CreditDueDateChangeId cannot be an empty GUID.");
        }

        return new CreditDueDateChangeId(value);
    }

    public bool Equals(CreditDueDateChangeId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is CreditDueDateChangeId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(CreditDueDateChangeId? left, CreditDueDateChangeId? right) => Equals(left, right);

    public static bool operator !=(CreditDueDateChangeId? left, CreditDueDateChangeId? right) => !Equals(left, right);
}
