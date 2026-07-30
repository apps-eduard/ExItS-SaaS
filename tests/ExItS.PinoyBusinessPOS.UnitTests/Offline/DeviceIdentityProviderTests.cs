using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.UnitTests.Offline;

public sealed class DeviceIdentityProviderTests
{
    [Fact]
    public async Task GetOrCreate_returns_stable_guid_across_calls()
    {
        var tokens = new MemorySecureTokenStore();
        var sut = new DeviceIdentityProvider(tokens);

        var first = await sut.GetOrCreateDeviceIdAsync();
        var second = await sut.GetOrCreateDeviceIdAsync();

        Assert.False(string.IsNullOrWhiteSpace(first));
        Assert.True(Guid.TryParse(first, out _));
        Assert.Equal(first, second);
        Assert.Equal(first, await tokens.GetAsync(SecureTokenKeys.DeviceId));
    }

    [Fact]
    public async Task DeviceId_survives_session_key_clear()
    {
        var tokens = new MemorySecureTokenStore();
        var sut = new DeviceIdentityProvider(tokens);
        var id = await sut.GetOrCreateDeviceIdAsync();

        await tokens.SetAsync(SecureTokenKeys.UserId, Guid.NewGuid().ToString("D"));
        await tokens.ClearAllSessionKeysAsync();

        Assert.Null(await tokens.GetAsync(SecureTokenKeys.UserId));
        Assert.Equal(id, await tokens.GetAsync(SecureTokenKeys.DeviceId));
        Assert.Equal(id, await sut.GetOrCreateDeviceIdAsync());
    }

    [Fact]
    public async Task Missing_storage_value_creates_new_device_id()
    {
        var tokens = new MemorySecureTokenStore();
        var sut = new DeviceIdentityProvider(tokens);
        var first = await sut.GetOrCreateDeviceIdAsync();
        await tokens.ClearAsync(SecureTokenKeys.DeviceId);
        var second = await sut.GetOrCreateDeviceIdAsync();

        Assert.NotEqual(first, second);
        Assert.True(Guid.TryParse(second, out _));
    }

    [Fact]
    public async Task DeviceId_is_not_derived_from_user_or_org()
    {
        var tokens = new MemorySecureTokenStore();
        var sut = new DeviceIdentityProvider(tokens);
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var orgId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        await tokens.SetAsync(SecureTokenKeys.UserId, userId.ToString("D"));

        var deviceId = await sut.GetOrCreateDeviceIdAsync();

        Assert.NotEqual(userId.ToString("D"), deviceId);
        Assert.DoesNotContain(userId.ToString("N"), deviceId, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(orgId.ToString("N"), deviceId, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("IMEI", deviceId, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("MAC", deviceId, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class MemorySecureTokenStore : ISecureTokenStore
    {
        private readonly Dictionary<string, string> _map = new(StringComparer.Ordinal);

        public Task<string?> GetAsync(string key, CancellationToken ct = default) =>
            Task.FromResult(_map.TryGetValue(key, out var value) ? value : null);

        public Task SetAsync(string key, string value, CancellationToken ct = default)
        {
            _map[key] = value;
            return Task.CompletedTask;
        }

        public Task ClearAsync(string key, CancellationToken ct = default)
        {
            _map.Remove(key);
            return Task.CompletedTask;
        }

        public Task ClearAllSessionKeysAsync(CancellationToken ct = default)
        {
            _map.Remove(SecureTokenKeys.UserId);
            _map.Remove(SecureTokenKeys.SessionMarker);
            _map.Remove(SecureTokenKeys.IssuedAtUtc);
            _map.Remove(SecureTokenKeys.ExpiresAtUtc);
            _map.Remove(SecureTokenKeys.SubscriptionStatus);
            _map.Remove(SecureTokenKeys.FeatureGrants);
            return Task.CompletedTask;
        }
    }
}
