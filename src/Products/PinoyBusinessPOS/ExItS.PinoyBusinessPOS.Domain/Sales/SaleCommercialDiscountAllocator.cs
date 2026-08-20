using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>
/// Spreads one sale-level commercial discount across the eligible line bases.
///
/// Remainder rule (largest remainder, deterministic): each line first receives its exact
/// proportional share truncated toward zero at two decimal places, so the sum of the truncated
/// shares is never more than the discount. The leftover centavos are then handed out one centavo at
/// a time to the lines with the largest discarded fraction; ties — including the all-equal case that
/// a two-line 50/50 split produces — are broken by the lower <c>LineNumber</c> first. A line never
/// receives more than its own eligible base, so a net line total can never go negative. When every
/// line has reached its base and centavos still remain, allocation stops: the caller records the
/// amount actually allocated, so the per-line allocations always reconcile exactly to the recorded
/// sale-level discount total.
/// </summary>
public static class SaleCommercialDiscountAllocator
{
    private const decimal OneCentavo = 0.01m;

    /// <summary>
    /// Allocates <paramref name="amount"/> across <paramref name="eligibleBases"/>, which must be
    /// ordered by line number ascending. Non-positive bases receive nothing. The returned array is
    /// positionally aligned with the input.
    /// </summary>
    public static decimal[] Allocate(IReadOnlyList<decimal> eligibleBases, decimal amount)
    {
        ArgumentNullException.ThrowIfNull(eligibleBases);

        var allocations = new decimal[eligibleBases.Count];
        var totalBase = SaleMoney.RoundMoney(eligibleBases.Where(b => b > 0m).Sum());
        if (amount <= 0m || totalBase <= 0m)
        {
            return allocations;
        }

        if (amount > totalBase)
        {
            throw new DomainException(
                DomainErrorCodes.SaleDiscountExceedsEligible,
                $"A sale-level commercial discount of {amount} exceeds the eligible base of {totalBase}.");
        }

        var discardedFractions = new decimal[eligibleBases.Count];
        var truncatedTotal = 0m;
        for (var i = 0; i < eligibleBases.Count; i++)
        {
            if (eligibleBases[i] <= 0m)
            {
                continue;
            }

            var exactShare = amount * eligibleBases[i] / totalBase;
            var truncated = decimal.Round(exactShare, SaleMoney.MonetaryDecimals, MidpointRounding.ToZero);
            allocations[i] = truncated;
            discardedFractions[i] = exactShare - truncated;
            truncatedTotal += truncated;
        }

        var leftoverCentavos = (int)decimal.Round(
            (amount - truncatedTotal) / OneCentavo,
            0,
            MidpointRounding.AwayFromZero);
        if (leftoverCentavos <= 0)
        {
            return allocations;
        }

        var order = Enumerable.Range(0, eligibleBases.Count)
            .Where(i => eligibleBases[i] > 0m)
            .OrderByDescending(i => discardedFractions[i])
            .ThenBy(i => i)
            .ToArray();

        while (leftoverCentavos > 0)
        {
            var placedThisPass = false;
            foreach (var i in order)
            {
                if (leftoverCentavos == 0)
                {
                    break;
                }

                if (allocations[i] + OneCentavo > eligibleBases[i])
                {
                    continue;
                }

                allocations[i] += OneCentavo;
                leftoverCentavos--;
                placedThisPass = true;
            }

            if (!placedThisPass)
            {
                break;
            }
        }

        return allocations;
    }
}
