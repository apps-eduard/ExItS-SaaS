using ExItS.DesignSystem.Abstractions;
using Microsoft.Maui.Storage;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// MAUI implementation of <see cref="IDensityPreferenceStore"/> backed by
/// <see cref="Preferences.Default"/>. Persisted under key <c>exits-pos-density</c>.
/// Default is <see cref="DensityMode.Compact"/> (POS cashier density).
/// </summary>
public sealed class MauiDensityPreferenceStore : IDensityPreferenceStore
{
    public const string StorageKey = "exits-pos-density";

    public Task<DensityMode> GetAsync(CancellationToken ct = default)
    {
        var stored = Preferences.Default.Get(StorageKey, DensityMode.Compact.ToString());
        return Task.FromResult(Enum.TryParse<DensityMode>(stored, out var density) ? density : DensityMode.Compact);
    }

    public Task SetAsync(DensityMode density, CancellationToken ct = default)
    {
        Preferences.Default.Set(StorageKey, density.ToString());
        return Task.CompletedTask;
    }
}
