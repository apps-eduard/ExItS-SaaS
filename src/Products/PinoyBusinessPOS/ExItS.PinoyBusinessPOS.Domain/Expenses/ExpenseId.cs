using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Expenses;

/// <summary>Strongly typed identifier for a POS expense. Not a Platform identity.</summary>
public sealed class ExpenseId : IEquatable<ExpenseId>
{
    public Guid Value { get; }

    private ExpenseId(Guid value) => Value = value;

    public static ExpenseId New() => new(Guid.NewGuid());

    public static ExpenseId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidExpenseId, "ExpenseId cannot be an empty GUID.");
        }

        return new ExpenseId(value);
    }

    public bool Equals(ExpenseId? other) => other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is ExpenseId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(ExpenseId? left, ExpenseId? right) => Equals(left, right);

    public static bool operator !=(ExpenseId? left, ExpenseId? right) => !Equals(left, right);
}
