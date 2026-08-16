using ExItS.PinoyBusinessPOS.Application.OperationalSetup;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.OperationalSetup;
using ExItS.PinoyBusinessPOS.Domain.Registers;

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

    [Fact]
    public async Task DisabledTaxCapabilityDoesNotApplyTax()
    {
        var reader = new FakeTaxReader(enabled: false);
        var orgId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var actorId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var utc = new DateTimeOffset(2026, 8, 16, 8, 0, 0, TimeSpan.Zero);

        var setup = PosOperationalSetup.CreateIncomplete(PosOrganizationId.From(orgId), actorId, utc);
        setup.Complete(
            "Corner Store",
            "PHP",
            TaxPricingMode.TaxExclusive,
            12m,
            null,
            null,
            null,
            null,
            RegisterId.New(),
            actorId,
            utc);

        var enabled = await reader.IsTaxConfigurationEnabledAsync(orgId);

        Assert.False(enabled);
        Assert.False(OperationalSetupTaxCalculator.ShouldApplyConfiguredTax(enabled, setup));

        // Checkout must skip ComputeTaxAmount when capability is disabled even if rate > 0.
        var taxAmount = OperationalSetupTaxCalculator.ShouldApplyConfiguredTax(enabled, setup)
            ? OperationalSetupTaxCalculator.ComputeTaxAmount(100m, setup.TaxRatePercent, setup.TaxPricingMode)
            : 0m;
        Assert.Equal(0m, taxAmount);

        Assert.True(OperationalSetupTaxCalculator.ShouldApplyConfiguredTax(taxConfigurationEnabled: true, setup));
        Assert.Equal(
            12m,
            OperationalSetupTaxCalculator.ComputeTaxAmount(100m, setup.TaxRatePercent, setup.TaxPricingMode));
    }

    private sealed class FakeTaxReader(bool enabled) : IOrganizationTaxConfigurationCapabilityReader
    {
        public Task<bool> IsTaxConfigurationEnabledAsync(
            Guid organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(enabled);
    }
}
