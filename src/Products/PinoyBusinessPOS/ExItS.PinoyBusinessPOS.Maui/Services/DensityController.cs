using ExItS.DesignSystem.Abstractions;
using Microsoft.JSInterop;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// Blazor-friendly façade over <see cref="IDensityPreferenceStore"/> for the POS shell.
/// Applies the <c>data-density</c> attribute to the WebView document via JS interop.
/// </summary>
public sealed class DensityController(IDensityPreferenceStore store, IJSRuntime js)
{
    private bool _initialized;

    public DensityMode Current { get; private set; } = DensityMode.Compact;

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

    public async Task<bool> SetDensityAsync(DensityMode density)
    {
        try
        {
            Current = density;
            await store.SetAsync(density);
            await ApplyToDocumentAsync(density);

            if (Changed is not null)
            {
                await Changed.Invoke();
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async Task ApplyToDocumentAsync(DensityMode density)
    {
        try
        {
            await js.InvokeVoidAsync("exitsPosTheme.applyDensity", density.ToString());
        }
        catch (Exception)
        {
            // WebView not yet ready; theme-boot.js already applied a best-effort default.
        }
    }
}
