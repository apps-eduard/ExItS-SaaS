namespace ExItS.Platform.Admin.Services;

/// <summary>
/// Admin-side Local Validation convenience options (non-Production only).
/// SharedPassword is used only server-side for normal Platform /auth/login — never rendered to the browser.
/// </summary>
public sealed class LocalValidationAdminOptions
{
    public const string SectionName = "LocalValidation";

    public bool Enabled { get; set; }

    /// <summary>Must match Platform LocalValidation:SharedPassword. Env-supplied only.</summary>
    public string SharedPassword { get; set; } = string.Empty;
}
