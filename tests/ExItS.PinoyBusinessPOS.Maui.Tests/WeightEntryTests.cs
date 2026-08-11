using ExItS.PinoyBusinessPOS.Application.Sales;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class WeightEntryTests
{
    [Theory]
    [InlineData(350, "g", 0.350)]
    [InlineData(75, "g", 0.075)]
    [InlineData(1.2, "kg", 1.200)]
    [InlineData(0.001, "kg", 0.001)]
    [InlineData(1, "g", 0.001)]
    public void TryNormalize_converts_to_canonical_kg(decimal raw, string unit, decimal expectedKg)
    {
        Assert.True(WeightEntry.TryNormalize(raw, unit, out var kg, out var error));
        Assert.Null(error);
        Assert.Equal(expectedKg, kg);
    }

    [Theory]
    [InlineData(null, "g")]
    [InlineData(0.0, "g")]
    [InlineData(-100.0, "g")]
    public void TryNormalize_rejects_zero_or_negative(double? raw, string unit)
    {
        decimal? value = raw is null ? null : (decimal)raw.Value;
        Assert.False(WeightEntry.TryNormalize(value, unit, out _, out var error));
        Assert.Equal("zero", error);
    }

    [Fact]
    public void TryNormalize_rejects_over_precision_kilograms()
    {
        Assert.False(WeightEntry.TryNormalize(1.2345m, "kg", out _, out var error));
        Assert.Equal("precision", error);
    }

    [Fact]
    public void TryNormalize_rejects_gram_result_over_three_decimals()
    {
        // 1.5 g → 0.0015 kg (4 dp after /1000) → rejected
        Assert.False(WeightEntry.TryNormalize(1.5m, "g", out _, out var error));
        Assert.Equal("precision", error);
    }

    [Fact]
    public void IsByWeight_detects_mode()
    {
        Assert.True(WeightEntry.IsByWeight("ByWeight"));
        Assert.False(WeightEntry.IsByWeight("PerItem"));
        Assert.False(WeightEntry.IsByWeight(null));
    }

    [Fact]
    public void FormatKilograms_trims_trailing_zeros()
    {
        Assert.Equal("0.35", WeightEntry.FormatKilograms(0.350m));
        Assert.Equal("1.2", WeightEntry.FormatKilograms(1.200m));
        Assert.Equal("2", WeightEntry.FormatKilograms(2m));
    }

    [Fact]
    public void Mixed_cart_line_math_matches_wp09_examples()
    {
        var coke = PosSaleOptions.RoundMoney(25m * 2m);
        Assert.True(WeightEntry.TryNormalize(1.2m, "kg", out var tomatoKg, out _));
        var tomato = PosSaleOptions.RoundMoney(120m * tomatoKg);
        Assert.Equal(50.00m, coke);
        Assert.Equal(144.00m, tomato);
        Assert.Equal(194.00m, PosSaleOptions.RoundMoney(coke + tomato));

        Assert.True(WeightEntry.TryNormalize(350m, "g", out var partialKg, out _));
        Assert.Equal(0.350m, partialKg);
        Assert.Equal(42.00m, PosSaleOptions.RoundMoney(120m * partialKg));
    }
}
