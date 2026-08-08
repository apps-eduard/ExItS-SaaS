using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>SecureStorage-backed offline grant + PIN verifier. Survives logout intentionally until replaced.</summary>
public sealed class OfflineOperatingGrantStore(ISecureTokenStore tokens) : IOfflineOperatingGrantStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public async Task<OfflineOperatingGrant?> LoadGrantAsync(CancellationToken ct = default)
    {
        var json = await tokens.GetAsync(SecureTokenKeys.OfflineOperatingGrant, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<OfflineOperatingGrant>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public Task SaveGrantAsync(OfflineOperatingGrant grant, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(grant);
        var json = JsonSerializer.Serialize(grant, JsonOptions);
        return tokens.SetAsync(SecureTokenKeys.OfflineOperatingGrant, json, ct);
    }

    public Task ClearGrantAsync(CancellationToken ct = default) =>
        tokens.ClearAsync(SecureTokenKeys.OfflineOperatingGrant, ct);

    public async Task<OfflinePinVerifier?> LoadPinVerifierAsync(CancellationToken ct = default)
    {
        var json = await tokens.GetAsync(SecureTokenKeys.OfflinePinVerifier, ct).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<OfflinePinVerifier>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public Task SavePinVerifierAsync(OfflinePinVerifier verifier, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(verifier);
        var json = JsonSerializer.Serialize(verifier, JsonOptions);
        return tokens.SetAsync(SecureTokenKeys.OfflinePinVerifier, json, ct);
    }

    public Task ClearPinVerifierAsync(CancellationToken ct = default) =>
        tokens.ClearAsync(SecureTokenKeys.OfflinePinVerifier, ct);
}
