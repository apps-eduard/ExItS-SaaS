using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace ExItS.Platform.Admin;

/// <summary>
/// Constrained forwarded-header support for reverse-proxy deployments (P14-WP03).
/// Disabled by default; only configured KnownIPNetworks / KnownProxies are trusted.
/// </summary>
public static class AdminForwardedHeaders
{
    public const string SectionName = "ForwardedHeaders";

    public static void AddAdminForwardedHeaders(this WebApplicationBuilder builder)
    {
        var section = builder.Configuration.GetSection(SectionName);
        if (!section.GetValue("Enabled", false))
        {
            return;
        }

        builder.Services.Configure<ForwardedHeadersOptions>(options => Apply(options, section));
    }

    public static void UseAdminForwardedHeaders(this WebApplication app)
    {
        if (!app.Configuration.GetValue($"{SectionName}:Enabled", false))
        {
            return;
        }

        app.UseForwardedHeaders();
    }

    private static void Apply(ForwardedHeadersOptions options, IConfiguration section)
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
}
