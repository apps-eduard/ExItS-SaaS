namespace ExItS.DesignSystem.Localization;

/// <summary>
/// Marker type used only as the generic argument for <c>IStringLocalizer&lt;DesignSystemResources&gt;</c>.
/// Localized strings live in <c>DesignSystemResources.resx</c> (en) and
/// <c>DesignSystemResources.fil-PH.resx</c> (fil-PH) in this same folder; there is no
/// Designer.cs so the resource keys below are the source of truth.
/// </summary>
/// <remarks>
/// Keys: Empty_DefaultTitle, Empty_DefaultMessage, Error_DefaultTitle, Error_DefaultMessage,
/// Error_Retry, Loading_Label, Search_Placeholder, Offline_Title, Offline_Message,
/// ApiUnavailable_Title, ApiUnavailable_Message, Timeout_Title, Timeout_Message.
/// </remarks>
public sealed class DesignSystemResources
{
    private DesignSystemResources()
    {
    }
}
