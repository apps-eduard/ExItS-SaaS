using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Expenses;

/// <summary>Strongly typed identifier for a flat POS expense category.</summary>
public sealed class ExpenseCategoryId : IEquatable<ExpenseCategoryId>
{
    public Guid Value { get; }

    private ExpenseCategoryId(Guid value) => Value = value;

    public static ExpenseCategoryId New() => new(Guid.NewGuid());

    public static ExpenseCategoryId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpenseCategoryId,
                "ExpenseCategoryId cannot be an empty GUID.");
        }

        return new ExpenseCategoryId(value);
    }

    public bool Equals(ExpenseCategoryId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is ExpenseCategoryId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(ExpenseCategoryId? left, ExpenseCategoryId? right) => Equals(left, right);

    public static bool operator !=(ExpenseCategoryId? left, ExpenseCategoryId? right) => !Equals(left, right);
}
