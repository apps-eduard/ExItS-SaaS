using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class StockMovementPresentationTests
{
    [Theory]
    [InlineData(nameof(StockMovementType.ManualIncrease), "Stock added")]
    [InlineData(nameof(StockMovementType.ManualDecrease), "Stock removed")]
    public void Maps_manual_adjustment_codes_to_friendly_labels(string code, string expected)
    {
        Assert.Equal(expected, StockMovementPresentation.ToFriendlyLabel(code));
    }

    [Fact]
    public void Does_not_change_persistence_enum_names()
    {
        Assert.Equal("ManualIncrease", StockMovementTypes.ToCode(StockMovementType.ManualIncrease));
        Assert.Equal("ManualDecrease", StockMovementTypes.ToCode(StockMovementType.ManualDecrease));
        Assert.True(StockMovementTypes.TryParse("ManualIncrease", out var increase));
        Assert.Equal(StockMovementType.ManualIncrease, increase);
    }

    [Fact]
    public void Unknown_codes_are_returned_unchanged()
    {
        Assert.Equal("CustomType", StockMovementPresentation.ToFriendlyLabel("CustomType"));
        Assert.Equal(string.Empty, StockMovementPresentation.ToFriendlyLabel("  "));
    }
}
