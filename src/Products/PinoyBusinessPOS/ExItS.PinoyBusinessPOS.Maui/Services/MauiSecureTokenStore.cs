using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>MAUI SecureStorage-backed session secret store. Never used for passwords.</summary>
public sealed class MauiSecureTokenStore : ISecureTokenStore
{
    private static readonly string[] SessionKeys =
    [
        SecureTokenKeys.UserId,
        SecureTokenKeys.SessionMarker,
        SecureTokenKeys.IssuedAtUtc,
        SecureTokenKeys.ExpiresAtUtc,
        SecureTokenKeys.SubscriptionStatus,
        SecureTokenKeys.FeatureGrants,
        SecureTokenKeys.AccessToken,
        SecureTokenKeys.PlatformSessionToken,
        SecureTokenKeys.AccountClass,
        SecureTokenKeys.AccountProfileId,
        SecureTokenKeys.OrganizationContextLocked,
        SecureTokenKeys.BranchId,
        SecureTokenKeys.PosDeviceId
    ];

    public async Task<string?> GetAsync(string key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return await SecureStorage.Default.GetAsync(key).ConfigureAwait(false);
    }

    public async Task SetAsync(string key, string value, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await SecureStorage.Default.SetAsync(key, value).ConfigureAwait(false);
    }

    public Task ClearAsync(string key, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        SecureStorage.Default.Remove(key);
        return Task.CompletedTask;
    }

    public Task ClearAllSessionKeysAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        foreach (var key in SessionKeys)
        {
            SecureStorage.Default.Remove(key);
        }

        return Task.CompletedTask;
    }
}
