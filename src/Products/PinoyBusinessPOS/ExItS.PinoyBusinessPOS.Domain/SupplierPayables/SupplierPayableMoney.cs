using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.SupplierPayables;

/// <summary>
/// Monetary rules for supplier payables. Amounts use numeric(18,2) with
/// <see cref="MidpointRounding.AwayFromZero"/>, matching POS sale/expense money.
/// </summary>
public static class SupplierPayableMoney
{
    public const int MonetaryDecimals = 2;
    public const decimal MaxAmount = 999_999_999.99m;

    public static decimal RoundMoney(decimal amount) =>
        decimal.Round(amount, MonetaryDecimals, MidpointRounding.AwayFromZero);

    public static decimal NormalizeNonNegativeAmount(decimal amount, string errorCode, string fieldLabel)
    {
        if (amount < 0m)
        {
            throw new DomainException(errorCode, $"{fieldLabel} cannot be negative.");
        }

        if (amount > MaxAmount)
        {
            throw new DomainException(errorCode, $"{fieldLabel} must be at most {MaxAmount}.");
        }

        var rounded = RoundMoney(amount);
        if (rounded != amount)
        {
            throw new DomainException(errorCode, $"{fieldLabel} may have at most two decimal places.");
        }

        return rounded;
    }

    public static decimal NormalizePositiveAmount(decimal amount, string errorCode, string fieldLabel)
    {
        var normalized = NormalizeNonNegativeAmount(amount, errorCode, fieldLabel);
        if (normalized <= 0m)
        {
            throw new DomainException(errorCode, $"{fieldLabel} must be a positive decimal.");
        }

        return normalized;
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
                DomainErrorCodes.InvalidSupplierPayableActor,
                "A non-empty actor identifier is required.");
        }
    }
}
