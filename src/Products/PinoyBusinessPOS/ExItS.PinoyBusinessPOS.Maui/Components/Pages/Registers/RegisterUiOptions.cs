using ExItS.DesignSystem.Components.Primitives;
using ExItS.PinoyBusinessPOS.Maui.Localization;
using Microsoft.Extensions.Localization;

namespace ExItS.PinoyBusinessPOS.Maui.Components.Pages.Registers;

internal static class RegisterUiOptions
{
    public const string ActiveStatus = "Active";
    public const string InactiveStatus = "Inactive";

    public static IReadOnlyList<SelectOption> StatusFilters(IStringLocalizer<PosResources> localizer) =>
    [
        new(string.Empty, localizer["Registers_Filter_All"].Value),
        new(ActiveStatus, localizer["Registers_Status_Active"].Value),
        new(InactiveStatus, localizer["Registers_Status_Inactive"].Value)
    ];
}
