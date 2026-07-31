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
/// Password must come from configuration / environment — never commit real secrets.
/// </summary>
public sealed class PlatformAuthBootstrapOptions
{
    public const string SectionName = "PlatformAuthentication:Bootstrap";

    public bool Enabled { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
