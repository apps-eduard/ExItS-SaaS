using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Maui.Localization;
using Microsoft.Extensions.Localization;

namespace ExItS.PinoyBusinessPOS.Maui.Components.Pages.Inventory;

/// <summary>
/// Localized labels for inventory adjustment and movement history. Stable codes stay on the
/// Application/domain layer; only display text is resolved here.
/// </summary>
internal static class InventoryUiOptions
{
    public static string MovementTypeLabel(IStringLocalizer<PosResources> localizer, string? movementType)
    {
        if (string.IsNullOrWhiteSpace(movementType))
        {
            return string.Empty;
        }

        var code = movementType.Trim();
        var localized = localizer[$"Inventory_Movement_{code}"];
        return localized.ResourceNotFound
            ? StockMovementPresentation.ToFriendlyLabel(code)
            : localized.Value;
    }
}
