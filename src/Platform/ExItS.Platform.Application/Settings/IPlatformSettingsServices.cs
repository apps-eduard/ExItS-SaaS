using ExItS.Platform.Domain.Settings;

namespace ExItS.Platform.Application.Settings;

public interface IPlatformSettingsRepository
{
    Task<PlatformSettings?> GetAsync(CancellationToken cancellationToken = default);
    Task<string?> GetProtectedSmtpPasswordAsync(CancellationToken cancellationToken = default);
    Task AddAsync(PlatformSettings settings, CancellationToken cancellationToken = default);
    Task UpdateAsync(PlatformSettings settings, CancellationToken cancellationToken = default);
    Task UpdateSmtpPasswordAsync(int settingsId, string protectedPassword, CancellationToken cancellationToken = default);
    Task ClearSmtpPasswordAsync(int settingsId, CancellationToken cancellationToken = default);
}

public interface IPlatformSettingsSecretProtector
{
    string Protect(string plaintext);
    string Unprotect(string protectedValue);
}

public interface IPlatformEmailTestSender
{
    Task SendTestEmailAsync(
        string recipientEmail,
        ResolvedPlatformEmailDelivery delivery,
        CancellationToken cancellationToken = default);
}

public sealed record ResolvedPlatformEmailDelivery(
    string? SmtpHost,
    int? SmtpPort,
    string? SmtpUsername,
    string? SmtpPassword,
    PlatformSmtpSecurityMode SecurityMode,
    string FromAddress,
    string FromDisplayName,
    string? AdminPublicBaseUrl,
    bool IsConfigured);

public interface IPlatformEmailDeliveryResolver
{
    Task<ResolvedPlatformEmailDelivery> ResolveAsync(CancellationToken cancellationToken = default);
}
