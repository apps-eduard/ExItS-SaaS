using ExItS.PinoyBusinessPOS.Application.Offline;
using Microsoft.AspNetCore.Components;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// Navigates only when the destination is allowed offline, otherwise shows the shared Internet-required dialog.
/// Never clears session/org and never redirects to /reconnect for ordinary OnlineRequired features.
/// Must be scoped so it uses the BlazorWebView <see cref="NavigationManager"/> instance.
/// </summary>
public sealed class OfflineAwareNavigation(
    NavigationManager navigation,
    OnlineRequiredGuard onlineRequired,
    IPosOfflineCapabilityPolicy policy)
{
    public async Task NavigateAsync(string uri, bool replace = false, CancellationToken ct = default)
    {
        var path = PosOfflineCapabilityPolicy.Normalize(ToRelativePath(uri));
        if (policy.RequiresOnlineForRoute(path)
            && !await onlineRequired.EnsureOnlineForRouteAsync(path, ct))
        {
            return;
        }

        navigation.NavigateTo(uri, replace);
    }

    public Task NavigateToOrganizationSelectAsync(
        string? currentOrganizationDisplayName,
        bool replace = false,
        CancellationToken ct = default) =>
        NavigateForActionAsync(
            PosOfflineActionKeys.SwitchOrganization,
            "/workspace-select",
            currentOrganizationDisplayName,
            replace,
            ct);

    public async Task NavigateForActionAsync(
        string actionKey,
        string uri,
        string? currentOrganizationDisplayName = null,
        bool replace = false,
        CancellationToken ct = default)
    {
        if (!await onlineRequired.EnsureOnlineForActionAsync(actionKey, currentOrganizationDisplayName, ct))
        {
            return;
        }

        navigation.NavigateTo(uri, replace);
    }

    private string ToRelativePath(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
        {
            return "/";
        }

        if (uri.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return "/" + navigation.ToBaseRelativePath(uri);
        }

        return uri.StartsWith('/') ? uri : "/" + uri;
    }
}
