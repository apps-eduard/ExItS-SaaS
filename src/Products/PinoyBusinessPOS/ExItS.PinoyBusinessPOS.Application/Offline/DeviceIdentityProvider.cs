using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>
/// Cryptographically random DeviceId persisted in secure storage.
/// Survives logout and context switches; regenerates only when storage is unavailable/cleared.
/// </summary>
public sealed class DeviceIdentityProvider(ISecureTokenStore tokens) : IDeviceIdentityProvider
{
    public async Task<string> GetOrCreateDeviceIdAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        string? existing;
        try
        {
            existing = await tokens.GetAsync(SecureTokenKeys.DeviceId, ct).ConfigureAwait(false);
        }
        catch
        {
            // Secure storage unavailable — return an ephemeral id that is not persisted.
            // Callers must not treat this as durable or as auth proof.
            return Guid.NewGuid().ToString("D");
        }

        if (!string.IsNullOrWhiteSpace(existing) && Guid.TryParse(existing, out var parsed) && parsed != Guid.Empty)
        {
            return parsed.ToString("D");
        }

        var created = Guid.NewGuid().ToString("D");
        try
        {
            await tokens.SetAsync(SecureTokenKeys.DeviceId, created, ct).ConfigureAwait(false);
        }
        catch
        {
            return created;
        }

        return created;
    }
}
