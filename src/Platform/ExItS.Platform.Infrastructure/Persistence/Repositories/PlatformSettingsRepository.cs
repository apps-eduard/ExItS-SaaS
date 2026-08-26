using ExItS.Platform.Application.Settings;
using ExItS.Platform.Domain.Settings;
using ExItS.Platform.Infrastructure.Persistence.Settings;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class PlatformSettingsRepository(PlatformDbContext db) : IPlatformSettingsRepository
{
    public async Task<PlatformSettings?> GetAsync(CancellationToken cancellationToken = default)
    {
        var record = await db.PlatformSettings.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == PlatformSettings.SingletonId, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : ToDomain(record);
    }

    public async Task<string?> GetProtectedSmtpPasswordAsync(CancellationToken cancellationToken = default)
    {
        return await db.PlatformSettings.AsNoTracking()
            .Where(x => x.Id == PlatformSettings.SingletonId)
            .Select(x => x.ProtectedSmtpPassword)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public Task AddAsync(PlatformSettings settings, CancellationToken cancellationToken = default)
    {
        db.PlatformSettings.Add(ToRecord(settings));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PlatformSettings settings, CancellationToken cancellationToken = default)
    {
        var record = await db.PlatformSettings
            .FirstOrDefaultAsync(x => x.Id == settings.Id, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        Apply(record, settings);
    }

    public async Task UpdateSmtpPasswordAsync(
        int settingsId,
        string protectedPassword,
        CancellationToken cancellationToken = default)
    {
        var record = await db.PlatformSettings
            .FirstOrDefaultAsync(x => x.Id == settingsId, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.ProtectedSmtpPassword = protectedPassword;
        record.SmtpPasswordConfigured = true;
    }

    public async Task ClearSmtpPasswordAsync(int settingsId, CancellationToken cancellationToken = default)
    {
        var record = await db.PlatformSettings
            .FirstOrDefaultAsync(x => x.Id == settingsId, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            return;
        }

        record.ProtectedSmtpPassword = null;
        record.SmtpPasswordConfigured = false;
    }

    private static PlatformSettings ToDomain(PlatformSettingsRecord record) =>
        PlatformSettings.Rehydrate(
            record.Id,
            record.PlatformDisplayName,
            record.SupportEmail,
            PlatformBrandingDefaults.Create(
                record.BrandingLogoUrl,
                record.BrandingPrimaryColor,
                record.BrandingAccentColor),
            Enum.Parse<PlatformEmailProviderMode>(record.EmailProviderMode),
            record.SmtpHost,
            record.SmtpPort,
            record.SmtpUsername,
            record.SmtpPasswordConfigured,
            record.FromDisplayName,
            record.FromAddress,
            Enum.Parse<PlatformSmtpSecurityMode>(record.SmtpSecurityMode),
            record.AdminPublicBaseUrl,
            record.DefaultTimeZoneId,
            record.DefaultLocale,
            record.DefaultCurrencyCode,
            record.DefaultCountryCode,
            record.DateFormat,
            record.TimeFormat,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.UpdatedByActorId,
            record.Version);

    private static PlatformSettingsRecord ToRecord(PlatformSettings settings)
    {
        var record = new PlatformSettingsRecord
        {
            Id = settings.Id,
            CreatedAtUtc = settings.CreatedAtUtc,
            UpdatedAtUtc = settings.UpdatedAtUtc,
            UpdatedByActorId = settings.UpdatedByActorId,
            Version = settings.Version,
            ProtectedSmtpPassword = null,
            SmtpPasswordConfigured = settings.SmtpPasswordConfigured,
            EmailProviderMode = settings.EmailProviderMode.ToString(),
            SmtpSecurityMode = settings.SmtpSecurityMode.ToString(),
        };
        Apply(record, settings);
        return record;
    }

    private static void Apply(PlatformSettingsRecord record, PlatformSettings settings)
    {
        record.PlatformDisplayName = settings.PlatformDisplayName;
        record.SupportEmail = settings.SupportEmail;
        record.BrandingLogoUrl = settings.Branding.LogoUrl;
        record.BrandingPrimaryColor = settings.Branding.PrimaryColor;
        record.BrandingAccentColor = settings.Branding.AccentColor;
        record.EmailProviderMode = settings.EmailProviderMode.ToString();
        record.SmtpHost = settings.SmtpHost;
        record.SmtpPort = settings.SmtpPort;
        record.SmtpUsername = settings.SmtpUsername;
        record.SmtpPasswordConfigured = settings.SmtpPasswordConfigured;
        record.FromDisplayName = settings.FromDisplayName;
        record.FromAddress = settings.FromAddress;
        record.SmtpSecurityMode = settings.SmtpSecurityMode.ToString();
        record.AdminPublicBaseUrl = settings.AdminPublicBaseUrl;
        record.DefaultTimeZoneId = settings.DefaultTimeZoneId;
        record.DefaultLocale = settings.DefaultLocale;
        record.DefaultCurrencyCode = settings.DefaultCurrencyCode;
        record.DefaultCountryCode = settings.DefaultCountryCode;
        record.DateFormat = settings.DateFormat;
        record.TimeFormat = settings.TimeFormat;
        record.UpdatedAtUtc = settings.UpdatedAtUtc;
        record.UpdatedByActorId = settings.UpdatedByActorId;
        record.Version = settings.Version;
    }
}
