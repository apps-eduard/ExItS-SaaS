using ExItS.DesignSystem.Abstractions;
using Microsoft.Maui.Storage;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// MAUI implementation of <see cref="IThemePreferenceStore"/> backed by
/// <see cref="Preferences.Default"/>, the platform key/value store (SharedPreferences on
/// Android). Persisted under key <c>exits-pos-theme</c>.
/// </summary>
public sealed class MauiThemePreferenceStore : IThemePreferenceStore
{
    public const string StorageKey = "exits-pos-theme";

    public Task<ThemePreference> GetAsync(CancellationToken ct = default)
    {
        var stored = Preferences.Default.Get(StorageKey, ThemePreference.System.ToString());
        return Task.FromResult(Enum.TryParse<ThemePreference>(stored, out var preference) ? preference : ThemePreference.System);
    }

    public Task SetAsync(ThemePreference preference, CancellationToken ct = default)
    {
        Preferences.Default.Set(StorageKey, preference.ToString());
        return Task.CompletedTask;
    }
}
