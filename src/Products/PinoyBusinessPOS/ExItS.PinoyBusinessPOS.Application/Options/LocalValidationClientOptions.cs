namespace ExItS.PinoyBusinessPOS.Application.Options;

/// <summary>
/// MAUI Local Validation Quick Login: SharedPassword + normal Platform login (non-Production only).
/// </summary>
public sealed class LocalValidationClientOptions
{
    public const string SectionName = "LocalValidation";

    public bool Enabled { get; init; }

    /// <summary>Must match Platform LocalValidation:SharedPassword (min 12 chars).</summary>
    public string SharedPassword { get; init; } = string.Empty;

    /// <summary>
    /// Optional Tailscale/LAN PublicHost for PhysicalDevice Local Validation builds (informational).
    /// Not used in Production.
    /// </summary>
    public string? PublicHost { get; init; }

    /// <summary>
    /// Host UI port for MAUI Local Validation Mailpit (default 8125).
    /// React Local Validation Mailpit is 8025 — do not confuse the two.
    /// </summary>
    public int MailpitUiPort { get; init; } = 8125;

    public bool IsQuickLoginAvailable =>
        Enabled
        && !string.IsNullOrWhiteSpace(SharedPassword)
        && SharedPassword.Length >= 12;
}
