using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class WeightQuantitiesTests
{
    [Theory]
    [InlineData(1, WeightInputUnit.Kilogram, 1.000)]
    [InlineData(1.2, WeightInputUnit.Kilogram, 1.200)]
    [InlineData(350, WeightInputUnit.Gram, 0.350)]
    [InlineData(75, WeightInputUnit.Gram, 0.075)]
    public void NormalizeToKilograms_converts_supported_inputs(decimal value, WeightInputUnit unit, decimal expectedKg)
    {
        Assert.Equal(expectedKg, WeightQuantities.NormalizeToKilograms(value, unit));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NormalizeToKilograms_rejects_non_positive(decimal value)
    {
        var ex = Assert.Throws<DomainException>(
            () => WeightQuantities.NormalizeToKilograms(value, WeightInputUnit.Kilogram));
        Assert.Equal(DomainErrorCodes.InvalidWeightQuantity, ex.ErrorCode);
    }

    [Fact]
    public void NormalizeToKilograms_rejects_excessive_precision()
    {
        var ex = Assert.Throws<DomainException>(
            () => WeightQuantities.NormalizeToKilograms(1.2345m, WeightInputUnit.Kilogram));
        Assert.Equal(DomainErrorCodes.InvalidWeightQuantity, ex.ErrorCode);
    }

    [Fact]
    public void TryParseInputUnit_accepts_aliases()
    {
        Assert.True(WeightQuantities.TryParseInputUnit("g", out var grams));
        Assert.Equal(WeightInputUnit.Gram, grams);
        Assert.True(WeightQuantities.TryParseInputUnit("kg", out var kg));
        Assert.Equal(WeightInputUnit.Kilogram, kg);
        Assert.False(WeightQuantities.TryParseInputUnit("liter", out _));
    }
}
