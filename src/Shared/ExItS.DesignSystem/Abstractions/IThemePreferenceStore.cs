namespace ExItS.DesignSystem.Abstractions;

/// <summary>
/// Persists the user's theme preference. Host applications (Platform Admin, POS) provide the
/// concrete implementation (e.g. browser localStorage, MAUI Preferences); this library only
/// defines the contract and must not depend on any host-specific storage API.
/// </summary>
public interface IThemePreferenceStore
{
    Task<ThemePreference> GetAsync(CancellationToken ct = default);

    Task SetAsync(ThemePreference preference, CancellationToken ct = default);
}
