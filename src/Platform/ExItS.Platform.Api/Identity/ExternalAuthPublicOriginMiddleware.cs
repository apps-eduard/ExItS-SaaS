using System.Net;
using System.Net.Sockets;
using ExItS.Platform.Application.Identity;

namespace ExItS.Platform.Api.Identity;

/// <summary>
/// Applies the configured public browser origin only when the TCP peer is the named React proxy.
/// Spoofed X-Forwarded-* headers from other clients are ignored.
/// </summary>
internal static class ExternalAuthPublicOriginMiddleware
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(10);
    private static readonly object Gate = new();
    private static string? CachedHost;
    private static IPAddress[] CachedAddresses = [];
    private static DateTimeOffset CachedAtUtc = DateTimeOffset.MinValue;

    public static void UseExternalAuthPublicOrigin(this WebApplication app)
    {
        var origin = app.Configuration["PlatformAuthentication:External:PublicBrowserOrigin"];
        var proxyHost = app.Configuration["PlatformAuthentication:External:TrustedProxyHost"];
        if (!ExternalAuthCallbackUri.TryCreate(origin, out _)
            || string.IsNullOrWhiteSpace(proxyHost))
        {
            return;
        }

        var browserOrigin = new Uri(origin!.Trim());
        var trustedHost = proxyHost.Trim();
        app.Use(async (context, next) =>
        {
            var remote = context.Connection.RemoteIpAddress;
            if (remote is not null && await IsTrustedProxyAsync(remote, trustedHost).ConfigureAwait(false))
            {
                context.Request.Scheme = browserOrigin.Scheme;
                context.Request.Host = browserOrigin.IsDefaultPort
                    ? new HostString(browserOrigin.Host)
                    : new HostString(browserOrigin.Host, browserOrigin.Port);
            }

            await next().ConfigureAwait(false);
        });
    }

    internal static async Task<bool> IsTrustedProxyAsync(IPAddress remote, string proxyHost)
    {
        var addresses = await ResolveAsync(proxyHost).ConfigureAwait(false);
        var candidate = remote.IsIPv4MappedToIPv6 ? remote.MapToIPv4() : remote;
        foreach (var address in addresses)
        {
            var trusted = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
            if (candidate.Equals(trusted))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<IPAddress[]> ResolveAsync(string proxyHost)
    {
        var now = DateTimeOffset.UtcNow;
        lock (Gate)
        {
            if (string.Equals(CachedHost, proxyHost, StringComparison.OrdinalIgnoreCase)
                && now - CachedAtUtc < CacheDuration)
            {
                return CachedAddresses;
            }
        }

        IPAddress[] resolved;
        try
        {
            resolved = await Dns.GetHostAddressesAsync(proxyHost).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is SocketException or ArgumentException)
        {
            resolved = [];
        }

        lock (Gate)
        {
            CachedHost = proxyHost;
            CachedAddresses = resolved;
            CachedAtUtc = now;
        }

        return resolved;
    }
}
