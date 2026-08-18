using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;

namespace ExItS.PinoyBusinessPOS.Application.Auth;

/// <summary>
/// Per-user device recovery credentials keyed by user id. Never logs or exposes raw secrets.
/// </summary>
public sealed class DeviceRecoveryCredentialStore(ISecureTokenStore tokens) : IDeviceRecoveryCredentialStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SaveAsync(
        Guid userId,
        string deviceId,
        string recoveryCredential,
        DateTimeOffset idleExpiresAtUtc,
        DateTimeOffset absoluteExpiresAtUtc,
        CancellationToken ct = default)
    {
        if (userId == Guid.Empty
            || string.IsNullOrWhiteSpace(deviceId)
            || string.IsNullOrWhiteSpace(recoveryCredential))
        {
            return;
        }

        var payload = JsonSerializer.Serialize(
            new StoredDeviceRecoveryCredential(
                deviceId.Trim(),
                recoveryCredential.Trim(),
                idleExpiresAtUtc,
                absoluteExpiresAtUtc),
            JsonOptions);
        await tokens.SetAsync(SecureTokenKeys.DeviceRecoveryCredentialFor(userId), payload, ct)
            .ConfigureAwait(false);
        await tokens.ClearAsync(SecureTokenKeys.PinRecoveryPlatformSessionFor(userId), ct)
            .ConfigureAwait(false);
    }

    public async Task<StoredDeviceRecoveryCredential?> LoadAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            return null;
        }

        var json = await tokens.GetAsync(SecureTokenKeys.DeviceRecoveryCredentialFor(userId), ct)
            .ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<StoredDeviceRecoveryCredential>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public Task ClearAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            return Task.CompletedTask;
        }

        return tokens.ClearAsync(SecureTokenKeys.DeviceRecoveryCredentialFor(userId), ct);
    }

    public Task<string?> LoadLegacySessionHandleAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            return Task.FromResult<string?>(null);
        }

        return tokens.GetAsync(SecureTokenKeys.PinRecoveryPlatformSessionFor(userId), ct);
    }

    public Task ClearLegacySessionHandleAsync(Guid userId, CancellationToken ct = default)
    {
        if (userId == Guid.Empty)
        {
            return Task.CompletedTask;
        }

        return tokens.ClearAsync(SecureTokenKeys.PinRecoveryPlatformSessionFor(userId), ct);
    }
}

public sealed record StoredDeviceRecoveryCredential(
    string DeviceId,
    string Credential,
    DateTimeOffset IdleExpiresAtUtc,
    DateTimeOffset AbsoluteExpiresAtUtc);
