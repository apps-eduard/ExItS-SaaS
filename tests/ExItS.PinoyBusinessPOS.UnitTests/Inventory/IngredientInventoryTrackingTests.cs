using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class IngredientInventoryTrackingTests
{
    [Fact]
    public void Validate_rejects_ingredient_without_tracking()
    {
        var result = IngredientInventoryTracking.Validate(canBeUsedAsIngredient: true, isTracked: false);
        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorCodes.IngredientRequiresTrackedInventory, result.ErrorCode);
        Assert.Equal(IngredientInventoryTracking.RequiresTrackedMessage, result.ErrorMessage);
    }

    [Fact]
    public void Validate_accepts_ingredient_with_tracking()
    {
        var result = IngredientInventoryTracking.Validate(canBeUsedAsIngredient: true, isTracked: true);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_allows_non_ingredient_untracked()
    {
        var result = IngredientInventoryTracking.Validate(canBeUsedAsIngredient: false, isTracked: false);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Production_messages_name_the_product()
    {
        Assert.Contains("Tomato", IngredientInventoryTracking.UntrackedProductionMaterialMessage("Tomato"));
        Assert.Contains("Pandesal", IngredientInventoryTracking.UntrackedProductionOutputMessage("Pandesal"));
    }
}
