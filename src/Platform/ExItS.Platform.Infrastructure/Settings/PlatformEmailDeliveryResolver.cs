using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Settings;
using ExItS.Platform.Domain.Settings;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Infrastructure.Settings;

internal sealed class PlatformEmailDeliveryResolver(
    IPlatformSettingsRepository repository,
    IPlatformSettingsSecretProtector secretProtector,
    IOptions<PlatformEmailDeliveryOptions> fallbackOptions) : IPlatformEmailDeliveryResolver
{
    public async Task<ResolvedPlatformEmailDelivery> ResolveAsync(CancellationToken cancellationToken = default)
    {
        var settings = await repository.GetAsync(cancellationToken).ConfigureAwait(false);
        if (settings is not null && settings.EmailProviderMode == PlatformEmailProviderMode.Smtp)
        {
            string? password = null;
            if (settings.SmtpPasswordConfigured)
            {
                var protectedPassword = await repository
                    .GetProtectedSmtpPasswordAsync(cancellationToken)
                    .ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(protectedPassword))
                {
                    password = secretProtector.Unprotect(protectedPassword);
                }
            }

            var configured =
                !string.IsNullOrWhiteSpace(settings.SmtpHost)
                && settings.SmtpPort is > 0
                && !string.IsNullOrWhiteSpace(settings.FromAddress)
                && !string.IsNullOrWhiteSpace(settings.AdminPublicBaseUrl)
                && (!string.IsNullOrWhiteSpace(settings.SmtpUsername) == false || settings.SmtpPasswordConfigured);

            return new ResolvedPlatformEmailDelivery(
                settings.SmtpHost,
                settings.SmtpPort,
                settings.SmtpUsername,
                password,
                settings.SmtpSecurityMode,
                settings.FromAddress ?? string.Empty,
                settings.FromDisplayName ?? "ExItS",
                settings.AdminPublicBaseUrl,
                configured && (string.IsNullOrWhiteSpace(settings.SmtpUsername) || settings.SmtpPasswordConfigured));
        }

        var fallback = fallbackOptions.Value;
        return new ResolvedPlatformEmailDelivery(
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
}
