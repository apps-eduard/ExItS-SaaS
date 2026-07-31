using Microsoft.JSInterop;

namespace ExItS.Platform.Admin.Services;

public enum AdminTheme { System, Light, Dark }

/// <summary>
/// Circuit-scoped Light / Dark / System preference for Platform Admin.
/// Persisted in localStorage as lowercase <c>light</c>/<c>dark</c>/<c>system</c>.
/// </summary>
public sealed class ThemeService(IJSRuntime js)
{
    public const string StorageKey = "exits-admin-theme";

    public AdminTheme Current { get; private set; } = AdminTheme.Light;
    public event Func<Task>? Changed;

    public bool IsDark => Current == AdminTheme.Dark;

    public async Task InitializeAsync()
    {
        var stored = await js.InvokeAsync<string?>("exitsAdminTheme.get", StorageKey);
        Current = Parse(stored);
        await js.InvokeVoidAsync("exitsAdminTheme.set", StorageKey, ToStorageValue(Current));
        await js.InvokeVoidAsync("exitsAdminTheme.applyTheme", ToStorageValue(Current));
        if (Changed is not null)
        {
            await Changed.Invoke();
        }
    }

    public async Task SetThemeAsync(AdminTheme theme)
    {
        Current = theme == AdminTheme.System || theme == AdminTheme.Dark || theme == AdminTheme.Light
            ? theme
            : AdminTheme.Light;
        var value = ToStorageValue(Current);
        await js.InvokeVoidAsync("exitsAdminTheme.set", StorageKey, value);
        await js.InvokeVoidAsync("exitsAdminTheme.applyTheme", value);
        if (Changed is not null)
        {
            await Changed.Invoke();
        }
    }

    public static string ToStorageValue(AdminTheme theme) => theme switch
    {
        AdminTheme.Dark => "dark",
        AdminTheme.System => "system",
        _ => "light"
    };

    /// <summary>Accepts authoritative lowercase values and legacy PascalCase.</summary>
    public static AdminTheme Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return AdminTheme.Light;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "dark" => AdminTheme.Dark,
            "system" => AdminTheme.System,
            "light" => AdminTheme.Light,
            _ => AdminTheme.Light
        };
    }
}
