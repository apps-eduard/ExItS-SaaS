using System.Globalization;
using ExItS.DesignSystem.Abstractions;
using Microsoft.JSInterop;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// Blazor-friendly façade over <see cref="ICulturePreferenceStore"/> for the POS shell. Applies
/// the selected culture to the current thread (so <c>IStringLocalizer</c> resolves the right
/// resx) and to new threads via <see cref="CultureInfo.DefaultThreadCurrentUICulture"/>. Because
/// MAUI Blazor Hybrid has no request pipeline to re-localize a response, callers must trigger a
/// "soft refresh" (e.g. changing a <c>@key</c> on the root layout) after <see cref="Changed"/>
/// fires so already-rendered components re-evaluate their localized strings.
/// </summary>
public sealed class CultureController(ICulturePreferenceStore store, IJSRuntime js)
{
    private bool _initialized;

    public string Current { get; private set; } = MauiCulturePreferenceStore.English;

    /// <summary>Incremented on every culture change; usable as a <c>@key</c> to force a subtree remount.</summary>
    public int Epoch { get; private set; }

    /// <summary>Raised after the culture changes so subscribers (the shell) can re-render.</summary>
    public event Func<Task>? Changed;

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        Current = await store.GetAsync();
        ApplyCultureInfo(Current);
        await ApplyToDocumentAsync(Current);
    }

    public async Task SetCultureAsync(string culture)
    {
        var normalized = culture == MauiCulturePreferenceStore.Filipino
            ? MauiCulturePreferenceStore.Filipino
            : MauiCulturePreferenceStore.English;

        Current = normalized;
        Epoch++;
        await store.SetAsync(normalized);
        ApplyCultureInfo(normalized);
        await ApplyToDocumentAsync(normalized);

        if (Changed is not null)
        {
            await Changed.Invoke();
        }
    }

    private static void ApplyCultureInfo(string culture)
    {
        var info = CultureInfo.GetCultureInfo(culture);
        CultureInfo.DefaultThreadCurrentCulture = info;
        CultureInfo.DefaultThreadCurrentUICulture = info;
        CultureInfo.CurrentCulture = info;
        CultureInfo.CurrentUICulture = info;
    }

    private async Task ApplyToDocumentAsync(string culture)
    {
        try
        {
            await js.InvokeVoidAsync("exitsPosTheme.applyCulture", culture);
        }
        catch (JSException)
        {
            // WebView not yet ready for interop; safe to ignore, lang attribute is cosmetic.
        }
    }
}
