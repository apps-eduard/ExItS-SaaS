using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Maui.Localization;
using Microsoft.Extensions.Localization;

namespace ExItS.PinoyBusinessPOS.Maui.Components.Pages.Catalog;

internal readonly record struct CatalogFormErrors(
    string? Name = null,
    string? Sku = null,
    string? Barcode = null,
    string? Price = null,
    string? Form = null);

/// <summary>
/// Maps catalog application error codes onto the field or form message that should be shown.
/// </summary>
internal static class CatalogErrorPresenter
{
    public static CatalogFormErrors Describe(string? errorCode, IStringLocalizer<PosResources> localizer) => errorCode switch
    {
        ApplicationErrorCodes.ProductSkuConflict => new CatalogFormErrors(
            Sku: localizer["Catalog_SkuConflict"],
            Form: localizer["Catalog_SkuConflict"]),
        ApplicationErrorCodes.ProductBarcodeConflict => new CatalogFormErrors(
            Barcode: localizer["Catalog_BarcodeConflict"],
            Form: localizer["Catalog_BarcodeConflict"]),
        ApplicationErrorCodes.CategoryNameConflict => new CatalogFormErrors(
            Name: localizer["Catalog_Category_NameConflict"],
            Form: localizer["Catalog_Category_NameConflict"]),
        ApplicationErrorCodes.CategoryNotAssignable => new CatalogFormErrors(
            Form: localizer["Catalog_CategoryNotAssignable"]),
        ApplicationErrorCodes.CatalogConcurrencyConflict => new CatalogFormErrors(
            Form: localizer["Catalog_ConcurrencyConflict"]),
        _ => default
    };
}
