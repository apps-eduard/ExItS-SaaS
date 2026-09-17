using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Options;

namespace ExItS.PinoyBusinessPOS.Api.Common;

/// <summary>
/// Shared Development/Testing environment gate for POS API hardening.
/// Development-stage headers and probes are unavailable outside these environments.
/// </summary>
internal static class PosDevelopmentEnvironment
{
    public static bool IsApprovedDevelopmentEnvironment(IHostEnvironment environment) =>
        environment.IsDevelopment()
        || string.Equals(environment.EnvironmentName, "Testing", StringComparison.OrdinalIgnoreCase);

    public static IResult DevelopmentHeadersUnavailable() =>
        PosApiResults.Problem(
            ApplicationErrorCodes.DevelopmentHeadersUnavailable,
            "Development-stage organization, actor, and commercial headers are unavailable outside Development/Testing.",
            StatusCodes.Status403Forbidden);
}

/// <summary>Fails Production startup when required secure configuration is missing or uses known-dev secrets.</summary>
internal static class PosProductionSecurityGuard
{
    public const string KnownDevelopmentPasswordMarker = "exits_platform_dev_only";

    public static void ValidateOrThrow(WebApplicationBuilder builder)
    {
        var env = builder.Environment;
        var localValidationEnabled = builder.Configuration.GetValue<bool>("LocalValidation:Enabled")
            && !env.IsProduction();

        if (PosDevelopmentEnvironment.IsApprovedDevelopmentEnvironment(env) || localValidationEnabled)
        {
            if (localValidationEnabled)
            {
                AssertLocalValidationDatabasePortOrThrow(builder.Configuration.GetConnectionString("PosDatabase"));
            }

            return;
        }

        var connectionString = builder.Configuration.GetConnectionString("PosDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Production requires ConnectionStrings:PosDatabase from an approved secure configuration provider.");
        }

        if (connectionString.Contains(KnownDevelopmentPasswordMarker, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Production must not use the documented development database password.");
        }

        // An offline price lease is only as trustworthy as its signing key: with the published
        // development key, any device could mint its own prices.
        var priceAuthorityKey = builder.Configuration[
            $"{OfflinePriceAuthorityOptions.SectionName}:{nameof(OfflinePriceAuthorityOptions.PriceAuthoritySigningKey)}"];
        if (string.IsNullOrWhiteSpace(priceAuthorityKey))
        {
            throw new InvalidOperationException(
                "Production requires PosOffline:PriceAuthoritySigningKey from an approved secure configuration provider.");
        }

        if (string.Equals(
                priceAuthorityKey.Trim(),
                OfflinePriceAuthorityOptions.DevelopmentSigningKey,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Production must not use the documented development offline price authority signing key.");
        }

        var operatingGrantKey = builder.Configuration[
            $"{OfflinePriceAuthorityOptions.SectionName}:{nameof(OfflinePriceAuthorityOptions.OperatingGrantSigningPrivateKeyPem)}"];
        if (string.IsNullOrWhiteSpace(operatingGrantKey))
        {
            throw new InvalidOperationException(
                "Production requires PosOffline:OperatingGrantSigningPrivateKeyPem from an approved secure configuration provider.");
        }

        if (string.Equals(
                operatingGrantKey.Trim(),
                OfflinePriceAuthorityOptions.DevelopmentOperatingGrantPrivateKeyPem.Trim(),
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "Production must not use the documented development offline operating grant signing key.");
        }

        var allowedHosts = builder.Configuration["AllowedHosts"];
        if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Trim() == "*")
        {
            throw new InvalidOperationException(
                "Production requires an explicit AllowedHosts value (wildcard '*' is not allowed).");
        }

        var platformAuthBaseUrl = builder.Configuration[$"{PlatformAuthOptions.SectionName}:BaseUrl"];
        if (!string.IsNullOrWhiteSpace(platformAuthBaseUrl))
        {
            if (!Uri.TryCreate(platformAuthBaseUrl, UriKind.Absolute, out var platformUri))
            {
                throw new InvalidOperationException(
                    "Production requires PlatformAuth:BaseUrl to be an absolute URI when configured.");
            }

            // Local validation (non-Production) may call local Platform API over HTTP.
            if (!localValidationEnabled
                && !string.Equals(platformUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Production requires PlatformAuth:BaseUrl to use HTTPS when configured.");
            }
        }

        // Device installation enforcement cannot be paused in Production (PWA preview uses non-Production only).
        var deviceEnforcement = builder.Configuration.GetValue(
            $"{PosDeviceAuthorizationOptions.SectionName}:{nameof(PosDeviceAuthorizationOptions.EnforcementEnabled)}",
            defaultValue: true);
        if (!deviceEnforcement)
        {
            throw new InvalidOperationException(
                "POS device authorization cannot be disabled in Production. Set PosDeviceAuthorization:EnforcementEnabled=true.");
        }
    }

    /// <summary>
    /// Local Validation host mode must use the canonical POS DB host port (15534), never the
    /// plain Development docker-compose port 5434 from appsettings.Development.json.
    /// Docker apps profile (Host=pos-db;Port=5432) remains valid.
    /// </summary>
    internal static void AssertLocalValidationDatabasePortOrThrow(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "LocalValidation requires ConnectionStrings:PosDatabase (Start-LocalValidation sets Host=127.0.0.1;Port=15534).");
        }

        if (connectionString.Contains("Port=5434", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "LocalValidation must not use Development POS DB port 5434. "
                + "Use Start-LocalValidation / dashboard Start so ConnectionStrings__PosDatabase targets 127.0.0.1:15534.");
        }

        var isHostLoopback =
            connectionString.Contains("Host=127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Host=localhost", StringComparison.OrdinalIgnoreCase);
        if (isHostLoopback && !connectionString.Contains("Port=15534", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "LocalValidation host-mode POS DB must use 127.0.0.1:15534 (ConnectionStrings__PosDatabase from Start-LocalValidation).");
        }
    }
}
