using ExItS.DesignSystem.Abstractions;
using Microsoft.Maui.Storage;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// MAUI implementation of <see cref="ICulturePreferenceStore"/> backed by
/// <see cref="Preferences.Default"/>. Persisted under key <c>exits-pos-culture</c>. Only the two
/// cultures supported by this work package (<c>en</c>, <c>fil-PH</c>) are accepted; anything else
/// falls back to <c>en</c>.
/// </summary>
public sealed class MauiCulturePreferenceStore : ICulturePreferenceStore
{
    public const string StorageKey = "exits-pos-culture";
    public const string English = "en";
    public const string Filipino = "fil-PH";

    public Task<string> GetAsync(CancellationToken ct = default)
    {
        var stored = Preferences.Default.Get(StorageKey, English);
        return Task.FromResult(Normalize(stored));
    }

    public Task SetAsync(string culture, CancellationToken ct = default)
    {
        Preferences.Default.Set(StorageKey, Normalize(culture));
        return Task.CompletedTask;
    }

    private static string Normalize(string? culture) => culture == Filipino ? Filipino : English;
}
