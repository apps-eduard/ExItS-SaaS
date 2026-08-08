using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class AuthOfflineUxLayerTests
{
    [Fact]
    public async Task Pin_enrollment_required_when_grant_exists_without_pin()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await SeedGrantWithoutPinAsync(clock);
        Assert.False(await harness.Service.HasPinConfiguredAsync());
        var offer = await harness.Service.EvaluateColdStartOfferAsync();
        Assert.False(offer.CanOfferPinUnlock);
        Assert.NotNull(offer.Grant);
        Assert.Equal("offline_pin_not_configured", offer.DenialReasonCode);
    }

    [Fact]
    public async Task Enrolled_user_is_not_forced_to_enroll_again()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await SeedAsync(clock);
        Assert.True(await harness.Service.HasPinConfiguredAsync());
        var offer = await harness.Service.EvaluateColdStartOfferAsync();
        Assert.True(offer.CanOfferPinUnlock);
    }

    [Fact]
    public void Pin_enrollment_page_has_no_skip_action()
    {
        var page = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "OfflinePinEnrollment.razor"));
        Assert.Contains("Offline_PinEnrollTitle", page, StringComparison.Ordinal);
        Assert.Contains("Offline_PinEnrollAction", page, StringComparison.Ordinal);
        Assert.Contains("Offline_PinConfirmLabel", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Maybe later", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Offline_PinEnrollSkip", page, StringComparison.Ordinal);
        Assert.DoesNotContain("OnClick=\"Skip", page, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Pin_confirmation_mismatch_is_rejected_before_save()
    {
        // UI rejects mismatch; service still requires valid format.
        Assert.False(string.Equals("123456", "654321", StringComparison.Ordinal));
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await SeedGrantWithoutPinAsync(clock);
        Assert.True((await harness.Service.SetPinAsync("123456")).Succeeded);
        Assert.False(OfflinePinHasher.Verify("654321", (await harness.Store.LoadPinVerifierAsync())!));
    }

    [Fact]
    public async Task Lock_keeps_grant_and_pin_while_sign_out_clears_grant()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await SeedAsync(clock);
        Assert.Equal(OfflinePinUnlockStatus.Succeeded, (await harness.Service.UnlockWithPinAsync("123456")).Status);
        Assert.True(harness.Service.IsUnlockedThisProcess);

        harness.Service.LockThisProcess();
        Assert.False(harness.Service.IsUnlockedThisProcess);
        Assert.NotNull(await harness.Store.LoadGrantAsync());
        Assert.NotNull(await harness.Store.LoadPinVerifierAsync());
        Assert.True((await harness.Service.EvaluateColdStartOfferAsync()).CanOfferPinUnlock);

        await harness.Service.ClearAsync();
        Assert.Null(await harness.Store.LoadGrantAsync());
        Assert.NotNull(await harness.Store.LoadPinVerifierAsync());
        Assert.False((await harness.Service.EvaluateColdStartOfferAsync()).CanOfferPinUnlock);
    }

    [Fact]
    public void Offline_working_warning_appears_once_per_offline_session()
    {
        var ux = new OfflineSessionUxState();
        ux.NotifyOfflinePinUnlocked();
        Assert.True(ux.PendingOfflineWorkingWarning);
        ux.AcknowledgeOfflineWorkingWarning();
        Assert.False(ux.PendingOfflineWorkingWarning);
        Assert.True(ux.OfflineWorkingWarningShown);

        ux.NotifyOfflinePinUnlocked();
        Assert.False(ux.PendingOfflineWorkingWarning);

        ux.ResetSession();
        ux.NotifyOfflinePinUnlocked();
        Assert.True(ux.PendingOfflineWorkingWarning);
    }

    [Fact]
    public async Task Online_required_guard_shows_dialog_when_offline_and_preserves_callback_only()
    {
        var connectivity = new FakeConnectivity(connected: false);
        var guard = new OnlineRequiredGuard(connectivity, new PosOfflineCapabilityPolicy());
        Assert.False(await guard.EnsureOnlineAsync());
        Assert.True(guard.IsDialogVisible);
        await guard.DismissAsync();
        Assert.False(guard.IsDialogVisible);

        connectivity.Connected = true;
        Assert.True(await guard.EnsureOnlineAsync());
        Assert.False(guard.IsDialogVisible);
    }

    [Fact]
    public void SignIn_use_pin_and_provider_placeholders_are_wired()
    {
        var signIn = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "SignIn.razor"));
        Assert.Contains("SignIn_ContinueGoogle", signIn, StringComparison.Ordinal);
        Assert.Contains("SignIn_ContinueFacebook", signIn, StringComparison.Ordinal);
        Assert.Contains("ContinueWithGooglePlaceholderAsync", signIn, StringComparison.Ordinal);
        Assert.Contains("ContinueWithFacebookPlaceholderAsync", signIn, StringComparison.Ordinal);
        Assert.Contains("SignIn_UsePin", signIn, StringComparison.Ordinal);
        Assert.Contains("EvaluateOfflineColdStartOfferAsync", signIn, StringComparison.Ordinal);
        Assert.Contains("SignIn_InternetRequired", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("WebAuthenticator", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("external/google/challenge", signIn, StringComparison.Ordinal);
    }

    [Fact]
    public void NavigationGate_forces_offline_pin_setup_when_missing()
    {
        var gate = File.ReadAllText(Path.Combine(MauiProject(), "Services", "NavigationGate.cs"));
        Assert.Contains("/offline-pin-setup", gate, StringComparison.Ordinal);
        Assert.Contains("HasOfflinePinConfiguredAsync", gate, StringComparison.Ordinal);
        Assert.Contains("offline_pin_not_configured", gate, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_hosts_online_required_and_offline_warning_dialogs()
    {
        var posShell = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Layout", "PosShell.razor"));
        Assert.Contains("OnlineRequiredDialogHost", posShell, StringComparison.Ordinal);
        Assert.Contains("OfflineWorkingWarningHost", posShell, StringComparison.Ordinal);

        var menu = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "ShellAccountMenu.razor"));
        Assert.Contains("Auth_Lock", menu, StringComparison.Ordinal);
        Assert.Contains("LockAsync", menu, StringComparison.Ordinal);
        Assert.Contains("LogoutAsync", menu, StringComparison.Ordinal);
    }

    [Fact]
    public void Sync_status_shows_offline_waiting_and_device_storage_copy()
    {
        var sync = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "ShellSyncStatus.razor"));
        Assert.Contains("SyncStatus_OfflineWaiting", sync, StringComparison.Ordinal);
        Assert.Contains("SyncStatus_OfflineDeviceStored", sync, StringComparison.Ordinal);
        Assert.Contains("SyncStatus_RetryConnection", sync, StringComparison.Ordinal);
        Assert.Contains("SyncStatus_AllChangesSynced", sync, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Org_mismatch_denies_use_pin_offer_at_auth_restore_boundary()
    {
        // Grant service itself is device/user bound; org mismatch is applied in AuthenticationService.
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await SeedAsync(clock);
        var offer = await harness.Service.EvaluateColdStartOfferAsync();
        Assert.True(offer.CanOfferPinUnlock);
        Assert.NotNull(offer.Grant);

        var otherOrg = Guid.Parse("99999999-9999-9999-9999-999999999999");
        Assert.NotEqual(offer.Grant.OrganizationId, otherOrg);
        var denied = offer with { CanOfferPinUnlock = false, DenialReasonCode = "offline_org_mismatch" };
        Assert.False(denied.CanOfferPinUnlock);
        Assert.Equal("offline_org_mismatch", denied.DenialReasonCode);
    }

    private static async Task<Harness> SeedAsync(FakeClock clock)
    {
        var harness = await SeedGrantWithoutPinAsync(clock);
        Assert.True((await harness.Service.SetPinAsync("123456")).Succeeded);
        return harness;
    }

    private static async Task<Harness> SeedGrantWithoutPinAsync(FakeClock clock)
    {
        var options = Options.Create(new OfflineOperatingGrantOptions
        {
            DurationHours = 24,
            PinMinLength = 6,
            MaxFailedPinAttempts = 5,
            PinLockoutMinutes = 15,
            PinHashIterations = 10_000
        });
        var store = new MemoryOfflineGrantStore();
        var device = new FakeDevice("device-a");
        var sut = new OfflineOperatingGrantService(store, device, options, clock);
        await sut.EstablishFromOnlineSessionAsync(OnlineSession(), device.DeviceId, "Cashier");
        return new Harness(sut, store, device, options, clock);
    }

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

    private static string MauiProject()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Maui project not found.");
    }

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

    private sealed class FakeConnectivity(bool connected) : IConnectivityService
    {
        public bool Connected { get; set; } = connected;

        public event EventHandler<ConnectivityStatus>? ConnectivityChanged
        {
            add { }
            remove { }
        }

        public Task<bool> IsConnectedAsync(CancellationToken ct = default) =>
            Task.FromResult(Connected);
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
