using System.Net.Mail;
using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Identity;

/// <summary>
/// Platform User aggregate — global identity only.
/// Does not store passwords, tokens, MFA secrets, or product-local profiles.
/// </summary>
public sealed class PlatformUser
{
    private static readonly Regex DisplayNamePattern = new(
        @"^[\p{L}\p{N}][\p{L}\p{N} .'\-]{0,98}[\p{L}\p{N}.]?$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex UsernamePattern = new(
        @"^[a-z0-9][a-z0-9._-]{1,62}[a-z0-9]$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public PlatformUserId Id { get; }
    public string Username { get; private set; }
    public string NormalizedUsername { get; private set; }
    public string DisplayName { get; private set; }
    public string NormalizedEmail { get; private set; }
    public AccountStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? SuspendedAtUtc { get; private set; }
    public string? SuspensionReason { get; private set; }

    private PlatformUser(
        PlatformUserId id,
        string username,
        string normalizedUsername,
        string displayName,
        string normalizedEmail,
        AccountStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? suspendedAtUtc,
        string? suspensionReason)
    {
        Id = id;
        Username = username;
        NormalizedUsername = normalizedUsername;
        DisplayName = displayName;
        NormalizedEmail = normalizedEmail;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        SuspendedAtUtc = suspendedAtUtc;
        SuspensionReason = suspensionReason;
    }

    public static PlatformUser Create(
        string username,
        string displayName,
        string email,
        DateTimeOffset utcNow,
        PlatformUserId? id = null)
    {
        EnsureUtc(utcNow);
        var (displayUsername, normalizedUsername) = NormalizeUsername(username);
        var normalizedName = NormalizeDisplayName(displayName);
        var normalizedEmail = NormalizeEmail(email);

        return new PlatformUser(
            id ?? PlatformUserId.New(),
            displayUsername,
            normalizedUsername,
            normalizedName,
            normalizedEmail,
            AccountStatus.Active,
            utcNow,
            utcNow,
            null,
            null);
    }

    /// <summary>Rehydrate from persistence.</summary>
    public static PlatformUser Rehydrate(
        PlatformUserId id,
        string username,
        string normalizedUsername,
        string displayName,
        string normalizedEmail,
        AccountStatus status,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? suspendedAtUtc,
        string? suspensionReason) =>
        new(
            id,
            username,
            normalizedUsername,
            displayName,
            normalizedEmail,
            status,
            createdAtUtc,
            updatedAtUtc,
            suspendedAtUtc,
            suspensionReason);

    public void UpdateProfile(string displayName, string email, DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        EnsureNotDeactivated();

        DisplayName = NormalizeDisplayName(displayName);
        NormalizedEmail = NormalizeEmail(email);
        UpdatedAtUtc = utcNow;
    }

    public void Suspend(DateTimeOffset utcNow, string? reason = null)
    {
        EnsureUtc(utcNow);
        TransitionTo(AccountStatus.Suspended, utcNow);
        SuspendedAtUtc = utcNow;
        SuspensionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public void Reactivate(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (Status == AccountStatus.Deactivated)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidAccountStatusTransition,
                "A deactivated Platform User cannot be reactivated.");
        }

        TransitionTo(AccountStatus.Active, utcNow);
        SuspendedAtUtc = null;
        SuspensionReason = null;
    }

    public void Deactivate(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        TransitionTo(AccountStatus.Deactivated, utcNow);
    }

    private void TransitionTo(AccountStatus target, DateTimeOffset utcNow)
    {
        if (Status == target)
        {
            return;
        }

        var allowed = Status switch
        {
            AccountStatus.Active => target is AccountStatus.Suspended or AccountStatus.Deactivated,
            AccountStatus.Suspended => target is AccountStatus.Active or AccountStatus.Deactivated,
            AccountStatus.Deactivated => false,
            _ => false
        };

        if (!allowed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidAccountStatusTransition,
                $"Cannot transition Platform User from {Status} to {target}.");
        }

        Status = target;
        UpdatedAtUtc = utcNow;
    }

    private void EnsureNotDeactivated()
    {
        if (Status == AccountStatus.Deactivated)
        {
            throw new DomainException(
                DomainErrorCodes.UserNotActive,
                "A deactivated Platform User cannot be updated.");
        }
    }

    public static (string Display, string Normalized) NormalizeUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUsername,
                "Username cannot be blank.");
        }

        var trimmed = username.Trim();
        var normalized = trimmed.ToLowerInvariant();
        if (normalized.Length is < 3 or > 64 || !UsernamePattern.IsMatch(normalized))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUsername,
                "Username must be 3–64 characters: lowercase letters, numbers, dots, underscores, or hyphens.");
        }

        return (trimmed, normalized);
    }

    internal static string NormalizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDisplayName,
                "Display name cannot be blank.");
        }

        var trimmed = Regex.Replace(displayName.Trim(), @"\s+", " ");
        if (trimmed.Length is < 2 or > 100 || !DisplayNamePattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDisplayName,
                "Display name must be 2–100 characters and use letters, numbers, spaces, apostrophes, periods, or hyphens.");
        }

        return trimmed;
    }

    public static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidEmail,
                "Email cannot be blank.");
        }

        var trimmed = email.Trim();
        if (trimmed.Contains(' ', StringComparison.Ordinal)
            || trimmed.Contains('<', StringComparison.Ordinal)
            || trimmed.Contains('>', StringComparison.Ordinal))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidEmail,
                "Email must be a plain address without display name.");
        }

        try
        {
            var address = new MailAddress(trimmed);
            return address.Address.ToLowerInvariant();
        }
        catch (FormatException)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidEmail,
                "Email format is invalid.");
        }
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidUtcTimestamp,
                "Timestamps must be UTC (offset zero).");
        }
    }
}
