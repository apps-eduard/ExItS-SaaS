using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>
/// Where a commercial discount attaches: a single sale line, or the whole sale (then allocated
/// proportionally across eligible lines).
/// </summary>
public enum SaleDiscountScope
{
    Line = 0,
    Sale = 1
}

/// <summary>How the discount amount is expressed by the operator.</summary>
public enum SaleDiscountMethod
{
    Percentage = 0,
    FixedAmount = 1
}

/// <summary>
/// Origin of a commercial discount. Only <see cref="Manual"/> exists today: an authorized operator
/// typed a value and a reason. Promotion engines and statutory/regulatory discounts are deliberately
/// separate concepts and are not represented here.
/// </summary>
public enum SaleDiscountSource
{
    Manual = 0
}

/// <summary>
/// One requested commercial discount. This is an intent, not an outcome: the peso amount is always
/// computed server-side by <see cref="SaleCommercialDiscountCalculator"/> from
/// <see cref="Value"/> against the eligible base.
///
/// A commercial discount is none of the following, and must never be conflated with them:
/// today's price (catalog pricing), a cashier price override (changes UnitPrice), a promotion
/// (rule-driven), or a regulatory discount (statutory, with its own reporting rules). It reduces
/// money only — never <see cref="SaleLine.UnitPrice"/> and never inventory quantity.
///
/// For <see cref="SaleDiscountScope.Line"/> the target line is identified by
/// <see cref="LineNumber"/> when supplied, otherwise by <see cref="ProductId"/>.
/// </summary>
public sealed record CommercialDiscountIntent(
    SaleDiscountScope Scope,
    SaleDiscountMethod Method,
    decimal Value,
    string Reason,
    CatalogProductId? ProductId = null,
    int? LineNumber = null)
{
    public SaleDiscountSource Source => SaleDiscountSource.Manual;
}

/// <summary>Shared validation limits and normalization for commercial sale discounts.</summary>
public static class SaleCommercialDiscountRules
{
    public const decimal MaxPercentage = 100m;
    public const int ReasonMaxLength = 512;
    public const int MaxIntentCount = 50;

    /// <summary>Trims a required operator reason. Whitespace-only reasons are rejected.</summary>
    public static string NormalizeReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException(
                DomainErrorCodes.SaleDiscountReasonRequired,
                "A commercial discount requires a reason.");
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > ReasonMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.SaleDiscountReasonRequired,
                $"A commercial discount reason must be at most {ReasonMaxLength} characters.");
        }

        return trimmed;
    }

    /// <summary>Validates a percentage in (0, 100].</summary>
    public static decimal NormalizePercentage(decimal percentage)
    {
        if (percentage <= 0m || percentage > MaxPercentage)
        {
            throw new DomainException(
                DomainErrorCodes.SaleDiscountInvalidPercent,
                $"A commercial discount percentage must be greater than 0 and at most {MaxPercentage}.");
        }

        if (!SaleMoney.HasAtMostDecimals(percentage, SaleMoney.MonetaryDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.SaleDiscountInvalidPercent,
                "A commercial discount percentage may have at most 2 decimal places.");
        }

        return percentage;
    }

    /// <summary>Validates a fixed peso amount greater than zero with at most two decimal places.</summary>
    public static decimal NormalizeFixedAmount(decimal amount)
    {
        if (amount <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.SaleDiscountInvalidAmount,
                "A fixed commercial discount amount must be greater than zero.");
        }

        if (amount > Sale.MaxTotal)
        {
            throw new DomainException(
                DomainErrorCodes.SaleDiscountInvalidAmount,
                "The commercial discount amount is too large.");
        }

        if (!SaleMoney.HasAtMostDecimals(amount, SaleMoney.MonetaryDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.SaleDiscountInvalidAmount,
                "A fixed commercial discount amount must have at most 2 decimal places.");
        }

        return amount;
    }

    public static string ToCode(SaleDiscountScope scope) => scope.ToString();

    public static string ToCode(SaleDiscountMethod method) => method.ToString();

    public static string ToCode(SaleDiscountSource source) => source.ToString();

    public static SaleDiscountScope ParseScope(string? value)
    {
        if (!Enum.TryParse<SaleDiscountScope>(value, ignoreCase: true, out var parsed))
        {
            throw new DomainException(
                DomainErrorCodes.SaleDiscountInvalidScope,
                $"Unrecognized commercial discount scope '{value}'.");
        }

        return parsed;
    }

    public static SaleDiscountMethod ParseMethod(string? value)
    {
        if (!Enum.TryParse<SaleDiscountMethod>(value, ignoreCase: true, out var parsed))
        {
            throw new DomainException(
                DomainErrorCodes.SaleDiscountInvalidMethod,
                $"Unrecognized commercial discount method '{value}'.");
        }

        return parsed;
    }

    public static SaleDiscountSource ParseSource(string? value)
    {
        if (!Enum.TryParse<SaleDiscountSource>(value, ignoreCase: true, out var parsed))
        {
            throw new DomainException(
                DomainErrorCodes.SaleDiscountInvalidSource,
                $"Unrecognized commercial discount source '{value}'.");
        }

        return parsed;
    }
}
