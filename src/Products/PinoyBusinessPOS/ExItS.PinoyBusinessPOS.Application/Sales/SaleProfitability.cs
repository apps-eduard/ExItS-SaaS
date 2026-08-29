using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Sales;

/// <summary>Derived sale profitability from immutable checkout snapshots. Complete COGS only.</summary>
public static class SaleProfitability
{
    public sealed record Result(decimal GrossProfit, decimal? GrossMarginPercent);

    public static Result? Compute(Sale sale)
    {
        if (sale.CostStatus != ProductionCostStatus.Complete || sale.TotalCostSnapshot is null)
        {
            return null;
        }

        var grossProfit = SaleMoney.RoundMoney(sale.Total - sale.TotalCostSnapshot.Value);
        decimal? margin = sale.Total > 0m
            ? SaleMoney.RoundMoney(grossProfit / sale.Total * 100m)
            : null;

        return new Result(grossProfit, margin);
    }
}
