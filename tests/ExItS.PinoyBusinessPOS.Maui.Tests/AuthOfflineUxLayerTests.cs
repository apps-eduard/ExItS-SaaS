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
        Assert.Equal("offline_pin_not_configured", offer.DenialReasonCode);
        // Denial does not surface a grant for unlock; enrollment is gated separately.
        Assert.Null(offer.Grant);
    }

    [Fact]
    public async Task Online_establish_binds_legacy_unbound_pin_to_current_user()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await SeedGrantWithoutPinAsync(clock);
        var userId = OnlineSession().UserId;
        // Simulate a leftover device PIN written before UserId binding (or lost UserId on disk).
        await harness.Store.SavePinVerifierAsync(userId, OfflinePinHasher.Create("123456", 10_000, userId: null));
        Assert.True(await harness.Service.HasPinConfiguredAsync(userId));

        await harness.Service.EstablishFromOnlineSessionAsync(OnlineSession(), harness.Device.DeviceId, "Cashier");

        Assert.True(await harness.Service.HasPinConfiguredAsync(userId));
        var bound = await harness.Store.LoadPinVerifierAsync(userId);
        Assert.NotNull(bound);
        Assert.Equal(userId, bound!.UserId);
        Assert.NotNull(await harness.Store.LoadGrantAsync(userId));
    }

    [Fact]
    public async Task Personal_relogin_establish_keeps_same_user_pin()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var options = Options.Create(new OfflineOperatingGrantOptions
        {
            DurationHours = 720,
            PinMinLength = 6,
            MaxFailedPinAttempts = 5,
            PinLockoutMinutes = 15,
            PinHashIterations = 10_000
        });
        var store = new MemoryOfflineGrantStore();
        var device = new FakeDevice("device-personal");
        var sut = new OfflineOperatingGrantService(store, device, options, clock);
        var personal = new AuthSession(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            "Rosa Personal",
            "rosa",
            "rosa@example.com",
            OrganizationId: null,
            OrganizationDisplayName: null,
            IssuedAtUtc: clock.GetUtcNow(),
            ExpiresAtUtc: clock.GetUtcNow().AddHours(8),
            HasPosAccess: false,
            AccessReasonCode: null,
            AccountClass: "Personal");

        await sut.EstablishFromOnlineSessionAsync(personal, device.DeviceId, roleCode: null);
        Assert.True((await sut.SetPinAsync("123456")).Succeeded);
        var before = await store.LoadPinVerifierAsync(personal.UserId);

        // Simulate app kill + online Personal login again (new process unlock flag).
        sut.LockThisProcess();
        await sut.EstablishFromOnlineSessionAsync(personal, device.DeviceId, roleCode: null);

        Assert.True(await sut.HasPinConfiguredAsync(personal.UserId));
        var after = await store.LoadPinVerifierAsync(personal.UserId);
        Assert.NotNull(after);
        Assert.Equal(personal.UserId, after!.UserId);
        Assert.Equal(before!.HashBase64, after.HashBase64);
    }

    [Fact]
    public void Pin_verifier_json_roundtrip_preserves_user_id()
    {
        var original = OfflinePinHasher.Create(
            "123456",
            10_000,
            Guid.Parse("11111111-1111-1111-1111-111111111111"));
        var json = System.Text.Json.JsonSerializer.Serialize(
            original,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });
        var restored = System.Text.Json.JsonSerializer.Deserialize<OfflinePinVerifier>(
            json,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });
        Assert.NotNull(restored);
        Assert.Equal(original.UserId, restored!.UserId);
        Assert.Equal(original.HashBase64, restored.HashBase64);
    }

    [Fact]
    public async Task Store_loads_pascal_case_pin_json_without_dropping_user_id()
    {
        // Older builds may have written PascalCase SecureStorage JSON; UserId must still bind.
        var tokens = new MemorySecureTokenStoreForPin();
        var store = new OfflineOperatingGrantStore(tokens);
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var pascal = """
            {"Algorithm":"PBKDF2-SHA256","Iterations":10000,"SaltBase64":"c2FsdA==","HashBase64":"aGFzaA==","FailedAttempts":0,"LockedUntilUtc":null,"UserId":"11111111-1111-1111-1111-111111111111"}
            """;
        await tokens.SetAsync(SecureTokenKeys.OfflinePinVerifier, pascal);

        var loaded = await store.LoadPinVerifierAsync(userId);
        Assert.NotNull(loaded);
        Assert.Equal(userId, loaded!.UserId);
    }

    [Fact]
    public async Task Online_establish_binds_empty_userid_pin_instead_of_clearing()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await SeedGrantWithoutPinAsync(clock);
        var userId = OnlineSession().UserId;
        var unbound = OfflinePinHasher.Create("123456", 10_000, userId: Guid.Empty);
        await harness.Store.SavePinVerifierAsync(userId, unbound);

        await harness.Service.EstablishFromOnlineSessionAsync(OnlineSession(), harness.Device.DeviceId, "Cashier");

        Assert.True(await harness.Service.HasPinConfiguredAsync(userId));
        var bound = await harness.Store.LoadPinVerifierAsync(userId);
        Assert.Equal(userId, bound!.UserId);
    }

    [Fact]
    public async Task Online_establish_keeps_pin_owned_by_different_user()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await SeedGrantWithoutPinAsync(clock);
        var otherUser = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var otherPin = OfflinePinHasher.Create("123456", 10_000, otherUser);
        await harness.Store.SaveGrantAsync(new OfflineOperatingGrant(
            OfflineOperatingGrant.CurrentSchemaVersion,
            otherUser,
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            "Test Store",
            harness.Device.DeviceId,
            "Cashier",
            ["pos.sell"],
            "Active",
            "Other Cashier",
            "other",
            "other@example.com",
            clock.GetUtcNow(),
            clock.GetUtcNow(),
            clock.GetUtcNow().AddHours(720),
            OfflineGrantScopeKind.Organization,
            BranchId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            PosDeviceId: Guid.Parse("44444444-4444-4444-4444-444444444444")));
        await harness.Store.SavePinVerifierAsync(otherUser, otherPin);

        await harness.Service.EstablishFromOnlineSessionAsync(OnlineSession(), harness.Device.DeviceId, "Cashier");

        var kept = await harness.Store.LoadPinVerifierAsync(otherUser);
        Assert.NotNull(kept);
        Assert.Equal(otherPin.HashBase64, kept!.HashBase64);
        Assert.False(await harness.Service.HasPinConfiguredAsync(OnlineSession().UserId));
    }

    [Fact]
    public async Task Online_establish_keeps_pin_for_same_user()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await SeedAsync(clock);
        var userId = OnlineSession().UserId;
        Assert.True(await harness.Service.HasPinConfiguredAsync(userId));
        var before = await harness.Store.LoadPinVerifierAsync(userId);

        await harness.Service.EstablishFromOnlineSessionAsync(OnlineSession(), harness.Device.DeviceId, "Cashier");

        Assert.True(await harness.Service.HasPinConfiguredAsync(userId));
        var after = await harness.Store.LoadPinVerifierAsync(userId);
        Assert.NotNull(after);
        Assert.Equal(userId, after!.UserId);
        Assert.Equal(before!.HashBase64, after.HashBase64);
    }

    [Fact]
    public void Pin_enrollment_page_stays_when_pin_missing()
    {
        var page = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "OfflinePinEnrollment.razor"));
        Assert.Contains("never auto-dismiss", page, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("_isChange = false", page, StringComparison.Ordinal);
        Assert.DoesNotContain("already done", page, StringComparison.OrdinalIgnoreCase);
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
        Assert.Contains("Offline_PinChangeTitle", page, StringComparison.Ordinal);
        Assert.Contains("Offline_PinChangeAction", page, StringComparison.Ordinal);
        Assert.Contains("Offline_PinConfirmLabel", page, StringComparison.Ordinal);
        Assert.Contains("mode", page, StringComparison.Ordinal);
        Assert.Contains("AuthExperience", page, StringComparison.Ordinal);
        Assert.Contains("@page \"/setup-pin\"", page, StringComparison.Ordinal);
        Assert.Contains("EvaluateCurrentUserOfflinePinReadinessAsync", page, StringComparison.Ordinal);
        Assert.Contains("Offline_PinSetupIncomplete", page, StringComparison.Ordinal);
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
        var userId = OnlineSession().UserId;
        Assert.False(OfflinePinHasher.Verify("654321", (await harness.Store.LoadPinVerifierAsync(userId))!));
    }

    [Fact]
    public async Task Lock_keeps_grant_and_pin_hard_clear_drops_grant_only()
    {
        var clock = new FakeClock(DateTimeOffset.Parse("2026-08-08T00:00:00Z"));
        var harness = await SeedAsync(clock);
        var userId = OnlineSession().UserId;
        Assert.Equal(OfflinePinUnlockStatus.Succeeded, (await harness.Service.UnlockWithPinAsync(userId, "123456")).Status);
        Assert.True(harness.Service.IsUnlockedThisProcess);

        // Lock / Sign out path: process unlock drops, durable grant + PIN remain.
        harness.Service.LockThisProcess();
        Assert.False(harness.Service.IsUnlockedThisProcess);
        Assert.NotNull(await harness.Store.LoadGrantAsync(userId));
        Assert.NotNull(await harness.Store.LoadPinVerifierAsync(userId));
        Assert.True((await harness.Service.EvaluateColdStartOfferAsync()).CanOfferPinUnlock);

        // Hard clear (server denial / remove-from-device): grant gone, PIN verifier retained.
        await harness.Service.ClearUserGrantAsync(userId);
        Assert.Null(await harness.Store.LoadGrantAsync(userId));
        Assert.NotNull(await harness.Store.LoadPinVerifierAsync(userId));
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
    public void Offline_pin_unlock_ui_is_stacked_and_supports_account_selection()
    {
        var unlock = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "OfflinePinUnlock.razor"));
        var css = File.ReadAllText(Path.Combine(MauiProject(), "wwwroot", "app.css"));

        Assert.Contains("pos-offline-pin__badge", unlock, StringComparison.Ordinal);
        Assert.Contains("pos-offline-pin__actions", unlock, StringComparison.Ordinal);
        Assert.Contains("pos-offline-pin__signout", unlock, StringComparison.Ordinal);
        Assert.Contains("Offline_PinShow", unlock, StringComparison.Ordinal);
        Assert.Contains("NotifyApiReachability(false)", unlock, StringComparison.Ordinal);
        Assert.Contains("NotifyApiReachability(true)", unlock, StringComparison.Ordinal);
        Assert.Contains("PinSignInServerOutcome.ValidatedOnline", unlock, StringComparison.Ordinal);
        Assert.Contains("SignIn_WithPin", unlock, StringComparison.Ordinal);
        Assert.Contains("SignIn_SigningYouIn", unlock, StringComparison.Ordinal);
        Assert.Contains("pos-offline-pin__accounts", unlock, StringComparison.Ordinal);
        Assert.Contains("pos-offline-pin__account-meta", unlock, StringComparison.Ordinal);
        Assert.Contains("user.OrganizationDisplayName", unlock, StringComparison.Ordinal);
        Assert.Contains("CanOfferPinUnlock", unlock, StringComparison.Ordinal);
        Assert.Contains("Nav.NavigateTo(\"/signin\"", unlock, StringComparison.Ordinal);
        Assert.Contains("Offline_PinForgotAction", unlock, StringComparison.Ordinal);
        Assert.Contains("Offline_PinForgotOfflineMessage", unlock, StringComparison.Ordinal);
        Assert.DoesNotContain("GetEnrolledOfflineUsersAsync", unlock, StringComparison.Ordinal);
        Assert.DoesNotContain("InlineMessageTone.Warning", unlock, StringComparison.Ordinal);

        Assert.Contains(".pos-offline-pin__actions", css, StringComparison.Ordinal);
        Assert.Contains("flex-direction: column", css, StringComparison.Ordinal);
    }

    [Fact]
    public void SignIn_maps_credential_and_offline_failures_to_distinct_copy()
    {
        var signIn = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "SignIn.razor"));
        var en = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.fil-PH.resx"));

        Assert.Contains("SignIn_CredentialsRequired", signIn, StringComparison.Ordinal);
        Assert.Contains("Auth_InvalidCredentials", signIn, StringComparison.Ordinal);
        Assert.Contains("AuthFailureReason.Offline", signIn, StringComparison.Ordinal);
        Assert.Contains("SignIn_ServerUnreachablePinHint", signIn, StringComparison.Ordinal);

        Assert.Contains("Incorrect username or password", en, StringComparison.Ordinal);
        Assert.DoesNotContain("The Platform user could not be signed in.", en, StringComparison.Ordinal);
        Assert.Contains("name=\"SignIn_CredentialsRequired\"", en, StringComparison.Ordinal);
        Assert.Contains("name=\"SignIn_CredentialsRequired\"", fil, StringComparison.Ordinal);
        Assert.Contains("Mali ang username o password", fil, StringComparison.Ordinal);
    }

    [Fact]
    public void SignIn_use_pin_and_provider_placeholders_are_wired()
    {
        var signIn = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "SignIn.razor"));
        Assert.Contains("SignIn_ContinueGoogle", signIn, StringComparison.Ordinal);
        Assert.Contains("SignIn_ContinueFacebook", signIn, StringComparison.Ordinal);
        Assert.Contains("ContinueWithGooglePlaceholderAsync", signIn, StringComparison.Ordinal);
        Assert.Contains("ContinueWithFacebookPlaceholderAsync", signIn, StringComparison.Ordinal);
        Assert.Contains("SignIn_UsePinInstead", signIn, StringComparison.Ordinal);
        Assert.Contains("pos-auth-page__social-btn--pin", signIn, StringComparison.Ordinal);
        Assert.Contains("SignIn_WithPin", signIn, StringComparison.Ordinal);
        Assert.Contains("SignIn_PinKeypadHint", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowOfflinePinAction", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("_canUsePin && (_isOffline || _offerPinBecauseUnreachable)", signIn, StringComparison.Ordinal);
        Assert.Contains("SignIn_OfflineNoPinMessage", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("SignIn_OfflineLimitedHint", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("SignIn_ContinueOffline", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("pos-signin__offline-panel", signIn, StringComparison.Ordinal);
        Assert.Contains("RefreshOfflineStateAsync", signIn, StringComparison.Ordinal);
        Assert.Contains("EvaluateOfflineColdStartOfferAsync", signIn, StringComparison.Ordinal);
        Assert.Contains("SignIn_InternetRequired", signIn, StringComparison.Ordinal);
        Assert.Contains("IsOfflinePinSetupRoute", signIn, StringComparison.Ordinal);
        Assert.Contains("AppendReturnRoute", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("WebAuthenticator", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("external/google/challenge", signIn, StringComparison.Ordinal);
    }

    [Fact]
    public void NavigationGate_forces_offline_pin_setup_when_missing()
    {
        var gate = File.ReadAllText(Path.Combine(MauiProject(), "Services", "NavigationGate.cs"));
        Assert.Contains("/offline-pin-setup", gate, StringComparison.Ordinal);
        Assert.Contains("EvaluateCurrentUserOfflinePinReadinessAsync", gate, StringComparison.Ordinal);
        Assert.Contains("RequiresPinEnrollment", gate, StringComparison.Ordinal);
        Assert.Contains("RequiresOfflinePinSetupAsync", gate, StringComparison.Ordinal);
        // Personal path must enroll PIN before PersonalHome (not only Organization POS).
        var personalBranch = gate.IndexOf("OrganizationId is null", StringComparison.Ordinal);
        var personalPin = gate.IndexOf("RequiresOfflinePinSetupAsync", personalBranch, StringComparison.Ordinal);
        var personalHome = gate.IndexOf("RoleHomeResolver.PersonalHome", personalBranch, StringComparison.Ordinal);
        Assert.InRange(personalPin, personalBranch, personalHome);

        // Org POS: device registration before PIN, optional template, then sell-critical setup.
        var deviceRoute = gate.IndexOf("/devices/register", personalHome, StringComparison.Ordinal);
        var orgPosPin = gate.IndexOf("RequiresOfflinePinSetupAsync", deviceRoute, StringComparison.Ordinal);
        var templateRoute = gate.IndexOf("/catalog/import?onboarding=1", orgPosPin, StringComparison.Ordinal);
        var setupRoute = gate.IndexOf("return \"/setup\"", templateRoute, StringComparison.Ordinal);
        Assert.InRange(deviceRoute, personalHome, orgPosPin);
        Assert.InRange(orgPosPin, deviceRoute, templateRoute);
        Assert.InRange(templateRoute, orgPosPin, setupRoute);
        Assert.Contains("GetBusinessTemplatePromptPendingAsync", gate, StringComparison.Ordinal);
        Assert.Contains("EvaluateCurrentUserOfflinePinReadinessAsync", gate, StringComparison.Ordinal);
        // Template prompt must not require ManageCatalog (trial feature hydration lag).
        var templateBlock = gate.Substring(templateRoute - 200, Math.Min(400, gate.Length - (templateRoute - 200)));
        Assert.DoesNotContain("ManageCatalog", templateBlock, StringComparison.Ordinal);

        Assert.Contains("IsOrgPosFirstTimeSetupIncompleteAsync", gate, StringComparison.Ordinal);
        Assert.Contains("IsOrgFirstTimeSetupRoute", gate, StringComparison.Ordinal);
    }

    [Fact]
    public void First_time_setup_pages_use_AuthShell_and_PosShell_hides_bottom_nav()
    {
        var maui = MauiProject();
        var posShell = File.ReadAllText(Path.Combine(maui, "Components", "Layout", "PosShell.razor"));
        Assert.Contains("IsOrgPosFirstTimeSetupIncompleteAsync", posShell, StringComparison.Ordinal);
        Assert.Contains("_showBottomNav", posShell, StringComparison.Ordinal);
        Assert.Contains("pos-shell--auth", posShell, StringComparison.Ordinal);
        Assert.Contains("IsOrgFirstTimeSetupRoute", posShell, StringComparison.Ordinal);

        Assert.Contains("@layout Layout.AuthShell",
            File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Devices", "PosDeviceRegister.razor")),
            StringComparison.Ordinal);
        Assert.Contains("@layout Layout.AuthShell",
            File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Catalog", "CatalogImport.razor")),
            StringComparison.Ordinal);
        Assert.Contains("@layout Layout.AuthShell",
            File.ReadAllText(Path.Combine(maui, "Components", "Pages", "OperationalSetup", "OperationalSetupPage.razor")),
            StringComparison.Ordinal);
        Assert.Contains("@layout Layout.AuthShell",
            File.ReadAllText(Path.Combine(maui, "Components", "Pages", "OfflinePinEnrollment.razor")),
            StringComparison.Ordinal);

        var gate = File.ReadAllText(Path.Combine(maui, "Services", "NavigationGate.cs"));
        Assert.Contains("\"/devices/register\"", gate, StringComparison.Ordinal);
        Assert.Contains("\"/offline-pin-setup\"", gate, StringComparison.Ordinal);
        Assert.Contains("\"/catalog/import\"", gate, StringComparison.Ordinal);
        Assert.Contains("\"/setup\"", gate, StringComparison.Ordinal);
        Assert.Contains("IsOrgFirstTimeSetupRoute", gate, StringComparison.Ordinal);
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
        // Lock is POS-operate only — Org essentials (no POS) must not offer Lock.
        Assert.Contains("CurrentUser.HasPosAccess", menu, StringComparison.Ordinal);
        var showLock = menu.IndexOf("private bool ShowLock", StringComparison.Ordinal);
        var hasPos = menu.IndexOf("CurrentUser.HasPosAccess", showLock, StringComparison.Ordinal);
        Assert.InRange(hasPos, showLock, showLock + 400);

        var settings = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Settings.razor"));
        // Org essentials members must keep Settings (appearance) without bouncing to /org.
        Assert.Contains("OrganizationId is not null", settings, StringComparison.Ordinal);
        var gate = settings.IndexOf("if (!Gate.CanEnterProtectedShell)", StringComparison.Ordinal);
        var orgStay = settings.IndexOf("OrganizationId is not null", gate, StringComparison.Ordinal);
        var bounce = settings.IndexOf("ResolveStartRouteAsync", gate, StringComparison.Ordinal);
        Assert.InRange(orgStay, gate, bounce);
    }

    [Fact]
    public void Sync_status_shows_offline_waiting_and_device_storage_copy()
    {
        var sync = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "ShellSyncStatus.razor"));
        Assert.Contains("SyncStatus_OfflineWaiting", sync, StringComparison.Ordinal);
        Assert.Contains("SyncStatus_OfflineDeviceStored", sync, StringComparison.Ordinal);
        Assert.Contains("SyncStatus_RetryConnection", sync, StringComparison.Ordinal);
        Assert.Contains("SyncStatus_AllChangesSynced", sync, StringComparison.Ordinal);
        Assert.Contains("IOfflineReconnectAutoSync", sync, StringComparison.Ordinal);
        Assert.Contains("RetryIncludingFailedAsync", sync, StringComparison.Ordinal);
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
            DurationHours = 720,
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
            EnabledFeatureCodes: ["pos.sell"],
            BranchId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            PosDeviceId: Guid.Parse("44444444-4444-4444-4444-444444444444"));

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
