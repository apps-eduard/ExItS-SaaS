namespace ExItS.Platform.Infrastructure.Persistence.Identity;

internal sealed class PlatformUserCredentialRecord
{
    public Guid UserId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordHashAlgorithm { get; set; } = string.Empty;
    public string SecurityStamp { get; set; } = string.Empty;
    public DateTimeOffset PasswordChangedAtUtc { get; set; }
    public DateTimeOffset? EmailVerifiedAtUtc { get; set; }
    public string? PendingRecoveryNormalizedEmail { get; set; }
    public string? RecoveryNormalizedEmail { get; set; }
    public DateTimeOffset? RecoveryEmailVerifiedAtUtc { get; set; }
    public DateTimeOffset? RecoveryEmailPromptSkippedAtUtc { get; set; }
    public int FailedAccessCount { get; set; }
    public DateTimeOffset? LockoutEndUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
