using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;

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
    }
}
