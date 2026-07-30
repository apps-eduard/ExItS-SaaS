namespace ExItS.DesignSystem.Abstractions;

/// <summary>
/// Persists the user's density preference. Host applications provide the concrete
/// implementation; this library must not depend on any host-specific storage API.
/// </summary>
public interface IDensityPreferenceStore
{
    Task<DensityMode> GetAsync(CancellationToken ct = default);

    Task SetAsync(DensityMode density, CancellationToken ct = default);
}
