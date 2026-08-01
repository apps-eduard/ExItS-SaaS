using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace ExItS.Platform.Api.Common;

/// <summary>
/// Constrained forwarded-header support for reverse-proxy deployments (P14-WP03).
/// Disabled by default. When enabled, only configured KnownIPNetworks / KnownProxies are trusted —
/// spoofed X-Forwarded-* from untrusted clients is ignored.
/// </summary>
internal static class PlatformForwardedHeaders
{
    public const string SectionName = "ForwardedHeaders";

    public static void AddPlatformForwardedHeaders(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(SectionName);
        if (!section.GetValue("Enabled", false))
        {
            return;
        }

        builder.Services.Configure<ForwardedHeadersOptions>(options => Apply(options, section));
    }

    public static void UsePlatformForwardedHeaders(this WebApplication app)
    {
        if (!app.Configuration.GetValue($"{SectionName}:Enabled", false))
        {
            return;
        }

        app.UseForwardedHeaders();
    }

    internal static void Apply(ForwardedHeadersOptions options, IConfiguration section)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
            | ForwardedHeaders.XForwardedProto
            | ForwardedHeaders.XForwardedHost;
        options.RequireHeaderSymmetry = false;
        options.ForwardLimit = section.GetValue("ForwardLimit", 1);

        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();

        foreach (var cidr in section.GetSection("KnownNetworks").Get<string[]>() ?? [])
        {
            if (System.Net.IPNetwork.TryParse(cidr, out var network))
            {
                options.KnownIPNetworks.Add(network);
            }
        }

        foreach (var proxy in section.GetSection("KnownProxies").Get<string[]>() ?? [])
        {
            if (IPAddress.TryParse(proxy, out var address))
            {
                options.KnownProxies.Add(address);
            }
        }
    }

    internal static bool TryParseCidr(string? cidr, out System.Net.IPNetwork network) =>
        System.Net.IPNetwork.TryParse(cidr, out network);
}
