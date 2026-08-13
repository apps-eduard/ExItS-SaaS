using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.CashierShifts;

/// <summary>
/// Historical snapshot of one denomination quantity for an opening or closing cash count.
/// Values are frozen at count time; later organization config changes do not rewrite them.
/// Line totals are supporting/audit data only — they never replace OpeningCashAmount/ClosingCashAmount.
/// </summary>
public sealed record CashCountDenominationLine(decimal DenominationValue, int Quantity)
{
    public decimal LineTotal => SaleMoney.RoundMoney(DenominationValue * Quantity);

    public static CashCountDenominationLine Create(decimal denominationValue, int quantity)
    {
        if (denominationValue <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCashDenominationValue,
                "Denomination value must be greater than zero.");
        }

        if (!SaleMoney.HasAtMostDecimals(denominationValue, SaleMoney.MonetaryDecimals))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCashDenominationValue,
                "Denomination value must have at most 2 decimal places.");
        }

        if (quantity < 0)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCashDenominationQuantity,
                "Denomination quantity cannot be negative.");
        }

        return new CashCountDenominationLine(SaleMoney.RoundMoney(denominationValue), quantity);
    }
}

public static class CashCountDenominationBreakdown
{
    public static IReadOnlyList<CashCountDenominationLine> Normalize(IReadOnlyList<CashCountDenominationLine>? lines)
    {
        if (lines is null || lines.Count == 0)
        {
            return Array.Empty<CashCountDenominationLine>();
        }

        var seen = new HashSet<decimal>();
        var normalized = new List<CashCountDenominationLine>(lines.Count);
        foreach (var line in lines)
        {
            var created = CashCountDenominationLine.Create(line.DenominationValue, line.Quantity);
            if (!seen.Add(created.DenominationValue))
            {
                throw new DomainException(
                    DomainErrorCodes.CashCountDenominationDuplicateLine,
                    "The same denomination value cannot appear twice in one count.");
            }

            normalized.Add(created);
        }

        return normalized;
    }

    public static decimal Recalculate(IReadOnlyList<CashCountDenominationLine> lines)
    {
        decimal total = 0m;
        foreach (var line in Normalize(lines))
        {
            total += line.LineTotal;
        }

        return SaleMoney.RoundMoney(total);
    }

    public static IReadOnlyList<CashCountDenominationLine> EnsureMatchesSubmittedTotal(
        decimal submittedTotal,
        IReadOnlyList<CashCountDenominationLine>? lines)
    {
        var normalized = Normalize(lines);
        if (normalized.Count == 0)
        {
            return normalized;
        }

        var calculated = Recalculate(normalized);
        if (calculated != SaleMoney.RoundMoney(submittedTotal))
        {
            throw new DomainException(
                DomainErrorCodes.CashCountDenominationTotalMismatch,
                "The denomination breakdown total does not match the submitted cash count.");
        }

        return normalized;
    }

    public static void EnsureConfigured(
        IReadOnlyList<CashCountDenominationLine> lines,
        IReadOnlySet<decimal> enabledDenominationValues)
    {
        foreach (var line in lines)
        {
            if (!enabledDenominationValues.Contains(line.DenominationValue))
            {
                throw new DomainException(
                    DomainErrorCodes.CashCountDenominationNotConfigured,
                    "Cashiers can only count denominations configured for this organization.");
            }
        }
    }
}
