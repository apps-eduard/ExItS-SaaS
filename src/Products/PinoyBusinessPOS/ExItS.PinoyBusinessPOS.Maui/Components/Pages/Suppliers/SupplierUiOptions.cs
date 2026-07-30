using ExItS.DesignSystem.Components.Primitives;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;
using ExItS.PinoyBusinessPOS.Maui.Localization;
using Microsoft.Extensions.Localization;

namespace ExItS.PinoyBusinessPOS.Maui.Components.Pages.Suppliers;

/// <summary>
/// Localized labels for supplier status filters and display. Stable codes come from the domain
/// layer; only the display text is localized here.
/// </summary>
internal static class SupplierUiOptions
{
    public const string ActiveStatus = nameof(SupplierStatus.Active);
    public const string InactiveStatus = nameof(SupplierStatus.Inactive);

    public static string StatusLabel(IStringLocalizer<PosResources> localizer, string? status) =>
        string.Equals(status, ActiveStatus, StringComparison.Ordinal)
            ? localizer["Suppliers_Status_Active"].Value
            : localizer["Suppliers_Status_Inactive"].Value;

    public static IReadOnlyList<SelectOption> StatusFilters(IStringLocalizer<PosResources> localizer) =>
    [
        new(string.Empty, localizer["Suppliers_Filter_AllStatuses"].Value),
        new(ActiveStatus, localizer["Suppliers_Status_Active"].Value),
        new(InactiveStatus, localizer["Suppliers_Status_Inactive"].Value)
    ];
}
