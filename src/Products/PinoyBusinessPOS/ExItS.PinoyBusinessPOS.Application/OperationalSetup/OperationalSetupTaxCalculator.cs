using ExItS.PinoyBusinessPOS.Domain.OperationalSetup;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.OperationalSetup;

public static class OperationalSetupTaxCalculator
{
    public static decimal ComputeTaxAmount(decimal subtotal, decimal taxRatePercent, TaxPricingMode taxPricingMode)
    {
        if (taxRatePercent <= 0 || subtotal <= 0)
        {
            return 0m;
        }

        var rate = taxRatePercent / 100m;
        return taxPricingMode switch
        {
            TaxPricingMode.TaxExclusive => SaleMoney.RoundMoney(subtotal * rate),
            TaxPricingMode.TaxInclusive => SaleMoney.RoundMoney(subtotal - subtotal / (1 + rate)),
            _ => 0m
        };
    }
}
