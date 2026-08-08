using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class OfflineOperatingGrantServiceTests
{
    [Fact]
    public void Pin_hasher_rejects_non_digit_and_short_pins()
    {
        Assert.False(OfflinePinHasher.IsValidPinFormat("12345", 6));
        Assert.False(OfflinePinHasher.IsValidPinFormat("12ab56", 6));
        Assert.True(OfflinePinHasher.IsValidPinFormat("123456", 6));
    }

    [Fact]
    public void Pin_hasher_round_trips_and_rejects_wrong_pin()
    {
        var verifier = OfflinePinHasher.Create("654321", iterations: 10_000);
        Assert.True(OfflinePinHasher.Verify("654321", verifier));
        Assert.False(OfflinePinHasher.Verify("000000", verifier));
    }

    [Fact]
    public async Task Valid_grant_correct_pin_unlocks_without_extending_expiry()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await SeedAsync(clock);

        var before = (await harness.Store.LoadGrantAsync())!;
        var cold = harness.CreateColdStartService();
        var unlock = await cold.UnlockWithPinAsync("123456");

        Assert.Equal(OfflinePinUnlockStatus.Succeeded, unlock.Status);
        Assert.True(cold.IsUnlockedThisProcess);
        var after = (await harness.Store.LoadGrantAsync())!;
        Assert.Equal(before.ExpiresAtUtc, after.ExpiresAtUtc);
        Assert.Equal(before.LastOnlineValidatedAtUtc, after.LastOnlineValidatedAtUtc);
    }

    [Fact]
    public async Task Wrong_pin_is_denied()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await SeedAsync(clock);
        var cold = harness.CreateColdStartService();

        var unlock = await cold.UnlockWithPinAsync("999999");
        Assert.Equal(OfflinePinUnlockStatus.WrongPin, unlock.Status);
        Assert.False(cold.IsUnlockedThisProcess);
    }

    [Fact]
    public async Task Expired_grant_is_denied()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await SeedAsync(clock, durationHours: 1);
        clock.UtcNow = DateTimeOffset.Parse("2026-08-08T02:00:00Z");

        var cold = harness.CreateColdStartService();
        var offer = await cold.EvaluateColdStartOfferAsync();
        Assert.False(offer.CanOfferPinUnlock);
        Assert.Equal("offline_grant_expired", offer.DenialReasonCode);

        var unlock = await cold.UnlockWithPinAsync("123456");
        Assert.Equal(OfflinePinUnlockStatus.GrantExpired, unlock.Status);
    }

    [Fact]
    public async Task Different_device_is_denied()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await SeedAsync(clock);
        harness.Device.DeviceId = "other-device-id";

        var cold = harness.CreateColdStartService();
        var unlock = await cold.UnlockWithPinAsync("123456");
        Assert.Equal(OfflinePinUnlockStatus.DeviceMismatch, unlock.Status);
    }

    [Fact]
    public async Task Pin_never_extends_grant_expiry_on_repeated_unlock()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await SeedAsync(clock, durationHours: 24);
        var originalExpiry = (await harness.Store.LoadGrantAsync())!.ExpiresAtUtc;

        clock.UtcNow = DateTimeOffset.Parse("2026-08-08T12:00:00Z");
        var cold = harness.CreateColdStartService();
        Assert.Equal(OfflinePinUnlockStatus.Succeeded, (await cold.UnlockWithPinAsync("123456")).Status);
        Assert.Equal(originalExpiry, (await harness.Store.LoadGrantAsync())!.ExpiresAtUtc);
    }

    [Fact]
    public async Task Cold_start_offer_requires_configured_pin()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var options = CreateOptions(24);
        var store = new MemoryOfflineGrantStore();
        var device = new FakeDevice("device-a");
        var sut = new OfflineOperatingGrantService(store, device, options, clock);

        await sut.EstablishFromOnlineSessionAsync(OnlineSession(), device.DeviceId, "Cashier");
        var offer = await sut.EvaluateColdStartOfferAsync();
        Assert.False(offer.CanOfferPinUnlock);
        Assert.Equal("offline_pin_not_configured", offer.DenialReasonCode);
    }

    [Fact]
    public async Task Clear_drops_grant_but_keeps_pin_verifier()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await SeedAsync(clock);
        await harness.OnlineService.ClearAsync();

        Assert.Null(await harness.Store.LoadGrantAsync());
        Assert.NotNull(await harness.Store.LoadPinVerifierAsync());
    }

    private static async Task<Harness> SeedAsync(FakeClock clock, int durationHours = 24)
    {
        var options = CreateOptions(durationHours);
        var store = new MemoryOfflineGrantStore();
        var device = new FakeDevice("device-a");
        var sut = new OfflineOperatingGrantService(store, device, options, clock);
        var session = OnlineSession();
        await sut.EstablishFromOnlineSessionAsync(session, device.DeviceId, "Cashier");
        Assert.True((await sut.SetPinAsync("123456")).Succeeded);
        return new Harness(sut, store, device, options, clock);
    }

    private static IOptions<OfflineOperatingGrantOptions> CreateOptions(int durationHours) =>
        Options.Create(new OfflineOperatingGrantOptions
        {
            DurationHours = durationHours,
            PinMinLength = 6,
            MaxFailedPinAttempts = 5,
            PinLockoutMinutes = 15,
            PinHashIterations = 10_000
        });

    private static AuthSession OnlineSession() =>
        new(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Cashier One",
            "cashier1",
            "cashier@example.com",
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Test Store",
            DateTimeOffset.Parse("2026-08-08T00:00:00Z"),
            DateTimeOffset.Parse("2026-08-09T00:00:00Z"),
            HasPosAccess: true,
            AccessReasonCode: "allowed",
            SubscriptionStatus: "Active",
            EnabledFeatureCodes: ["pos.sell"]);

    private sealed record Harness(
        OfflineOperatingGrantService OnlineService,
        MemoryOfflineGrantStore Store,
        FakeDevice Device,
        IOptions<OfflineOperatingGrantOptions> Options,
        FakeClock Clock)
    {
        public OfflineOperatingGrantService CreateColdStartService() =>
            new(Store, Device, Options, Clock);
    }

    private sealed class FakeClock(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    private sealed class FakeDevice(string deviceId) : IDeviceIdentityProvider
    {
        public string DeviceId { get; set; } = deviceId;

        public Task<string> GetOrCreateDeviceIdAsync(CancellationToken ct = default) =>
            Task.FromResult(DeviceId);
    }

    private sealed class MemoryOfflineGrantStore : IOfflineOperatingGrantStore
    {
        private OfflineOperatingGrant? _grant;
        private OfflinePinVerifier? _pin;

        public Task<OfflineOperatingGrant?> LoadGrantAsync(CancellationToken ct = default) =>
            Task.FromResult(_grant);

        public Task SaveGrantAsync(OfflineOperatingGrant grant, CancellationToken ct = default)
        {
            _grant = grant;
            return Task.CompletedTask;
        }

        public Task ClearGrantAsync(CancellationToken ct = default)
        {
            _grant = null;
            return Task.CompletedTask;
        }

        public Task<OfflinePinVerifier?> LoadPinVerifierAsync(CancellationToken ct = default) =>
            Task.FromResult(_pin);

        public Task SavePinVerifierAsync(OfflinePinVerifier verifier, CancellationToken ct = default)
        {
            _pin = verifier;
            return Task.CompletedTask;
        }

        public Task ClearPinVerifierAsync(CancellationToken ct = default)
        {
            _pin = null;
            return Task.CompletedTask;
        }
    }
}
