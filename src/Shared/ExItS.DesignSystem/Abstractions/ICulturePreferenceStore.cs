namespace ExItS.DesignSystem.Abstractions;

/// <summary>
/// Persists the user's language/culture preference (e.g. "en", "fil-PH"). Host applications
/// provide the concrete implementation; this library only defines the contract.
/// </summary>
public interface ICulturePreferenceStore
{
    Task<string> GetAsync(CancellationToken ct = default);

    Task SetAsync(string culture, CancellationToken ct = default);
}
