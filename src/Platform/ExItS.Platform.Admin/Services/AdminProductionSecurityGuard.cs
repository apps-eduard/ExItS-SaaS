namespace ExItS.Platform.Admin.Services;

/// <summary>Fails Production startup when Admin→Platform API configuration is unsafe.</summary>
internal static class AdminProductionSecurityGuard
{
    public static void ValidateOrThrow(WebApplicationBuilder builder)
    {
        var env = builder.Environment;
        if (env.IsDevelopment() || env.IsEnvironment("Testing"))
        {
            return;
        }

        var livePreviewEnabled = builder.Configuration.GetValue<bool>("LivePreview:Enabled");
        if (livePreviewEnabled && env.IsProduction())
        {
            throw new InvalidOperationException("LivePreview:Enabled=true is forbidden in Production.");
        }

        var baseUrl = builder.Configuration[$"{PlatformApiOptions.SectionName}:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(
                "Production requires PlatformApi:BaseUrl from an approved secure configuration provider.");
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException(
                "Production requires PlatformApi:BaseUrl to be an absolute URI.");
        }

        // Live preview (non-Production) may use HTTP against the packaging Compose network.
        if (livePreviewEnabled && !env.IsProduction())
        {
            return;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Production requires PlatformApi:BaseUrl to use HTTPS.");
        }
    }
}
