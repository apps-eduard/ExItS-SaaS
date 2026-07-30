using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Maui.Localization;
using Microsoft.Extensions.Localization;

namespace ExItS.PinoyBusinessPOS.Maui.Components.Pages.Suppliers;

internal readonly record struct SupplierFormErrors(
    string? Name = null,
    string? Email = null,
    string? Mobile = null,
    string? TaxNumber = null,
    string? Form = null);

/// <summary>
/// Maps supplier application error codes onto the field or form message that should be shown.
/// </summary>
internal static class SupplierErrorPresenter
{
    public static SupplierFormErrors Describe(string? errorCode, IStringLocalizer<PosResources> localizer) => errorCode switch
    {
        ApplicationErrorCodes.SupplierNameConflict => new SupplierFormErrors(
            Name: localizer["Suppliers_NameConflict"],
            Form: localizer["Suppliers_NameConflict"]),
        ApplicationErrorCodes.SupplierEmailConflict => new SupplierFormErrors(
            Email: localizer["Suppliers_EmailConflict"],
            Form: localizer["Suppliers_EmailConflict"]),
        ApplicationErrorCodes.SupplierMobileConflict => new SupplierFormErrors(
            Mobile: localizer["Suppliers_MobileConflict"],
            Form: localizer["Suppliers_MobileConflict"]),
        ApplicationErrorCodes.SupplierTaxConflict => new SupplierFormErrors(
            TaxNumber: localizer["Suppliers_TaxConflict"],
            Form: localizer["Suppliers_TaxConflict"]),
        ApplicationErrorCodes.SupplierConcurrencyConflict => new SupplierFormErrors(
            Form: localizer["Suppliers_ConcurrencyConflict"]),
        _ => default
    };
}
