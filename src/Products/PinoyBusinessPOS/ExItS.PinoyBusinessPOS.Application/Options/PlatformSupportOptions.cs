namespace ExItS.PinoyBusinessPOS.Application.Options;

/// <summary>
/// Shared-secret options for Platform → POS support read APIs.
/// Empty <see cref="ApiKey"/> denies all support calls.
/// </summary>
public sealed class PlatformSupportOptions
{
    public const string SectionName = "PlatformSupport";

    /// <summary>Shared API key. When blank, all platform-support endpoints deny access.</summary>
    public string ApiKey { get; set; } = string.Empty;
}
