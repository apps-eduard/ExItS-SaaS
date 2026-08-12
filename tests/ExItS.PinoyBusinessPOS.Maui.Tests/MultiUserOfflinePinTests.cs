using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class MultiUserOfflinePinTests
{
    private static readonly Guid MicaId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid PaulId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OrgId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid BranchId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid PosDeviceId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    [Fact]
    public async Task Mica_enrolls_and_unlocks_offline()
    {
        var harness = await CreateHarnessAsync();
        await EnrollAsync(harness, MicaSession(), "111111");

        harness.Service.LockThisProcess();
        var unlock = await harness.Service.UnlockWithPinAsync(MicaId, "111111");
        Assert.Equal(OfflinePinUnlockStatus.Succeeded, unlock.Status);
        Assert.Equal(MicaId, unlock.Grant!.UserId);
        Assert.True(harness.Service.IsUnlockedThisProcess);
    }

    [Fact]
    public async Task Logout_preserves_Mica_enrollment()
    {
        var harness = await CreateHarnessAsync();
        await EnrollAsync(harness, MicaSession(), "111111");
        harness.Service.LockThisProcess();

        Assert.NotNull(await harness.Store.LoadGrantAsync(MicaId));
        Assert.NotNull(await harness.Store.LoadPinVerifierAsync(MicaId));
        Assert.True(await harness.Service.HasPinConfiguredAsync(MicaId));
        Assert.True((await harness.Service.EvaluateColdStartOfferAsync()).CanOfferPinUnlock);
    }

    [Fact]
    public async Task Paul_enrolls_without_removing_Mica()
    {
        var harness = await CreateHarnessAsync();
        await EnrollAsync(harness, MicaSession(), "111111");
        harness.Service.LockThisProcess();
        await EnrollAsync(harness, PaulSession(), "222222");

        Assert.NotNull(await harness.Store.LoadGrantAsync(MicaId));
        Assert.NotNull(await harness.Store.LoadPinVerifierAsync(MicaId));
        Assert.NotNull(await harness.Store.LoadGrantAsync(PaulId));
        Assert.NotNull(await harness.Store.LoadPinVerifierAsync(PaulId));
    }

    [Fact]
    public async Task Both_users_unlock_independently()
    {
        var harness = await CreateHarnessAsync();
        await EnrollAsync(harness, MicaSession(), "111111");
        harness.Service.LockThisProcess();
        await EnrollAsync(harness, PaulSession(), "222222");
        harness.Service.LockThisProcess();

        Assert.Equal(
            OfflinePinUnlockStatus.Succeeded,
            (await harness.Service.UnlockWithPinAsync(MicaId, "111111")).Status);
        harness.Service.LockThisProcess();
        Assert.Equal(
            OfflinePinUnlockStatus.Succeeded,
            (await harness.Service.UnlockWithPinAsync(PaulId, "222222")).Status);
    }

    [Fact]
    public async Task Cross_pin_fails_Mica_pin_does_not_unlock_Paul()
    {
        var harness = await CreateHarnessAsync();
        await EnrollAsync(harness, MicaSession(), "111111");
        harness.Service.LockThisProcess();
        await EnrollAsync(harness, PaulSession(), "222222");
        harness.Service.LockThisProcess();

        var cross = await harness.Service.UnlockWithPinAsync(PaulId, "111111");
        Assert.Equal(OfflinePinUnlockStatus.WrongPin, cross.Status);
        Assert.False(harness.Service.IsUnlockedThisProcess);
    }

    [Fact]
    public async Task Same_numeric_pin_value_is_isolated_per_user()
    {
        var harness = await CreateHarnessAsync();
        await EnrollAsync(harness, MicaSession(), "555555");
        harness.Service.LockThisProcess();
        await EnrollAsync(harness, PaulSession(), "555555");
        harness.Service.LockThisProcess();

        Assert.Equal(
            OfflinePinUnlockStatus.Succeeded,
            (await harness.Service.UnlockWithPinAsync(MicaId, "555555")).Status);
        harness.Service.LockThisProcess();
        Assert.Equal(
            OfflinePinUnlockStatus.Succeeded,
            (await harness.Service.UnlockWithPinAsync(PaulId, "555555")).Status);

        var micaPin = await harness.Store.LoadPinVerifierAsync(MicaId);
        var paulPin = await harness.Store.LoadPinVerifierAsync(PaulId);
        Assert.NotNull(micaPin);
        Assert.NotNull(paulPin);
        // Same PIN digits still produce distinct salts/hashes per enrollment.
        Assert.NotEqual(micaPin!.SaltBase64, paulPin!.SaltBase64);
        Assert.Equal(MicaId, micaPin.UserId);
        Assert.Equal(PaulId, paulPin.UserId);
    }

    [Fact]
    public async Task GetEnrolledUsers_lists_both_safely_without_hash_secrets()
    {
        var harness = await CreateHarnessAsync();
        await EnrollAsync(harness, MicaSession(), "111111");
        harness.Service.LockThisProcess();
        await EnrollAsync(harness, PaulSession(), "222222");

        var enrolled = await harness.Service.GetEnrolledUsersAsync();
        Assert.Equal(2, enrolled.Count);
        Assert.Contains(enrolled, u => u.UserId == MicaId && u.HasPinConfigured && u.DisplayName.Contains("Mica", StringComparison.Ordinal));
        Assert.Contains(enrolled, u => u.UserId == PaulId && u.HasPinConfigured && u.DisplayName.Contains("Paul", StringComparison.Ordinal));

        foreach (var summary in enrolled)
        {
            Assert.NotEqual(Guid.Empty, summary.UserId);
            Assert.False(string.IsNullOrWhiteSpace(summary.DisplayName));
            Assert.True(summary.HasPinConfigured);
            // Summary must not expose verifier material — only safe directory fields exist.
            var json = JsonSerializer.Serialize(summary);
            Assert.DoesNotContain("HashBase64", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SaltBase64", json, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PBKDF2", json, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Non_enrolled_user_cannot_unlock()
    {
        var harness = await CreateHarnessAsync();
        await EnrollAsync(harness, MicaSession(), "111111");
        harness.Service.LockThisProcess();

        var stranger = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var unlock = await harness.Service.UnlockWithPinAsync(stranger, "111111");
        Assert.Equal(OfflinePinUnlockStatus.GrantMissing, unlock.Status);
    }

    [Fact]
    public async Task Remove_one_user_keeps_the_other()
    {
        var harness = await CreateHarnessAsync();
        await EnrollAsync(harness, MicaSession(), "111111");
        harness.Service.LockThisProcess();
        await EnrollAsync(harness, PaulSession(), "222222");

        await harness.Service.RemoveEnrolledUserAsync(PaulId);

        Assert.Null(await harness.Store.LoadGrantAsync(PaulId));
        Assert.Null(await harness.Store.LoadPinVerifierAsync(PaulId));
        Assert.NotNull(await harness.Store.LoadGrantAsync(MicaId));
        Assert.NotNull(await harness.Store.LoadPinVerifierAsync(MicaId));
        Assert.Equal(
            OfflinePinUnlockStatus.Succeeded,
            (await harness.Service.UnlockWithPinAsync(MicaId, "111111")).Status);
    }

    [Fact]
    public async Task Lockout_is_isolated_per_user()
    {
        var harness = await CreateHarnessAsync(maxFailed: 3);
        await EnrollAsync(harness, MicaSession(), "111111");
        harness.Service.LockThisProcess();
        await EnrollAsync(harness, PaulSession(), "222222");
        harness.Service.LockThisProcess();

        for (var i = 0; i < 3; i++)
        {
            var fail = await harness.Service.UnlockWithPinAsync(MicaId, "000000");
            if (i < 2)
            {
                Assert.Equal(OfflinePinUnlockStatus.WrongPin, fail.Status);
            }
            else
            {
                Assert.Equal(OfflinePinUnlockStatus.Locked, fail.Status);
            }
        }

        Assert.Equal(
            OfflinePinUnlockStatus.Succeeded,
            (await harness.Service.UnlockWithPinAsync(PaulId, "222222")).Status);
    }

    [Fact]
    public async Task Grant_expires_after_duration_and_pin_unlock_does_not_extend_ExpiresAtUtc()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await CreateHarnessAsync(clock, durationHours: 1);
        await EnrollAsync(harness, MicaSession(), "111111");
        var expiry = (await harness.Store.LoadGrantAsync(MicaId))!.ExpiresAtUtc;
        Assert.Equal(clock.GetUtcNow().AddHours(1), expiry);

        harness.Service.LockThisProcess();
        clock.UtcNow = DateTimeOffset.Parse("2026-08-08T00:30:00Z");
        Assert.Equal(
            OfflinePinUnlockStatus.Succeeded,
            (await harness.Service.UnlockWithPinAsync(MicaId, "111111")).Status);
        Assert.Equal(expiry, (await harness.Store.LoadGrantAsync(MicaId))!.ExpiresAtUtc);

        harness.Service.LockThisProcess();
        clock.UtcNow = DateTimeOffset.Parse("2026-08-08T01:05:00Z");
        var expired = await harness.Service.UnlockWithPinAsync(MicaId, "111111");
        Assert.Equal(OfflinePinUnlockStatus.GrantExpired, expired.Status);
    }

    [Fact]
    public async Task ForceExpireGrantForDevelopment_requires_AllowDevelopmentExpiryOverride()
    {
        var harness = await CreateHarnessAsync(allowDevExpiry: false);
        await EnrollAsync(harness, MicaSession(), "111111");

        Assert.False(await harness.Service.ForceExpireGrantForDevelopmentAsync(MicaId));
        Assert.False((await harness.Store.LoadGrantAsync(MicaId))!.IsExpired(harness.Clock.GetUtcNow()));

        var enabled = await CreateHarnessAsync(allowDevExpiry: true);
        await EnrollAsync(enabled, MicaSession(), "111111");
        Assert.True(await enabled.Service.ForceExpireGrantForDevelopmentAsync(MicaId));
        Assert.True((await enabled.Store.LoadGrantAsync(MicaId))!.IsExpired(enabled.Clock.GetUtcNow()));
    }

    [Fact]
    public void Default_DurationHours_option_is_720()
    {
        Assert.Equal(720, new OfflineOperatingGrantOptions().DurationHours);
    }

    [Fact]
    public async Task Legacy_migration_moves_attributable_keys_and_is_idempotent()
    {
        var tokens = new MemorySecureTokenStoreForPin();
        var store = new OfflineOperatingGrantStore(tokens);
        var now = DateTimeOffset.Parse("2026-08-08T00:00:00Z");
        var grant = new OfflineOperatingGrant(
            OfflineOperatingGrant.CurrentSchemaVersion,
            MicaId,
            OrgId,
            "Test Store",
            "device-a",
            "Cashier",
            ["pos.sell"],
            "Active",
            "Mica",
            "mica",
            "mica@example.com",
            now,
            now,
            now.AddHours(720),
            OfflineGrantScopeKind.Organization,
            BranchId,
            PosDeviceId);
        var pin = OfflinePinHasher.Create("111111", 10_000, MicaId);
        await tokens.SetAsync(SecureTokenKeys.OfflineOperatingGrant, JsonSerializer.Serialize(grant, JsonOptions));
        await tokens.SetAsync(SecureTokenKeys.OfflinePinVerifier, JsonSerializer.Serialize(pin, JsonOptions));

        await store.EnsureMigratedAsync();

        Assert.NotNull(await store.LoadGrantAsync(MicaId));
        Assert.NotNull(await store.LoadPinVerifierAsync(MicaId));
        Assert.Null(await tokens.GetAsync(SecureTokenKeys.OfflineOperatingGrant));
        Assert.Null(await tokens.GetAsync(SecureTokenKeys.OfflinePinVerifier));

        await store.EnsureMigratedAsync();
        Assert.NotNull(await store.LoadGrantAsync(MicaId));
        Assert.Single(await store.GetEnrolledUsersAsync());
    }

    [Fact]
    public async Task Legacy_migration_leaves_corrupt_or_unbound_legacy_securely()
    {
        var tokens = new MemorySecureTokenStoreForPin();
        var store = new OfflineOperatingGrantStore(tokens);
        await tokens.SetAsync(SecureTokenKeys.OfflineOperatingGrant, "{not-json");
        await tokens.SetAsync(SecureTokenKeys.OfflinePinVerifier, """{"algorithm":"PBKDF2-SHA256","iterations":10000,"saltBase64":"c2FsdA==","hashBase64":"aGFzaA==","failedAttempts":0,"lockedUntilUtc":null}""");

        await store.EnsureMigratedAsync();

        Assert.Equal("{not-json", await tokens.GetAsync(SecureTokenKeys.OfflineOperatingGrant));
        Assert.NotNull(await tokens.GetAsync(SecureTokenKeys.OfflinePinVerifier));
        Assert.Empty(await store.GetEnrolledUsersAsync());
    }

    private static async Task EnrollAsync(Harness harness, AuthSession session, string pin)
    {
        await harness.Service.EstablishFromOnlineSessionAsync(session, harness.Device.DeviceId, "Cashier");
        Assert.True((await harness.Service.SetPinAsync(pin)).Succeeded);
    }

    private static async Task<Harness> CreateHarnessAsync(
        FakeClock? clock = null,
        int durationHours = 720,
        int maxFailed = 5,
        bool allowDevExpiry = false)
    {
        clock ??= new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var options = Options.Create(new OfflineOperatingGrantOptions
        {
            DurationHours = durationHours,
            PinMinLength = 6,
            MaxFailedPinAttempts = maxFailed,
            PinLockoutMinutes = 15,
            PinHashIterations = 10_000,
            AllowDevelopmentExpiryOverride = allowDevExpiry
        });
        var store = new MemoryOfflineGrantStore();
        var device = new FakeDevice("device-a");
        var service = new OfflineOperatingGrantService(store, device, options, clock);
        return new Harness(service, store, device, options, clock);
    }

    private static AuthSession MicaSession() => OrgSession(MicaId, "Mica Cashier", "mica", "mica@example.com");

    private static AuthSession PaulSession() => OrgSession(PaulId, "Paul Cashier", "paul", "paul@example.com");

    private static AuthSession OrgSession(Guid userId, string display, string username, string email) =>
        new(
            userId,
            display,
            username,
            email,
            OrgId,
            "Test Store",
            DateTimeOffset.Parse("2026-08-08T00:00:00Z"),
            DateTimeOffset.Parse("2026-08-09T00:00:00Z"),
            HasPosAccess: true,
            AccessReasonCode: "allowed",
            SubscriptionStatus: "Active",
            EnabledFeatureCodes: ["pos.sell"],
            AccountClass: "Organization",
            BranchId: BranchId,
            PosDeviceId: PosDeviceId);

    private sealed record Harness(
        OfflineOperatingGrantService Service,
        MemoryOfflineGrantStore Store,
        FakeDevice Device,
        IOptions<OfflineOperatingGrantOptions> Options,
        FakeClock Clock);

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

    private sealed class MemorySecureTokenStoreForPin : ISecureTokenStore
    {
        private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

        public Task<string?> GetAsync(string key, CancellationToken ct = default) =>
            Task.FromResult(_values.TryGetValue(key, out var value) ? value : null);

        public Task SetAsync(string key, string value, CancellationToken ct = default)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }

        public Task ClearAsync(string key, CancellationToken ct = default)
        {
            _values.Remove(key);
            return Task.CompletedTask;
        }

        public Task ClearAllSessionKeysAsync(CancellationToken ct = default)
        {
            _values.Clear();
            return Task.CompletedTask;
        }
    }
}
