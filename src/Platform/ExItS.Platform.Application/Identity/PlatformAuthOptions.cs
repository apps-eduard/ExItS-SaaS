namespace ExItS.Platform.Application.Identity;

public sealed class PlatformPasswordOptions
{
    public const string SectionName = "PlatformAuthentication:Password";

    public int MinimumLength { get; set; } = 12;
    public int MaximumLength { get; set; } = 128;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireDigit { get; set; } = true;
    public bool RequireNonAlphanumeric { get; set; } = true;
}

public sealed class PlatformLockoutOptions
{
    public const string SectionName = "PlatformAuthentication:Lockout";

    public int MaxFailedAccessAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
}

/// <summary>
/// One-time first Platform Administrator bootstrap. Disabled by default.
/// Requires a shared secret header when enabled. Must never be enabled in Production.
/// Password and SharedSecret must come from configuration / environment — never commit real secrets.
/// </summary>
public sealed class PlatformAuthBootstrapOptions
{
    public const string SectionName = "PlatformAuthentication:Bootstrap";
    public const string SharedSecretHeaderName = "X-ExItS-Bootstrap-Secret";
    public const int MinimumSharedSecretLength = 32;

    public bool Enabled { get; set; }
    public string SharedSecret { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>Browser session lifetimes and cookie/header names for Platform interactive auth.</summary>
public sealed class PlatformSessionOptions
{
    public const string SectionName = "PlatformAuthentication:Session";

    public string CookieName { get; set; } = ".ExItS.Platform.Auth";
    public string SessionTokenHeaderName { get; set; } = "X-ExItS-Session-Token";
    public int IdleTimeoutMinutes { get; set; } = 30;
    public int AbsoluteLifetimeHours { get; set; } = 12;
    public bool SlidingRenewal { get; set; } = true;
}

/// <summary>
/// Password-reset and email-verification token lifetimes.
/// Email delivery is an explicit boundary — tokens are created and hashed; outbound delivery is optional/no-op without a vendor.
/// </summary>
public sealed class PlatformCredentialLifecycleOptions
{
    public const string SectionName = "PlatformAuthentication:Lifecycle";

    public int PasswordResetTokenLifetimeMinutes { get; set; } = 60;
    public int EmailVerificationTokenLifetimeHours { get; set; } = 24;

    /// <summary>
    /// When true (Development/Testing only), auth workflow responses may include a debug token for local verification.
    /// Must remain false in Production (startup rejects otherwise).
    /// </summary>
    public bool ExposeDebugTokens { get; set; }
}

/// <summary>
/// Optional SMTP delivery for auth outbound messages (Local Validation Mailpit / production provider).
/// When SmtpHost is empty, the null sink is used (tokens still issued).
/// </summary>
public sealed class PlatformEmailDeliveryOptions
{
    public const string SectionName = "PlatformEmail";

    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 1025;
    public bool UseSsl { get; set; }
    public string FromAddress { get; set; } = "noreply@exits.local";
    public string FromDisplayName { get; set; } = "ExItS";
    /// <summary>Public Admin base URL used to build verification / reset links (e.g. http://localhost:8090).</summary>
    public string? AdminPublicBaseUrl { get; set; }

    /// <summary>
    /// Explicit Pinoy Loan Manager Browser/PWA public origin for EmailVerification and PasswordReset only.
    /// Never derived from request Host/Origin/Referer.
    /// </summary>
    public string? PinoyLoanManagerPublicBaseUrl { get; set; }

    /// <summary>
    /// When true, http://localhost and http://127.0.0.1 are allowed for the PLM public base URL (Local Validation).
    /// Production must keep this false so only HTTPS PLM origins are accepted.
    /// </summary>
    public bool AllowHttpLoopbackPublicUrls { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(SmtpHost)
        && SmtpPort > 0
        && !string.IsNullOrWhiteSpace(FromAddress)
        && !string.IsNullOrWhiteSpace(AdminPublicBaseUrl);
}
