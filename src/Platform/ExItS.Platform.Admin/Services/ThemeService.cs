using Microsoft.JSInterop;

namespace ExItS.Platform.Admin.Services;

public enum AdminTheme { System, Light, Dark }

/// <summary>
/// Circuit-scoped light/dark preference for Platform Admin (Ant Design Pro–inspired shell).
/// Persisted in localStorage as lowercase <c>light</c>/<c>dark</c>.
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
        Current = NormalizeBinary(Parse(stored));
        await js.InvokeVoidAsync("exitsAdminTheme.set", StorageKey, ToStorageValue(Current));
        await js.InvokeVoidAsync("exitsAdminTheme.applyTheme", ToStorageValue(Current));
        if (Changed is not null)
        {
            await Changed.Invoke();
        }
    }

    public async Task SetThemeAsync(AdminTheme theme)
    {
        Current = NormalizeBinary(theme);
        var value = ToStorageValue(Current);
        await js.InvokeVoidAsync("exitsAdminTheme.set", StorageKey, value);
        await js.InvokeVoidAsync("exitsAdminTheme.applyTheme", value);
        if (Changed is not null)
        {
            await Changed.Invoke();
        }
    }

    public Task ToggleLightDarkAsync() =>
        SetThemeAsync(IsDark ? AdminTheme.Light : AdminTheme.Dark);

    public static string ToStorageValue(AdminTheme theme) =>
        theme == AdminTheme.Dark ? "dark" : "light";

    /// <summary>Accepts authoritative lowercase values and legacy PascalCase / system values.</summary>
    public static AdminTheme Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return AdminTheme.Light;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "dark" => AdminTheme.Dark,
            "light" => AdminTheme.Light,
            "system" => AdminTheme.System,
            _ => AdminTheme.Light
        };
    }

    private static AdminTheme NormalizeBinary(AdminTheme theme) =>
        theme == AdminTheme.Dark ? AdminTheme.Dark : AdminTheme.Light;
}
