using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Expenses;

/// <summary>
/// Monetary rules for store expenses. Amounts use 2 decimal places with
/// <see cref="MidpointRounding.AwayFromZero"/>, matching <c>CreditEntry</c> / <c>SaleMoney</c>.
/// </summary>
public static class ExpenseMoney
{
    public const int MonetaryDecimals = 2;
    public const decimal MaxAmount = 999_999_999.99m;

    public static decimal RoundMoney(decimal amount) =>
        decimal.Round(amount, MonetaryDecimals, MidpointRounding.AwayFromZero);

    public static bool HasAtMostDecimals(decimal value, int decimals) =>
        decimal.Round(value, decimals, MidpointRounding.ToZero) == value;

    /// <summary>Requires amount &gt; 0, ≤ <see cref="MaxAmount"/>, and at most two decimal places.</summary>
    public static decimal NormalizeAmount(decimal amount)
    {
        if (amount <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpenseAmount,
                "Expense amount must be a positive decimal.");
        }

        if (amount > MaxAmount)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpenseAmount,
                $"Expense amount must be at most {MaxAmount}.");
        }

        var rounded = RoundMoney(amount);
        if (rounded != amount)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpenseAmount,
                "Expense amount may have at most two decimal places.");
        }

        return rounded;
    }

    internal static void EnsureUtc(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamp must be UTC.");
        }
    }

    internal static void EnsureActor(Guid actorId)
    {
        if (actorId == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpenseActor,
                "A non-empty actor identifier is required for expense recording and voiding.");
        }
    }
}
