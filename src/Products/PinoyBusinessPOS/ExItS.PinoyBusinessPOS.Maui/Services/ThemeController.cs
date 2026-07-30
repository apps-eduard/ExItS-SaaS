using ExItS.DesignSystem.Abstractions;
using Microsoft.JSInterop;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// Blazor-friendly façade over <see cref="IThemePreferenceStore"/> for the POS shell. Reads the
/// persisted preference and applies the <c>data-theme</c> attribute to the WebView document via
/// JS interop. Must be initialized from a component's <c>OnAfterRenderAsync(firstRender)</c> —
/// JS interop is not available before the WebView has completed its first render.
/// </summary>
public sealed class ThemeController(IThemePreferenceStore store, IJSRuntime js)
{
    private bool _initialized;

    public ThemePreference Current { get; private set; } = ThemePreference.System;

    /// <summary>Raised after the theme changes so subscribers (the shell) can re-render.</summary>
    public event Func<Task>? Changed;

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        Current = await store.GetAsync();
        await ApplyToDocumentAsync(Current);
    }

    public async Task SetThemeAsync(ThemePreference preference)
    {
        Current = preference;
        await store.SetAsync(preference);
        await ApplyToDocumentAsync(preference);

        if (Changed is not null)
        {
            await Changed.Invoke();
        }
    }

    private async Task ApplyToDocumentAsync(ThemePreference preference)
    {
        try
        {
            await js.InvokeVoidAsync("exitsPosTheme.applyTheme", preference.ToString());
        }
        catch (JSException)
        {
            // WebView not yet ready for interop; theme-boot.js already applied a best-effort
            // default and the next render will retry.
        }
    }
}
