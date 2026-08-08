namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>Configurable offline operate-grant lifetime. PIN unlock never extends this window.</summary>
public sealed class OfflineOperatingGrantOptions
{
    public const string SectionName = "OfflineOperatingGrant";

    /// <summary>Hours after last online validation that offline operate remains allowed. Default 24.</summary>
    public int DurationHours { get; set; } = 24;

    /// <summary>Minimum PIN length (digits only).</summary>
    public int PinMinLength { get; set; } = 6;

    /// <summary>Failed PIN attempts before temporary lockout.</summary>
    public int MaxFailedPinAttempts { get; set; } = 5;

    /// <summary>Lockout duration after too many failed PIN attempts.</summary>
    public int PinLockoutMinutes { get; set; } = 15;

    /// <summary>PBKDF2 iterations for the local PIN verifier.</summary>
    public int PinHashIterations { get; set; } = 100_000;
}
