namespace ExItS.DesignSystem.Localization;

/// <summary>
/// Marker type used only as the generic argument for <c>IStringLocalizer&lt;DesignSystemResources&gt;</c>.
/// Localized strings live in <c>DesignSystemResources.resx</c> (en) and
/// <c>DesignSystemResources.fil-PH.resx</c> (fil-PH) in this same folder; there is no
/// Designer.cs so the resource keys below are the source of truth.
/// </summary>
/// <remarks>
/// Keys: Empty_*, Error_*, Loading_*, Search_*, Offline_*, ApiUnavailable_*, Timeout_*,
/// Action_*, Status_*, Empty_NoRecords, Loading_BusyAria, Validation_SummaryTitle,
/// Confirm_ReasonLabel, Data_*, Money_Unavailable.
/// </remarks>
public sealed class DesignSystemResources
{
    private DesignSystemResources()
    {
    }
}
