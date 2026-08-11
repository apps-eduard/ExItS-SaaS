using ExItS.DesignSystem.Components.Primitives;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Maui.Localization;
using Microsoft.Extensions.Localization;

namespace ExItS.PinoyBusinessPOS.Maui.Components.Pages.Catalog;

/// <summary>
/// Localized labels for the controlled catalog option sets. Stable codes come from the Application
/// layer; only the display text is localized here.
/// </summary>
internal static class CatalogUiOptions
{
    public static IReadOnlyList<SelectOption> UnitOfMeasures(IStringLocalizer<PosResources> localizer) =>
        PosCatalogOptions.UnitOfMeasureCodes
            .Select(code => new SelectOption(code, localizer[$"Catalog_Uom_{code}"].Value))
            .ToList();

    public static string UnitOfMeasureLabel(IStringLocalizer<PosResources> localizer, string? code) =>
        string.IsNullOrWhiteSpace(code) ? string.Empty : localizer[$"Catalog_Uom_{code}"].Value;

    public static IReadOnlyList<SelectOption> SellingModes(IStringLocalizer<PosResources> localizer) =>
        PosCatalogOptions.SellingModeCodes
            .Select(code => new SelectOption(code, localizer[$"Catalog_SellingMode_{code}"].Value))
            .ToList();

    public static string SellingModeLabel(IStringLocalizer<PosResources> localizer, string? code) =>
        string.IsNullOrWhiteSpace(code) ? string.Empty : localizer[$"Catalog_SellingMode_{code}"].Value;

    public static string StatusLabel(IStringLocalizer<PosResources> localizer, string? status) =>
        string.Equals(status, PosCatalogOptions.ActiveStatus, StringComparison.Ordinal)
            ? localizer["Catalog_Status_Active"].Value
            : localizer["Catalog_Status_Inactive"].Value;

    public static IReadOnlyList<SelectOption> StatusFilters(IStringLocalizer<PosResources> localizer) =>
    [
        new(string.Empty, localizer["Catalog_Filter_AllStatuses"].Value),
        new(PosCatalogOptions.ActiveStatus, localizer["Catalog_Status_Active"].Value),
        new(PosCatalogOptions.InactiveStatus, localizer["Catalog_Status_Inactive"].Value)
    ];

    public static IReadOnlyList<SelectOption> UnitOfMeasureFilters(IStringLocalizer<PosResources> localizer)
    {
        var options = new List<SelectOption>
        {
            new(string.Empty, localizer["Catalog_Filter_AllUnits"].Value)
        };
        options.AddRange(UnitOfMeasures(localizer));
        return options;
    }

    public static IReadOnlyList<SelectOption> CategoryChoices(
        IStringLocalizer<PosResources> localizer,
        IEnumerable<PosProductCategoryDto> categories)
    {
        var options = new List<SelectOption>
        {
            new(string.Empty, localizer["Catalog_Product_NoCategory"].Value)
        };
        options.AddRange(categories.Select(c => new SelectOption(c.CategoryId.ToString("D"), c.Name)));
        return options;
    }

    public static IReadOnlyList<SelectOption> CategoryFilters(
        IStringLocalizer<PosResources> localizer,
        IEnumerable<PosProductCategoryDto> categories)
    {
        var options = new List<SelectOption>
        {
            new(string.Empty, localizer["Catalog_Filter_AllCategories"].Value)
        };
        options.AddRange(categories.Select(c => new SelectOption(c.CategoryId.ToString("D"), c.Name)));
        return options;
    }

    public static IReadOnlyList<SelectOption> CheckoutCategoryFilters(
        IStringLocalizer<PosResources> localizer,
        IEnumerable<PosProductCategoryDto> categories)
    {
        var options = new List<SelectOption>
        {
            new(string.Empty, localizer["Sales_Checkout_AllCategories"].Value)
        };
        options.AddRange(categories.Select(c => new SelectOption(c.CategoryId.ToString("D"), c.Name)));
        return options;
    }
}
