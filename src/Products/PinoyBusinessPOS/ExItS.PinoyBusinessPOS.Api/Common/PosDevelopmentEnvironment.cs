using ExItS.PinoyBusinessPOS.Application.Common;

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
        if (PosDevelopmentEnvironment.IsApprovedDevelopmentEnvironment(env))
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

        var allowedHosts = builder.Configuration["AllowedHosts"];
        if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Trim() == "*")
        {
            throw new InvalidOperationException(
                "Production requires an explicit AllowedHosts value (wildcard '*' is not allowed).");
        }
    }
}
