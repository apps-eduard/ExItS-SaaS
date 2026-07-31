using Microsoft.JSInterop;

namespace ExItS.Platform.Admin.Services;

public enum AdminTheme { System, Light, Dark }

/// <summary>
/// Circuit-scoped theme preference. Persisted in localStorage as lowercase
/// <c>system</c>/<c>light</c>/<c>dark</c> (<c>exits-admin-theme</c>).
/// Applied by <c>theme-boot.js</c> before first paint and re-applied after
/// Blazor enhanced navigation so <c>data-theme</c> is not lost.
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
        // Re-apply after SSR/enhanced navigation — storage alone is not enough because
        // documentElement data-theme can be replaced when Blazor swaps page HTML.
        await js.InvokeVoidAsync("exitsAdminTheme.applyTheme", ToStorageValue(Current));
    }

    public async Task SetThemeAsync(AdminTheme theme)
    {
        Current = theme;
        var value = ToStorageValue(theme);
        await js.InvokeVoidAsync("exitsAdminTheme.set", StorageKey, value);
        await js.InvokeVoidAsync("exitsAdminTheme.applyTheme", value);
        if (Changed is not null)
        {
            await Changed.Invoke();
        }
    }

    public static string ToStorageValue(AdminTheme theme) => theme switch
    {
        AdminTheme.Light => "light",
        AdminTheme.Dark => "dark",
        _ => "system"
    };

    /// <summary>Accepts authoritative lowercase values and legacy PascalCase values.</summary>
    public static AdminTheme Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return AdminTheme.System;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "light" => AdminTheme.Light,
            "dark" => AdminTheme.Dark,
            "system" => AdminTheme.System,
            _ => AdminTheme.System
        };
    }
}
