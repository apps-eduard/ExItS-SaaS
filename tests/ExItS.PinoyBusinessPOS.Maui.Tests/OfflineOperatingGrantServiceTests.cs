using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class OfflineOperatingGrantServiceTests
{
    private static readonly Guid TestUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

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

        var before = (await harness.Store.LoadGrantAsync(TestUserId))!;
        var cold = harness.CreateColdStartService();
        var unlock = await cold.UnlockWithPinAsync(TestUserId, "123456");

        Assert.Equal(OfflinePinUnlockStatus.Succeeded, unlock.Status);
        Assert.True(cold.IsUnlockedThisProcess);
        var after = (await harness.Store.LoadGrantAsync(TestUserId))!;
        Assert.Equal(before.ExpiresAtUtc, after.ExpiresAtUtc);
        Assert.Equal(before.LastOnlineValidatedAtUtc, after.LastOnlineValidatedAtUtc);
    }

    [Fact]
    public async Task Wrong_pin_is_denied()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await SeedAsync(clock);
        var cold = harness.CreateColdStartService();

        var unlock = await cold.UnlockWithPinAsync(TestUserId, "999999");
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

        var unlock = await cold.UnlockWithPinAsync(TestUserId, "123456");
        Assert.Equal(OfflinePinUnlockStatus.GrantExpired, unlock.Status);
    }

    [Fact]
    public async Task Different_device_is_denied()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await SeedAsync(clock);
        harness.Device.DeviceId = "other-device-id";

        var cold = harness.CreateColdStartService();
        var unlock = await cold.UnlockWithPinAsync(TestUserId, "123456");
        Assert.Equal(OfflinePinUnlockStatus.DeviceMismatch, unlock.Status);
    }

    [Fact]
    public async Task Pin_never_extends_grant_expiry_on_repeated_unlock()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await SeedAsync(clock, durationHours: 720);
        var originalExpiry = (await harness.Store.LoadGrantAsync(TestUserId))!.ExpiresAtUtc;

        clock.UtcNow = DateTimeOffset.Parse("2026-08-08T12:00:00Z");
        var cold = harness.CreateColdStartService();
        Assert.Equal(
            OfflinePinUnlockStatus.Succeeded,
            (await cold.UnlockWithPinAsync(TestUserId, "123456")).Status);
        Assert.Equal(originalExpiry, (await harness.Store.LoadGrantAsync(TestUserId))!.ExpiresAtUtc);
    }

    [Fact]
    public async Task Cold_start_offer_requires_configured_pin()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var options = CreateOptions(720);
        var store = new MemoryOfflineGrantStore();
        var device = new FakeDevice("device-a");
        var sut = new OfflineOperatingGrantService(store, device, options, clock);

        await sut.EstablishFromOnlineSessionAsync(OnlineSession(), device.DeviceId, "Cashier");
        var offer = await sut.EvaluateColdStartOfferAsync();
        Assert.False(offer.CanOfferPinUnlock);
        Assert.Equal("offline_pin_not_configured", offer.DenialReasonCode);
    }

    [Fact]
    public async Task Process_lock_and_new_service_instance_still_offer_persisted_pin()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await SeedAsync(clock);
        Assert.True((await harness.OnlineService.EvaluateColdStartOfferAsync()).CanOfferPinUnlock);

        harness.OnlineService.LockThisProcess();
        Assert.False(harness.OnlineService.IsUnlockedThisProcess);
        Assert.Null(harness.OnlineService.ActiveUnlockedGrant);
        Assert.True((await harness.OnlineService.EvaluateColdStartOfferAsync()).CanOfferPinUnlock);

        var cold = harness.CreateColdStartService();
        Assert.True((await cold.EvaluateColdStartOfferAsync()).CanOfferPinUnlock);
        Assert.Equal(
            OfflinePinUnlockStatus.Succeeded,
            (await cold.UnlockWithPinAsync(TestUserId, "123456")).Status);
    }

    [Fact]
    public async Task Clear_drops_grant_but_keeps_pin_verifier()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await SeedAsync(clock);
        await harness.OnlineService.ClearAsync();

        Assert.Null(await harness.Store.LoadGrantAsync(TestUserId));
        Assert.NotNull(await harness.Store.LoadPinVerifierAsync(TestUserId));
    }

    [Fact]
    public async Task Personal_session_establishes_personal_grant_without_organization()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var options = CreateOptions(720);
        var store = new MemoryOfflineGrantStore();
        var device = new FakeDevice("device-a");
        var sut = new OfflineOperatingGrantService(store, device, options, clock);

        await sut.EstablishFromOnlineSessionAsync(PersonalSession(), device.DeviceId, roleCode: null);
        var grant = await store.LoadGrantAsync(TestUserId);
        Assert.NotNull(grant);
        Assert.Equal(OfflineOperatingGrant.CurrentSchemaVersion, grant.SchemaVersion);
        Assert.True(grant.IsPersonalScope);
        Assert.Null(grant.OrganizationId);
        Assert.Equal(OfflineGrantScopeKind.Personal, grant.ScopeKind);
    }

    [Fact]
    public async Task Unspecified_account_class_without_org_can_establish_personal_grant()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var options = CreateOptions(720);
        var store = new MemoryOfflineGrantStore();
        var device = new FakeDevice("device-a");
        var sut = new OfflineOperatingGrantService(store, device, options, clock);

        var session = PersonalSession() with { AccountClass = null };
        await sut.EstablishFromOnlineSessionAsync(session, device.DeviceId, roleCode: null);
        Assert.NotNull(await store.LoadGrantAsync(TestUserId));
        Assert.True((await sut.SetPinAsync("123456")).Succeeded);
        Assert.True((await sut.EvaluateColdStartOfferAsync()).CanOfferPinUnlock);
        Assert.Equal(OfflinePinEligibilityReason.Eligible, (await sut.EvaluateColdStartOfferAsync()).EligibilityReason);
    }

    [Fact]
    public async Task Cold_start_reasons_cover_missing_pin_device_and_expiry()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var options = CreateOptions(1);
        var store = new MemoryOfflineGrantStore();
        var device = new FakeDevice("device-a");
        var sut = new OfflineOperatingGrantService(store, device, options, clock);
        await sut.EstablishFromOnlineSessionAsync(OnlineSession(), device.DeviceId, "Cashier");

        var missingPin = await sut.EvaluateColdStartOfferAsync();
        Assert.False(missingPin.CanOfferPinUnlock);
        Assert.Equal(OfflinePinEligibilityReason.NoPinVerifier, missingPin.EligibilityReason);

        Assert.True((await sut.SetPinAsync("123456")).Succeeded);
        var mismatch = new OfflineOperatingGrantService(store, new FakeDevice("other-device"), options, clock);
        var deviceOffer = await mismatch.EvaluateColdStartOfferAsync();
        Assert.False(deviceOffer.CanOfferPinUnlock);
        Assert.Equal(OfflinePinEligibilityReason.DeviceMismatch, deviceOffer.EligibilityReason);

        clock.UtcNow = clock.UtcNow.AddHours(2);
        var expired = await new OfflineOperatingGrantService(store, device, options, clock)
            .EvaluateColdStartOfferAsync();
        Assert.False(expired.CanOfferPinUnlock);
        Assert.Equal(OfflinePinEligibilityReason.Expired, expired.EligibilityReason);
        Assert.False(expired.RequiresPinEnrollment);
    }

    [Fact]
    public async Task User_readiness_rejects_orphan_verifier_and_missing_pin()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var options = CreateOptions(720);
        var store = new MemoryOfflineGrantStore();
        var device = new FakeDevice("device-a");
        var sut = new OfflineOperatingGrantService(store, device, options, clock);
        var userId = OnlineSession().UserId;

        var empty = await sut.EvaluateUserReadinessAsync(userId);
        Assert.False(empty.CanOfferPinUnlock);
        Assert.Equal(OfflinePinEligibilityReason.NoStoredIdentity, empty.EligibilityReason);
        Assert.True(empty.RequiresPinEnrollment);

        await store.SavePinVerifierAsync(userId, OfflinePinHasher.Create("123456", 10_000, userId));
        var orphanPin = await sut.EvaluateUserReadinessAsync(userId);
        Assert.False(orphanPin.CanOfferPinUnlock);
        Assert.Equal(OfflinePinEligibilityReason.NoGrant, orphanPin.EligibilityReason);
        Assert.True(orphanPin.RequiresPinEnrollment);
        Assert.False((await sut.EvaluateColdStartOfferAsync()).CanOfferPinUnlock);

        await store.ClearPinVerifierAsync(userId);
        await sut.EstablishFromOnlineSessionAsync(OnlineSession(), device.DeviceId, "Cashier");
        var missingPin = await sut.EvaluateUserReadinessAsync(userId);
        Assert.False(missingPin.CanOfferPinUnlock);
        Assert.Equal(OfflinePinEligibilityReason.NoPinVerifier, missingPin.EligibilityReason);
        Assert.True(missingPin.RequiresPinEnrollment);

        Assert.True((await sut.SetPinAsync("123456")).Succeeded);
        var ready = await sut.EvaluateUserReadinessAsync(userId);
        Assert.True(ready.CanOfferPinUnlock);
        Assert.False(ready.RequiresPinEnrollment);
        Assert.Equal(OfflinePinEligibilityReason.Eligible, ready.EligibilityReason);
    }

    [Fact]
    public async Task Pin_save_failure_does_not_complete_setup()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var options = CreateOptions(720);
        var store = new ThrowingPinStore();
        var device = new FakeDevice("device-a");
        var sut = new OfflineOperatingGrantService(store, device, options, clock);
        await sut.EstablishFromOnlineSessionAsync(OnlineSession(), device.DeviceId, "Cashier");

        store.ThrowOnSave = true;
        var result = await sut.SetPinAsync("123456");
        Assert.False(result.Succeeded);
        Assert.Equal("Auth_SecureStorageFailure", result.SafeMessageKey);
        Assert.False(await sut.HasPinConfiguredAsync(TestUserId));
        Assert.False((await sut.EvaluateUserReadinessAsync(TestUserId)).CanOfferPinUnlock);
    }

    [Fact]
    public async Task Ineligible_unlock_is_not_wrong_pin()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var options = CreateOptions(720);
        var store = new MemoryOfflineGrantStore();
        var device = new FakeDevice("device-a");
        var sut = new OfflineOperatingGrantService(store, device, options, clock);

        var firstTime = await sut.UnlockWithPinAsync("123456");
        Assert.NotEqual(OfflinePinUnlockStatus.WrongPin, firstTime.Status);
        Assert.NotEqual("Offline_PinWrong", firstTime.SafeMessageKey);

        await sut.EstablishFromOnlineSessionAsync(OnlineSession(), device.DeviceId, "Cashier");
        var missingPin = await sut.UnlockWithPinAsync(TestUserId, "123456");
        Assert.Equal(OfflinePinUnlockStatus.PinNotConfigured, missingPin.Status);
        Assert.Equal("Offline_PinNotConfigured", missingPin.SafeMessageKey);
    }

    [Fact]
    public async Task Organization_grant_still_requires_org_and_pos_access()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var options = CreateOptions(720);
        var store = new MemoryOfflineGrantStore();
        var device = new FakeDevice("device-a");
        var sut = new OfflineOperatingGrantService(store, device, options, clock);

        var noAccess = OnlineSession() with { HasPosAccess = false };
        await sut.EstablishFromOnlineSessionAsync(noAccess, device.DeviceId, "Cashier");
        Assert.Null(await store.LoadGrantAsync(TestUserId));

        var noOrg = OnlineSession() with { OrganizationId = null, HasPosAccess = true };
        await sut.EstablishFromOnlineSessionAsync(noOrg, device.DeviceId, "Cashier");
        Assert.Null(await store.LoadGrantAsync(TestUserId));
    }

    [Fact]
    public async Task Staff_org_locked_session_does_not_establish_personal_grant()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var options = CreateOptions(720);
        var store = new MemoryOfflineGrantStore();
        var device = new FakeDevice("device-a");
        var sut = new OfflineOperatingGrantService(store, device, options, clock);

        var locked = PersonalSession() with
        {
            OrganizationContextLocked = true,
            OrganizationId = Guid.Parse("22222222-2222-2222-2222-222222222222")
        };
        await sut.EstablishFromOnlineSessionAsync(locked, device.DeviceId, null);
        Assert.Null(await store.LoadGrantAsync(TestUserId));
    }

    [Fact]
    public async Task Cold_start_accepts_legacy_schema_version_1_as_organization()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await SeedAsync(clock);
        var existing = (await harness.Store.LoadGrantAsync(TestUserId))!;
        await harness.Store.SaveGrantAsync(existing with
        {
            SchemaVersion = OfflineOperatingGrant.LegacySchemaVersion,
            ScopeKind = OfflineGrantScopeKind.Organization
        });
        Assert.True((await harness.OnlineService.SetPinAsync("123456")).Succeeded);

        var cold = harness.CreateColdStartService();
        var offer = await cold.EvaluateColdStartOfferAsync();
        Assert.True(offer.CanOfferPinUnlock);
        Assert.NotNull(offer.Grant);
        Assert.True(offer.Grant.IsOrganizationScope);
    }

    [Fact]
    public async Task Personal_and_organization_grants_are_scope_isolated_helpers()
    {
        var org = new OfflineOperatingGrant(
            OfflineOperatingGrant.CurrentSchemaVersion,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Org",
            "d",
            "Cashier",
            Array.Empty<string>(),
            "Active",
            "A",
            "a",
            "a@example.com",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(1),
            OfflineGrantScopeKind.Organization,
            BranchId: Guid.NewGuid(),
            PosDeviceId: Guid.NewGuid());
        var personal = org with
        {
            OrganizationId = null,
            OrganizationDisplayName = PersonalLocalScope.DisplayName,
            ScopeKind = OfflineGrantScopeKind.Personal,
            RoleCode = null,
            BranchId = null,
            PosDeviceId = null
        };

        Assert.True(org.IsOrganizationScope);
        Assert.False(org.IsPersonalScope);
        Assert.True(personal.IsPersonalScope);
        Assert.False(personal.IsOrganizationScope);
    }

    [Fact]
    public void Default_duration_hours_is_720()
    {
        Assert.Equal(720, new OfflineOperatingGrantOptions().DurationHours);
        Assert.Equal(8760, new OfflineOperatingGrantOptions().MaxDurationHours);
    }

    private sealed class ThrowingPinStore : IOfflineOperatingGrantStore
    {
        private readonly MemoryOfflineGrantStore _inner = new();
        public bool ThrowOnSave { get; set; }

        public Task EnsureMigratedAsync(CancellationToken ct = default) => _inner.EnsureMigratedAsync(ct);

        public Task<IReadOnlyList<OfflineEnrolledUserSummary>> GetEnrolledUsersAsync(CancellationToken ct = default) =>
            _inner.GetEnrolledUsersAsync(ct);

        public Task<OfflineOperatingGrant?> LoadGrantAsync(Guid userId, CancellationToken ct = default) =>
            _inner.LoadGrantAsync(userId, ct);

        public Task SaveGrantAsync(OfflineOperatingGrant grant, CancellationToken ct = default) =>
            _inner.SaveGrantAsync(grant, ct);

        public Task ClearGrantAsync(Guid userId, CancellationToken ct = default) =>
            _inner.ClearGrantAsync(userId, ct);

        public Task<OfflinePinVerifier?> LoadPinVerifierAsync(Guid userId, CancellationToken ct = default) =>
            _inner.LoadPinVerifierAsync(userId, ct);

        public Task SavePinVerifierAsync(Guid userId, OfflinePinVerifier verifier, CancellationToken ct = default)
        {
            if (ThrowOnSave)
            {
                throw new InvalidOperationException("secure storage unavailable");
            }

            return _inner.SavePinVerifierAsync(userId, verifier, ct);
        }

        public Task ClearPinVerifierAsync(Guid userId, CancellationToken ct = default) =>
            _inner.ClearPinVerifierAsync(userId, ct);

        public Task RemoveUserAsync(Guid userId, CancellationToken ct = default) =>
            _inner.RemoveUserAsync(userId, ct);
    }

    private static async Task<Harness> SeedAsync(FakeClock clock, int durationHours = 720)
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

    private static readonly Guid TestBranchId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid TestPosDeviceId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static AuthSession OnlineSession() =>
        new(
            TestUserId,
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
            EnabledFeatureCodes: ["pos.sell"],
            AccountClass: "Organization",
            BranchId: TestBranchId,
            PosDeviceId: TestPosDeviceId);
    private static AuthSession PersonalSession() =>
        new(
            TestUserId,
            "Personal One",
            "personal1",
            "personal@example.com",
            OrganizationId: null,
            OrganizationDisplayName: null,
            DateTimeOffset.Parse("2026-08-08T00:00:00Z"),
            DateTimeOffset.Parse("2026-08-09T00:00:00Z"),
            HasPosAccess: false,
            AccessReasonCode: null,
            AccountClass: "Personal");

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
}
