using Microsoft.JSInterop;

namespace ExItS.Platform.Admin.Services;

public enum AdminTheme { System, Light, Dark }

/// <summary>
/// Scoped per-circuit theme preference. Persisted client-side in localStorage
/// (<c>exits-admin-theme</c>) and applied by <c>wwwroot/theme-boot.js</c> before render to avoid
/// a flash of the wrong theme. Convenience only — does not affect server-side authorization.
/// </summary>
public sealed class ThemeService(IJSRuntime js)
{
    public const string StorageKey = "exits-admin-theme";

    public AdminTheme Current { get; private set; } = AdminTheme.System;
    public event Func<Task>? Changed;

    public async Task InitializeAsync()
    {
        var stored = await js.InvokeAsync<string?>("exitsAdminTheme.get", StorageKey);
        Current = Parse(stored);
    }

    public async Task SetThemeAsync(AdminTheme theme)
    {
        Current = theme;
        await js.InvokeVoidAsync("exitsAdminTheme.set", StorageKey, theme.ToString());
        await js.InvokeVoidAsync("exitsAdminTheme.applyTheme", theme.ToString());
        if (Changed is not null)
        {
            await Changed.Invoke();
        }
    }

    private static AdminTheme Parse(string? value) => value switch
    {
        "Light" => AdminTheme.Light,
        "Dark" => AdminTheme.Dark,
        _ => AdminTheme.System
    };
}
