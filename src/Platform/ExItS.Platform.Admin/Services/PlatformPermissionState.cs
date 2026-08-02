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

    public Task EnsureLoadedAsync()
    {
        _loadTask ??= LoadAsync();
        return _loadTask;
    }

    /// <summary>Clears the circuit cache and reloads permissions (call after organization switch).</summary>
    public async Task RefreshAsync()
    {
        Loaded = false;
        LoadFailed = false;
        ActorIdentifier = null;
        _permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _loadTask = LoadAsync();
        await _loadTask;
    }

    private async Task LoadAsync()
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
                ApplyFallbackPermissions();
            }
        }
        catch
        {
            LoadFailed = true;
            ApplyFallbackPermissions();
        }

        Loaded = true;
    }

    private void ApplyFallbackPermissions()
    {
        // Development/Testing only: keep the shell usable when /authorization/me fails.
        // Production-like hosts stay closed.
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
