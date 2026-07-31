namespace ExItS.PinoyBusinessPOS.Api.Common;

/// <summary>Configuration for Platform opaque-token introspection used by POS API bearer auth.</summary>
public sealed class PlatformAuthOptions
{
    public const string SectionName = "PlatformAuth";

    /// <summary>Base URL of the Platform API (e.g. http://localhost:5288). Empty disables introspection.</summary>
    public string BaseUrl { get; set; } = string.Empty;
}
