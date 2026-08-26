using System.Globalization;
using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Settings;

/// <summary>Persisted platform-owned singleton settings (general, email, regional).</summary>
public sealed class PlatformSettings
{
    public const int SingletonId = 1;

    private static readonly Regex CurrencyCode = new(
        @"^[A-Z]{3}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex CountryCode = new(
        @"^[A-Z]{2}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public int Id { get; }
    public string? PlatformDisplayName { get; private set; }
    public string? SupportEmail { get; private set; }
    public PlatformBrandingDefaults Branding { get; private set; }
    public PlatformEmailProviderMode EmailProviderMode { get; private set; }
    public string? SmtpHost { get; private set; }
    public int? SmtpPort { get; private set; }
    public string? SmtpUsername { get; private set; }
    public bool SmtpPasswordConfigured { get; private set; }
    public string? FromDisplayName { get; private set; }
    public string? FromAddress { get; private set; }
    public PlatformSmtpSecurityMode SmtpSecurityMode { get; private set; }
    public string? AdminPublicBaseUrl { get; private set; }
    public string? DefaultTimeZoneId { get; private set; }
    public string? DefaultLocale { get; private set; }
    public string? DefaultCurrencyCode { get; private set; }
    public string? DefaultCountryCode { get; private set; }
    public string? DateFormat { get; private set; }
    public string? TimeFormat { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public string? UpdatedByActorId { get; private set; }
    public int Version { get; private set; }

    private PlatformSettings(
        int id,
        string? platformDisplayName,
        string? supportEmail,
        PlatformBrandingDefaults branding,
        PlatformEmailProviderMode emailProviderMode,
        string? smtpHost,
        int? smtpPort,
        string? smtpUsername,
        bool smtpPasswordConfigured,
        string? fromDisplayName,
        string? fromAddress,
        PlatformSmtpSecurityMode smtpSecurityMode,
        string? adminPublicBaseUrl,
        string? defaultTimeZoneId,
        string? defaultLocale,
        string? defaultCurrencyCode,
        string? defaultCountryCode,
        string? dateFormat,
        string? timeFormat,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        string? updatedByActorId,
        int version)
    {
        Id = id;
        PlatformDisplayName = platformDisplayName;
        SupportEmail = supportEmail;
        Branding = branding;
        EmailProviderMode = emailProviderMode;
        SmtpHost = smtpHost;
        SmtpPort = smtpPort;
        SmtpUsername = smtpUsername;
        SmtpPasswordConfigured = smtpPasswordConfigured;
        FromDisplayName = fromDisplayName;
        FromAddress = fromAddress;
        SmtpSecurityMode = smtpSecurityMode;
        AdminPublicBaseUrl = adminPublicBaseUrl;
        DefaultTimeZoneId = defaultTimeZoneId;
        DefaultLocale = defaultLocale;
        DefaultCurrencyCode = defaultCurrencyCode;
        DefaultCountryCode = defaultCountryCode;
        DateFormat = dateFormat;
        TimeFormat = timeFormat;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        UpdatedByActorId = updatedByActorId;
        Version = version;
    }

    public static PlatformSettings CreateDefaults(DateTimeOffset utcNow, string? actorId)
    {
        EnsureUtc(utcNow);
        return new PlatformSettings(
            SingletonId,
            platformDisplayName: "ExItS",
            supportEmail: null,
            PlatformBrandingDefaults.Empty,
            PlatformEmailProviderMode.Smtp,
            smtpHost: null,
            smtpPort: null,
            smtpUsername: null,
            smtpPasswordConfigured: false,
            fromDisplayName: "ExItS",
            fromAddress: null,
            PlatformSmtpSecurityMode.None,
            adminPublicBaseUrl: null,
            defaultTimeZoneId: "UTC",
            defaultLocale: "en-US",
            defaultCurrencyCode: "USD",
            defaultCountryCode: "US",
            dateFormat: null,
            timeFormat: null,
            utcNow,
            utcNow,
            actorId,
            version: 1);
    }

    public static PlatformSettings Rehydrate(
        int id,
        string? platformDisplayName,
        string? supportEmail,
        PlatformBrandingDefaults branding,
        PlatformEmailProviderMode emailProviderMode,
        string? smtpHost,
        int? smtpPort,
        string? smtpUsername,
        bool smtpPasswordConfigured,
        string? fromDisplayName,
        string? fromAddress,
        PlatformSmtpSecurityMode smtpSecurityMode,
        string? adminPublicBaseUrl,
        string? defaultTimeZoneId,
        string? defaultLocale,
        string? defaultCurrencyCode,
        string? defaultCountryCode,
        string? dateFormat,
        string? timeFormat,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        string? updatedByActorId,
        int version) =>
        new(
            id,
            platformDisplayName,
            supportEmail,
            branding,
            emailProviderMode,
            smtpHost,
            smtpPort,
            smtpUsername,
            smtpPasswordConfigured,
            fromDisplayName,
            fromAddress,
            smtpSecurityMode,
            adminPublicBaseUrl,
            defaultTimeZoneId,
            defaultLocale,
            defaultCurrencyCode,
            defaultCountryCode,
            dateFormat,
            timeFormat,
            createdAtUtc,
            updatedAtUtc,
            updatedByActorId,
            version);

    public void UpdateGeneral(
        string platformDisplayName,
        string? supportEmail,
        PlatformBrandingDefaults branding,
        DateTimeOffset utcNow,
        string actorId,
        int? expectedVersion)
    {
        EnsureUtc(utcNow);
        EnsureVersion(expectedVersion);
        PlatformDisplayName = NormalizeDisplayName(platformDisplayName);
        SupportEmail = NormalizeOptionalEmail(supportEmail);
        Branding = branding;
        Touch(utcNow, actorId);
    }

    public void UpdateEmail(
        PlatformEmailProviderMode providerMode,
        string? smtpHost,
        int? smtpPort,
        string? smtpUsername,
        bool replacePassword,
        bool passwordConfiguredAfterUpdate,
        string fromDisplayName,
        string fromAddress,
        PlatformSmtpSecurityMode securityMode,
        string? adminPublicBaseUrl,
        DateTimeOffset utcNow,
        string actorId,
        int? expectedVersion)
    {
        EnsureUtc(utcNow);
        EnsureVersion(expectedVersion);
        EmailProviderMode = providerMode;
        if (providerMode == PlatformEmailProviderMode.Disabled)
        {
            SmtpHost = null;
            SmtpPort = null;
            SmtpUsername = null;
            SmtpPasswordConfigured = false;
            FromDisplayName = NormalizeDisplayName(fromDisplayName);
            FromAddress = NormalizeRequiredEmail(fromAddress);
            SmtpSecurityMode = securityMode;
            AdminPublicBaseUrl = NormalizeAdminPublicBaseUrl(adminPublicBaseUrl);
            Touch(utcNow, actorId);
            return;
        }

        SmtpHost = NormalizeSmtpHost(smtpHost);
        SmtpPort = NormalizeSmtpPort(smtpPort);
        SmtpUsername = NormalizeOptionalUsername(smtpUsername);
        if (replacePassword)
        {
            SmtpPasswordConfigured = passwordConfiguredAfterUpdate;
        }

        FromDisplayName = NormalizeDisplayName(fromDisplayName);
        FromAddress = NormalizeRequiredEmail(fromAddress);
        SmtpSecurityMode = securityMode;
        AdminPublicBaseUrl = NormalizeAdminPublicBaseUrl(adminPublicBaseUrl);
        Touch(utcNow, actorId);
    }

    public void MarkSmtpPasswordConfigured(bool configured, DateTimeOffset utcNow, string actorId)
    {
        EnsureUtc(utcNow);
        SmtpPasswordConfigured = configured;
        Touch(utcNow, actorId);
    }

    public void UpdateRegional(
        string defaultTimeZoneId,
        string defaultLocale,
        string defaultCurrencyCode,
        string defaultCountryCode,
        string? dateFormat,
        string? timeFormat,
        DateTimeOffset utcNow,
        string actorId,
        int? expectedVersion)
    {
        EnsureUtc(utcNow);
        EnsureVersion(expectedVersion);
        DefaultTimeZoneId = NormalizeTimeZoneId(defaultTimeZoneId);
        DefaultLocale = NormalizeLocale(defaultLocale);
        DefaultCurrencyCode = NormalizeCurrencyCode(defaultCurrencyCode);
        DefaultCountryCode = NormalizeCountryCode(defaultCountryCode);
        DateFormat = NormalizeOptionalFormat(dateFormat, nameof(dateFormat));
        TimeFormat = NormalizeOptionalFormat(timeFormat, nameof(timeFormat));
        Touch(utcNow, actorId);
    }

    public bool IsEmailDeliveryConfigured =>
        EmailProviderMode == PlatformEmailProviderMode.Smtp
        && !string.IsNullOrWhiteSpace(SmtpHost)
        && SmtpPort is > 0
        && !string.IsNullOrWhiteSpace(FromAddress)
        && !string.IsNullOrWhiteSpace(AdminPublicBaseUrl);

    private void Touch(DateTimeOffset utcNow, string actorId)
    {
        UpdatedAtUtc = utcNow;
        UpdatedByActorId = actorId;
        Version++;
    }

    private void EnsureVersion(int? expectedVersion)
    {
        if (expectedVersion is null)
        {
            return;
        }

        if (expectedVersion.Value != Version)
        {
            throw new DomainException(
                DomainErrorCodes.PlatformSettingsConcurrencyConflict,
                "Platform settings were modified by another request.");
        }
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }

    private static string NormalizeDisplayName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrorCodes.InvalidPlatformSettings, "Display name is required.");
        }

        var trimmed = value.Trim();
        if (trimmed.Length > 200)
        {
            throw new DomainException(DomainErrorCodes.InvalidPlatformSettings, "Display name is too long.");
        }

        return trimmed;
    }

    private static string? NormalizeOptionalEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        return PlatformUser.NormalizeEmail(email);
    }

    private static string NormalizeRequiredEmail(string email) => PlatformUser.NormalizeEmail(email);

    private static string NormalizeSmtpHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            throw new DomainException(DomainErrorCodes.InvalidPlatformSettings, "SMTP host is required.");
        }

        var trimmed = host.Trim();
        if (trimmed.Length > 255)
        {
            throw new DomainException(DomainErrorCodes.InvalidPlatformSettings, "SMTP host is too long.");
        }

        return trimmed;
    }

    private static int NormalizeSmtpPort(int? port)
    {
        if (port is null or < 1 or > 65535)
        {
            throw new DomainException(DomainErrorCodes.InvalidPlatformSettings, "SMTP port must be between 1 and 65535.");
        }

        return port.Value;
    }

    private static string? NormalizeOptionalUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        var trimmed = username.Trim();
        if (trimmed.Length > 255)
        {
            throw new DomainException(DomainErrorCodes.InvalidPlatformSettings, "SMTP username is too long.");
        }

        return trimmed;
    }

    private static string? NormalizeAdminPublicBaseUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var trimmed = url.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlatformSettings,
                "Admin public base URL must be an absolute http(s) URL.");
        }

        return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
    }

    private static string NormalizeTimeZoneId(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            throw new DomainException(DomainErrorCodes.InvalidPlatformSettings, "Default timezone is required.");
        }

        try
        {
            _ = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim());
        }
        catch (TimeZoneNotFoundException)
        {
            throw new DomainException(DomainErrorCodes.InvalidPlatformSettings, "Default timezone is not supported.");
        }
        catch (InvalidTimeZoneException)
        {
            throw new DomainException(DomainErrorCodes.InvalidPlatformSettings, "Default timezone is not supported.");
        }

        return timeZoneId.Trim();
    }

    private static string NormalizeLocale(string locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            throw new DomainException(DomainErrorCodes.InvalidPlatformSettings, "Default locale is required.");
        }

        try
        {
            _ = CultureInfo.GetCultureInfo(locale.Trim());
        }
        catch (CultureNotFoundException)
        {
            throw new DomainException(DomainErrorCodes.InvalidPlatformSettings, "Default locale is not supported.");
        }

        return locale.Trim();
    }

    private static string NormalizeCurrencyCode(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new DomainException(DomainErrorCodes.InvalidPlatformSettings, "Default currency is required.");
        }

        var normalized = currencyCode.Trim().ToUpperInvariant();
        if (!CurrencyCode.IsMatch(normalized))
        {
            throw new DomainException(DomainErrorCodes.InvalidPlatformSettings, "Default currency must be a 3-letter ISO code.");
        }

        return normalized;
    }

    private static string NormalizeCountryCode(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            throw new DomainException(DomainErrorCodes.InvalidPlatformSettings, "Default country is required.");
        }

        var normalized = countryCode.Trim().ToUpperInvariant();
        if (!CountryCode.IsMatch(normalized))
        {
            throw new DomainException(DomainErrorCodes.InvalidPlatformSettings, "Default country must be a 2-letter ISO code.");
        }

        return normalized;
    }

    private static string? NormalizeOptionalFormat(string? format, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return null;
        }

        var trimmed = format.Trim();
        if (trimmed.Length > 64)
        {
            throw new DomainException(DomainErrorCodes.InvalidPlatformSettings, $"{fieldName} is too long.");
        }

        return trimmed;
    }
}
