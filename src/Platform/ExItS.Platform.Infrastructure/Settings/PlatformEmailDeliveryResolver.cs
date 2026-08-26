using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Settings;
using ExItS.Platform.Domain.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Infrastructure.Settings;

internal sealed class PlatformEmailDeliveryResolver(
    IPlatformSettingsRepository repository,
    IPlatformSettingsSecretProtector secretProtector,
    IOptions<PlatformEmailDeliveryOptions> fallbackOptions,
    ILogger<PlatformEmailDeliveryResolver> logger) : IPlatformEmailDeliveryResolver
{
    public async Task<ResolvedPlatformEmailDelivery> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var fromSettings = await TryResolveFromSettingsAsync(cancellationToken).ConfigureAwait(false);
        if (fromSettings is { IsConfigured: true })
        {
            return fromSettings;
        }

        return MapFallback(fallbackOptions.Value);
    }

    private async Task<ResolvedPlatformEmailDelivery?> TryResolveFromSettingsAsync(
        CancellationToken cancellationToken)
    {
        var settings = await repository.GetAsync(cancellationToken).ConfigureAwait(false);
        if (settings is null || settings.EmailProviderMode != PlatformEmailProviderMode.Smtp)
        {
            return null;
        }

        string? password = null;
        if (settings.SmtpPasswordConfigured)
        {
            var protectedPassword = await repository
                .GetProtectedSmtpPasswordAsync(cancellationToken)
                .ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(protectedPassword))
            {
                try
                {
                    password = secretProtector.Unprotect(protectedPassword);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Failed to unprotect SMTP password from platform settings; using PlatformEmail fallback.");
                    return null;
                }
            }
        }

        var configured =
            !string.IsNullOrWhiteSpace(settings.SmtpHost)
            && settings.SmtpPort is > 0
            && !string.IsNullOrWhiteSpace(settings.FromAddress)
            && !string.IsNullOrWhiteSpace(settings.AdminPublicBaseUrl)
            && (string.IsNullOrWhiteSpace(settings.SmtpUsername) || settings.SmtpPasswordConfigured);

        return new ResolvedPlatformEmailDelivery(
            settings.SmtpHost,
            settings.SmtpPort,
            settings.SmtpUsername,
            password,
            settings.SmtpSecurityMode,
            settings.FromAddress ?? string.Empty,
            settings.FromDisplayName ?? "ExItS",
            settings.AdminPublicBaseUrl,
            configured);
    }

    private static ResolvedPlatformEmailDelivery MapFallback(PlatformEmailDeliveryOptions fallback) =>
        new(
            fallback.SmtpHost,
            fallback.SmtpPort,
            null,
            null,
            fallback.UseSsl ? PlatformSmtpSecurityMode.Ssl : PlatformSmtpSecurityMode.None,
            fallback.FromAddress,
            fallback.FromDisplayName,
            fallback.AdminPublicBaseUrl,
            fallback.IsConfigured);
}
