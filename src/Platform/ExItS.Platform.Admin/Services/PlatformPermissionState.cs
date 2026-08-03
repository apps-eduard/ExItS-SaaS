using ExItS.Platform.Admin.Models;
using Microsoft.Extensions.Hosting;

namespace ExItS.Platform.Admin.Services;

/// <summary>
/// Scoped per-circuit cache of the current actor's Platform permissions, loaded once from
/// <c>GET /api/v1/platform/authorization/me</c>. Used only to shape the UI (hide nav items and
/// mutation controls the actor is unlikely to be allowed to use); it is convenience only and never
/// replaces server-side authorization.
/// </summary>
public sealed class PlatformPermissionState(
    IPlatformApiClient api,
    IHostEnvironment env)
{
    private HashSet<string> _permissions = new(StringComparer.OrdinalIgnoreCase);
    private Task? _loadTask;

    public bool Loaded { get; private set; }
    public bool LoadFailed { get; private set; }
    public string? ActorIdentifier { get; private set; }

    /// <summary>Fired when <see cref="Loaded"/> becomes true or permissions are refreshed.</summary>
    public event Action? Changed;

    public Task EnsureLoadedAsync()
    {
        _loadTask ??= LoadAsync(allowDevFallback: true);
        return _loadTask;
    }

    /// <summary>
    /// Marks permissions loaded as empty for Organization/Personal shells.
    /// Never applies the Development “all permissions” fallback (that would force Platform chrome).
    /// </summary>
    public Task EnsureLoadedForNonPlatformAsync()
    {
        _loadTask ??= LoadAsync(allowDevFallback: false);
        return _loadTask;
    }

    /// <summary>Clears the circuit cache and reloads permissions (call after organization switch).</summary>
    public async Task RefreshAsync()
    {
        Loaded = false;
        LoadFailed = false;
        ActorIdentifier = null;
        _permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _loadTask = LoadAsync(allowDevFallback: true);
        await _loadTask;
        Changed?.Invoke();
    }

    private async Task LoadAsync(bool allowDevFallback)
    {
        try
        {
            // Stay on the Blazor sync context; off-context resumes can fault circuit JS interop.
            var result = await api.GetMyAuthorizationAsync();
            if (result.IsSuccess && result.Data is not null)
            {
                _permissions = new HashSet<string>(
                    result.Data.Permissions ?? [],
                    StringComparer.OrdinalIgnoreCase);
                ActorIdentifier = result.Data.ActorIdentifier;
            }
            else
            {
                LoadFailed = true;
                if (allowDevFallback)
                {
                    ApplyFallbackPermissions();
                }
            }
        }
        catch
        {
            LoadFailed = true;
            if (allowDevFallback)
            {
                ApplyFallbackPermissions();
            }
        }

        Loaded = true;
        Changed?.Invoke();
    }

    private void ApplyFallbackPermissions()
    {
        // Development/Testing only: keep Platform shell usable when /authorization/me fails
        // for a Platform session. Never used for Organization/Personal (would leak Platform nav).
        if (env.IsDevelopment() || env.IsEnvironment("Testing"))
        {
            _permissions = new HashSet<string>(PlatformPermissionCodes.All, StringComparer.OrdinalIgnoreCase);
        }
    }

    public bool HasPermission(string permission) =>
        Loaded && _permissions.Contains(permission);

    public bool HasAnyPermission(params string[] permissions) =>
        Loaded && permissions.Any(_permissions.Contains);
}
