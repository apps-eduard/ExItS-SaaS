using ExItS.DesignSystem.Components.Primitives;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Maui.Localization;
using Microsoft.Extensions.Localization;

namespace ExItS.PinoyBusinessPOS.Maui.Components.Pages.Inventory;

/// <summary>
/// Localized labels for inventory adjustment, movement history, and stock counts.
/// Stable codes stay on the Application/domain layer; only display text is resolved here.
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

    public static string StockCountStatusLabel(IStringLocalizer<PosResources> localizer, string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return string.Empty;
        }

        var code = status.Trim();
        var localized = localizer[$"Inventory_CountsStatus_{code}"];
        return localized.ResourceNotFound ? code : localized.Value;
    }

    public static string? StockCountDifferenceCss(decimal? variance)
    {
        if (variance is null || variance.Value == 0m)
        {
            return null;
        }

        return variance.Value > 0m
            ? "pos-stock-count-detail__difference--up"
            : "pos-stock-count-detail__difference--down";
    }

    public static IReadOnlyList<SelectOption> StockCountTitleOptions(IStringLocalizer<PosResources> localizer)
    {
        var options = StockCountDisplay.PresetTitles
            .Select(title => new SelectOption(title, title))
            .ToList();
        options.Add(new SelectOption(StockCountDisplay.CustomPresetValue, localizer["Inventory_CountsTitleCustom"]));
        return options;
    }
}
