using ExItS.PinoyBusinessPOS.Application.OperationalSetup;
using ExItS.PinoyBusinessPOS.Domain.OperationalSetup;

namespace ExItS.PinoyBusinessPOS.UnitTests.OperationalSetup;

public sealed class OperationalSetupTaxCalculatorTests
{
    [Theory]
    [InlineData(100, 12, TaxPricingMode.TaxExclusive, 12)]
    [InlineData(100, 12, TaxPricingMode.TaxInclusive, 10.71)]
    [InlineData(100, 0, TaxPricingMode.TaxExclusive, 0)]
    public void ComputeTaxAmount_returns_expected_values(
        decimal subtotal,
        decimal rate,
        TaxPricingMode mode,
        decimal expected) =>
        Assert.Equal(expected, OperationalSetupTaxCalculator.ComputeTaxAmount(subtotal, rate, mode));
}
