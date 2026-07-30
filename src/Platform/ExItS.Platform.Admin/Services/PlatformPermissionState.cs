using ExItS.Platform.Admin.Models;
using Microsoft.Extensions.Hosting;

namespace ExItS.Platform.Admin.Services;

/// <summary>
/// Scoped per-circuit cache of the current actor's Platform permissions, loaded once from
/// <c>GET /api/v1/platform/authorization/me</c>. Used only to shape the UI (hide nav items and
/// mutation controls the actor is unlikely to be allowed to use); it is convenience only and never
/// replaces server-side authorization. If the endpoint is unavailable (for example, not yet wired
/// up on the API), Development/Testing falls back to full access — mirroring the existing
/// unauthenticated development-operator behavior — while other environments fail closed.
/// </summary>
public sealed class PlatformPermissionState(IPlatformApiClient api, IHostEnvironment env)
{
    private HashSet<string> _permissions = new(StringComparer.Ordinal);
    private Task? _loadTask;

    public bool Loaded { get; private set; }
    public bool LoadFailed { get; private set; }
    public string? ActorIdentifier { get; private set; }

    public Task EnsureLoadedAsync()
    {
        _loadTask ??= LoadAsync();
        return _loadTask;
    }

    private async Task LoadAsync()
    {
        var result = await api.GetMyAuthorizationAsync().ConfigureAwait(false);
        if (result.IsSuccess && result.Data is not null)
        {
            _permissions = new HashSet<string>(result.Data.Permissions, StringComparer.Ordinal);
            ActorIdentifier = result.Data.ActorIdentifier;
        }
        else
        {
            LoadFailed = true;
            if (env.IsDevelopment() || env.IsEnvironment("Testing"))
            {
                _permissions = new HashSet<string>(PlatformPermissionCodes.All, StringComparer.Ordinal);
            }
        }

        Loaded = true;
    }

    public bool HasPermission(string permission) => _permissions.Contains(permission);

    public bool HasAnyPermission(params string[] permissions) => permissions.Any(_permissions.Contains);
}
