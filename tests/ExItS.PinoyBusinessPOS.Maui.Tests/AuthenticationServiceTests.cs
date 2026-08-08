using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Platform;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class AuthenticationServiceTests
{
    [Fact]
    public async Task SignIn_is_blocked_outside_development_testing()
    {
        var sut = CreateSut(environment: "Production", access: new FakeAccessClient());
        var result = await sut.SignInAsync(new SignInRequest(null, null, Guid.NewGuid()));
        Assert.False(result.Succeeded);
        Assert.Equal(AuthFailureReason.ProductionAuthUnavailable, result.FailureReason);
    }

    [Fact]
    public async Task SignIn_rejects_unknown_user()
    {
        var access = new FakeAccessClient { UserResult = ApiResult<PlatformUserDto>.NotFound() };
        var sut = CreateSut("Development", access);
        var result = await sut.SignInAsync(new SignInRequest(null, null, Guid.NewGuid()));
        Assert.False(result.Succeeded);
        Assert.Equal(AuthFailureReason.InvalidCredentials, result.FailureReason);
    }

    [Fact]
    public async Task SignIn_rejects_inactive_user()
    {
        var userId = Guid.NewGuid();
        var access = new FakeAccessClient
        {
            UserResult = ApiResult<PlatformUserDto>.Success(User(userId, "Suspended"))
        };
        var sut = CreateSut("Development", access);
        var result = await sut.SignInAsync(new SignInRequest(null, null, userId));
        Assert.False(result.Succeeded);
        Assert.Equal(AuthFailureReason.UserInactive, result.FailureReason);
    }

    [Fact]
    public async Task SignIn_stores_session_in_secure_store_only()
    {
        var userId = Guid.NewGuid();
        var tokens = new MemorySecureTokenStore();
        var prefs = new MemoryOnboardingStore();
        var access = new FakeAccessClient
        {
            UserResult = ApiResult<PlatformUserDto>.Success(User(userId, "Active"))
        };
        var sut = CreateSut("Development", access, tokens, prefs);
        var result = await sut.SignInAsync(new SignInRequest(null, null, userId));
        Assert.True(result.Succeeded);
        Assert.Equal(userId.ToString("D"), await tokens.GetAsync(SecureTokenKeys.UserId));
        Assert.False(string.IsNullOrWhiteSpace(await tokens.GetAsync(SecureTokenKeys.SessionMarker)));
        Assert.Null(prefs.PeekSecretLeak());
    }

    [Fact]
    public async Task Logout_clears_secure_session()
    {
        var userId = Guid.NewGuid();
        var tokens = new MemorySecureTokenStore();
        var access = new FakeAccessClient
        {
            UserResult = ApiResult<PlatformUserDto>.Success(User(userId, "Active"))
        };
        var current = new CurrentUserContext();
        var sut = CreateSut("Development", access, tokens, currentUser: current);
        await sut.SignInAsync(new SignInRequest(null, null, userId));
        await sut.LogoutAsync();
        Assert.Null(await tokens.GetAsync(SecureTokenKeys.UserId));
        Assert.Null(await tokens.GetAsync(SecureTokenKeys.SessionMarker));
        Assert.False(current.IsAuthenticated);
    }

    [Fact]
    public async Task Restore_fails_closed_when_session_expired()
    {
        var userId = Guid.NewGuid();
        var tokens = new MemorySecureTokenStore();
        var clock = new FakeTimeProvider(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
        var access = new FakeAccessClient
        {
            UserResult = ApiResult<PlatformUserDto>.Success(User(userId, "Active"))
        };
        var sut = CreateSut("Development", access, tokens, time: clock);
        await sut.SignInAsync(new SignInRequest(null, null, userId));
        clock.SetUtcNow(DateTimeOffset.Parse("2026-01-02T00:00:00Z"));
        var restore = await sut.RestoreSessionAsync();
        Assert.False(restore.Succeeded);
        Assert.Equal(AuthFailureReason.SessionExpired, restore.FailureReason);
        Assert.Null(await tokens.GetAsync(SecureTokenKeys.UserId));
    }

    [Fact]
    public async Task SelectOrganization_with_token_bind_loads_commercial_grants()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var tokens = new MemorySecureTokenStore();
        var prefs = new MemoryOnboardingStore();
        var current = new CurrentUserContext();
        var access = new FakeAccessClient
        {
            LoginResult = ApiResult<PlatformLoginResultDto>.Success(new PlatformLoginResultDto(
                "platform-session",
                Guid.NewGuid(),
                userId,
                "owner",
                "Owner",
                "o@example.com",
                DateTimeOffset.UtcNow.AddHours(8),
                DateTimeOffset.UtcNow.AddHours(24),
                null,
                null,
                "none",
                1,
                AccountProfileId: Guid.NewGuid(),
                AccountClass: "Organization",
                AllowedScope: "Organization")),
            IssueTokenResult = ApiResult<PlatformAccessTokenIssueDto>.Success(new PlatformAccessTokenIssueDto(
                "opaque-token",
                "Bearer",
                Guid.NewGuid(),
                userId,
                "owner",
                "Owner",
                "o@example.com",
                DateTimeOffset.UtcNow.AddHours(8),
                null,
                null,
                null,
                "none",
                1,
                null,
                null)),
            BindTokenResult = ApiResult<PlatformAccessTokenIssueDto>.Success(new PlatformAccessTokenIssueDto(
                "bound-token",
                "Bearer",
                Guid.NewGuid(),
                userId,
                "owner",
                "Owner",
                "o@example.com",
                DateTimeOffset.UtcNow.AddHours(8),
                orgId,
                "Store One",
                PosProductCodes.PinoyBusinessPos,
                "bound",
                1,
                true,
                "allowed")),
            IntrospectResult = ApiResult<PlatformAccessTokenIntrospectionDto>.Success(
                new PlatformAccessTokenIntrospectionDto(
                    Active: true,
                    TokenId: Guid.NewGuid(),
                    UserId: userId,
                    Username: "owner",
                    DisplayName: "Owner",
                    OrganizationId: orgId,
                    OrganizationDisplayName: "Store One",
                    ProductCode: PosProductCodes.PinoyBusinessPos,
                    ExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(8),
                    ProductAccessAllowed: true,
                    ProductAccessReasonCode: "allowed",
                    SubscriptionStatus: "Active",
                    EnabledFeatureCodes: ["store-catalog-manage", "customer-credit-view"]))
        };
        var sut = CreateSut("Development", access, tokens, prefs, current);
        var signIn = await sut.SignInAsync(new SignInRequest("owner", "password"));
        Assert.True(signIn.Succeeded);

        var select = await sut.SelectOrganizationAsync(orgId);
        Assert.True(select.Succeeded);
        Assert.True(select.Session!.HasPosAccess);
        Assert.Equal("Active", select.Session.SubscriptionStatus);
        Assert.Contains("store-catalog-manage", select.Session.EnabledFeatureCodes!);
        Assert.Equal("Active", await tokens.GetAsync(SecureTokenKeys.SubscriptionStatus));
    }

    [Fact]
    public async Task SelectOrganization_with_token_bind_fills_dev_grants_when_features_missing()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var tokens = new MemorySecureTokenStore();
        var prefs = new MemoryOnboardingStore();
        var current = new CurrentUserContext();
        var access = new FakeAccessClient
        {
            LoginResult = ApiResult<PlatformLoginResultDto>.Success(new PlatformLoginResultDto(
                "platform-session",
                Guid.NewGuid(),
                userId,
                "owner",
                "Owner",
                "o@example.com",
                DateTimeOffset.UtcNow.AddHours(8),
                DateTimeOffset.UtcNow.AddHours(24),
                null,
                null,
                "none",
                1,
                AccountProfileId: Guid.NewGuid(),
                AccountClass: "Personal",
                AllowedScope: "personal")),
            IssueTokenResult = ApiResult<PlatformAccessTokenIssueDto>.Success(new PlatformAccessTokenIssueDto(
                "opaque-token",
                "Bearer",
                Guid.NewGuid(),
                userId,
                "owner",
                "Owner",
                "o@example.com",
                DateTimeOffset.UtcNow.AddHours(8),
                null,
                null,
                null,
                "none",
                1,
                null,
                null)),
            BindTokenResult = ApiResult<PlatformAccessTokenIssueDto>.Success(new PlatformAccessTokenIssueDto(
                "bound-token",
                "Bearer",
                Guid.NewGuid(),
                userId,
                "owner",
                "Owner",
                "o@example.com",
                DateTimeOffset.UtcNow.AddHours(8),
                orgId,
                "Store One",
                PosProductCodes.PinoyBusinessPos,
                "bound",
                1,
                true,
                "allowed")),
            // Active status but empty feature list previously short-circuited and denied ManageCatalog.
            IntrospectResult = ApiResult<PlatformAccessTokenIntrospectionDto>.Success(
                new PlatformAccessTokenIntrospectionDto(
                    Active: true,
                    TokenId: Guid.NewGuid(),
                    UserId: userId,
                    Username: "owner",
                    DisplayName: "Owner",
                    OrganizationId: orgId,
                    OrganizationDisplayName: "Store One",
                    ProductCode: PosProductCodes.PinoyBusinessPos,
                    ExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(8),
                    ProductAccessAllowed: true,
                    ProductAccessReasonCode: "allowed",
                    SubscriptionStatus: "Active",
                    EnabledFeatureCodes: [])),
            EvaluateResult = ApiResult<EffectiveAccessDto>.Success(new EffectiveAccessDto(
                true, "allowed", userId, orgId, PosProductCodes.PinoyBusinessPos,
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow,
                SubscriptionStatus: "Active",
                EnabledFeatureCodes: null))
        };
        var sut = CreateSut("Development", access, tokens, prefs, current);
        Assert.True((await sut.SignInAsync(new SignInRequest("owner", "password"))).Succeeded);

        var select = await sut.SelectOrganizationAsync(orgId);
        Assert.True(select.Succeeded);
        Assert.True(select.Session!.HasPosAccess);
        Assert.Equal("Active", select.Session.SubscriptionStatus);
        Assert.Contains("store-catalog-manage", select.Session.EnabledFeatureCodes!);
    }

    [Fact]
    public async Task SelectOrganization_with_token_falls_back_to_org_essentials_when_bind_forbidden()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var tokens = new MemorySecureTokenStore();
        var prefs = new MemoryOnboardingStore();
        var current = new CurrentUserContext();
        var access = new FakeAccessClient
        {
            LoginResult = ApiResult<PlatformLoginResultDto>.Success(new PlatformLoginResultDto(
                "platform-session",
                Guid.NewGuid(),
                userId,
                "owner",
                "Owner",
                "o@example.com",
                DateTimeOffset.UtcNow.AddHours(8),
                DateTimeOffset.UtcNow.AddHours(24),
                null,
                null,
                "none",
                1,
                AccountProfileId: Guid.NewGuid(),
                AccountClass: "Organization",
                AllowedScope: "Organization")),
            IssueTokenResult = ApiResult<PlatformAccessTokenIssueDto>.Success(new PlatformAccessTokenIssueDto(
                "opaque-token",
                "Bearer",
                Guid.NewGuid(),
                userId,
                "owner",
                "Owner",
                "o@example.com",
                DateTimeOffset.UtcNow.AddHours(8),
                null,
                null,
                null,
                "none",
                1,
                null,
                null)),
            BindTokenResult = ApiResult<PlatformAccessTokenIssueDto>.Forbidden(new ApiError(
                "Forbidden",
                "Product-local role is required to operate this product.",
                "application.auth.product_entry_denied",
                null,
                403)),
            OrganizationResult = ApiResult<PlatformOrganizationDto>.Success(Org(orgId)),
            AuthEligibleOrganizationsResult = ApiResult<IReadOnlyList<PlatformAuthEligibleOrganizationDto>>.Success(
            [
                new PlatformAuthEligibleOrganizationDto(orgId, "Store One", "store-one", "OrganizationOwner", Guid.NewGuid())
            ])
        };
        var sut = CreateSut("Development", access, tokens, prefs, current);
        var signIn = await sut.SignInAsync(new SignInRequest("owner", "password"));
        Assert.True(signIn.Succeeded);
        Assert.False(string.IsNullOrWhiteSpace(signIn.Session!.AccessToken));

        var select = await sut.SelectOrganizationAsync(orgId);
        Assert.True(select.Succeeded);
        Assert.Equal(orgId, select.Session!.OrganizationId);
        Assert.False(select.Session.HasPosAccess);
        Assert.Equal(orgId, await prefs.GetSelectedOrganizationIdAsync());
    }

    [Fact]
    public async Task SelectOrganization_maps_rate_limited_bind_to_retry_message()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var tokens = new MemorySecureTokenStore();
        var prefs = new MemoryOnboardingStore();
        var current = new CurrentUserContext();
        var access = new FakeAccessClient
        {
            LoginResult = ApiResult<PlatformLoginResultDto>.Success(new PlatformLoginResultDto(
                "platform-session",
                Guid.NewGuid(),
                userId,
                "owner",
                "Owner",
                "o@example.com",
                DateTimeOffset.UtcNow.AddHours(8),
                DateTimeOffset.UtcNow.AddHours(24),
                null,
                null,
                "none",
                1,
                AccountProfileId: Guid.NewGuid(),
                AccountClass: "Organization",
                AllowedScope: "Organization")),
            IssueTokenResult = ApiResult<PlatformAccessTokenIssueDto>.Success(new PlatformAccessTokenIssueDto(
                "opaque-token",
                "Bearer",
                Guid.NewGuid(),
                userId,
                "owner",
                "Owner",
                "o@example.com",
                DateTimeOffset.UtcNow.AddHours(8),
                null,
                null,
                null,
                "none",
                1,
                null,
                null)),
            BindTokenResult = ApiResult<PlatformAccessTokenIssueDto>.RateLimited(new ApiError(
                "Too Many Requests",
                "Request rate limit exceeded. Retry later.",
                "platform.rate_limit.exceeded",
                null,
                429))
        };
        var sut = CreateSut("Development", access, tokens, prefs, current);
        var signIn = await sut.SignInAsync(new SignInRequest("owner", "password"));
        Assert.True(signIn.Succeeded);

        var select = await sut.SelectOrganizationAsync(orgId);
        Assert.False(select.Succeeded);
        Assert.Equal(AuthFailureReason.RateLimited, select.FailureReason);
        Assert.Equal("Auth_RateLimited", select.SafeMessageKey);
    }

    [Fact]
    public async Task SelectOrganization_requires_allowed_access()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var tokens = new MemorySecureTokenStore();
        var prefs = new MemoryOnboardingStore();
        var access = new FakeAccessClient
        {
            UserResult = ApiResult<PlatformUserDto>.Success(User(userId, "Active")),
            OrganizationResult = ApiResult<PlatformOrganizationDto>.Success(Org(orgId)),
            EvaluateResult = ApiResult<EffectiveAccessDto>.Success(new EffectiveAccessDto(
                false, "subscription_ineligible", userId, orgId, PosProductCodes.PinoyBusinessPos,
                null, null, null, null, DateTimeOffset.UtcNow))
        };
        var sut = CreateSut("Development", access, tokens, prefs);
        await sut.SignInAsync(new SignInRequest(null, null, userId));
        var select = await sut.SelectOrganizationAsync(orgId);
        Assert.False(select.Succeeded);
        Assert.Equal(AuthFailureReason.AccessDenied, select.FailureReason);
        Assert.Null(await prefs.GetSelectedOrganizationIdAsync());
    }

    [Fact]
    public async Task SelectOrganization_succeeds_for_allowed_access()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var tokens = new MemorySecureTokenStore();
        var prefs = new MemoryOnboardingStore();
        var access = new FakeAccessClient
        {
            UserResult = ApiResult<PlatformUserDto>.Success(User(userId, "Active")),
            OrganizationResult = ApiResult<PlatformOrganizationDto>.Success(Org(orgId)),
            EvaluateResult = ApiResult<EffectiveAccessDto>.Success(new EffectiveAccessDto(
                true, "allowed", userId, orgId, PosProductCodes.PinoyBusinessPos,
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow,
                SubscriptionStatus: "Active",
                EnabledFeatureCodes: ["customer-credit-view", "customer-credit-repay", "customer-credit-create"]))
        };
        var sut = CreateSut("Development", access, tokens, prefs);
        await sut.SignInAsync(new SignInRequest(null, null, userId));
        var select = await sut.SelectOrganizationAsync(orgId);
        Assert.True(select.Succeeded);
        Assert.Equal(orgId, await prefs.GetSelectedOrganizationIdAsync());
        Assert.True(select.Session!.HasPosAccess);
        Assert.Equal("Active", select.Session.SubscriptionStatus);
        Assert.Contains("customer-credit-view", select.Session.EnabledFeatureCodes!);
        Assert.Contains("customer-credit-repay", select.Session.EnabledFeatureCodes!);
        Assert.Contains("customer-credit-create", select.Session.EnabledFeatureCodes!);
        Assert.Equal("Active", await tokens.GetAsync(SecureTokenKeys.SubscriptionStatus));
        Assert.Contains("customer-credit-view", await tokens.GetAsync(SecureTokenKeys.FeatureGrants) ?? string.Empty);
    }

    [Fact]
    public async Task SignIn_password_grant_works_in_production()
    {
        var userId = Guid.NewGuid();
        var tokens = new MemorySecureTokenStore();
        var access = new FakeAccessClient
        {
            LoginResult = ApiResult<PlatformLoginResultDto>.Success(new PlatformLoginResultDto(
                "platform-session",
                Guid.NewGuid(),
                userId,
                "cashier",
                "Cashier",
                "c@example.com",
                DateTimeOffset.UtcNow.AddHours(8),
                DateTimeOffset.UtcNow.AddHours(24),
                null,
                null,
                "none",
                2,
                AccountProfileId: Guid.NewGuid(),
                AccountClass: "Personal",
                AllowedScope: "personal")),
            IssueTokenResult = ApiResult<PlatformAccessTokenIssueDto>.Success(new PlatformAccessTokenIssueDto(
                "opaque-token",
                "Bearer",
                Guid.NewGuid(),
                userId,
                "cashier",
                "Cashier",
                "c@example.com",
                DateTimeOffset.UtcNow.AddHours(8),
                null,
                null,
                null,
                "none",
                2,
                null,
                null))
        };
        var sut = CreateSut("Production", access, tokens);
        var result = await sut.SignInAsync(new SignInRequest("cashier", "secret"));
        Assert.True(result.Succeeded);
        Assert.Equal("opaque-token", result.Session!.AccessToken);
        Assert.Equal("platform-session", result.Session.PlatformSessionToken);
        Assert.Equal("opaque-token", await tokens.GetAsync(SecureTokenKeys.AccessToken));
        Assert.Equal("platform-session", await tokens.GetAsync(SecureTokenKeys.PlatformSessionToken));
    }

    [Fact]
    public async Task SignIn_with_platform_session_token_hydrates_from_auth_me()
    {
        var userId = Guid.NewGuid();
        var tokens = new MemorySecureTokenStore();
        var now = DateTimeOffset.UtcNow;
        var access = new FakeAccessClient
        {
            AuthMeResult = ApiResult<PlatformAuthSessionInfoDto>.Success(new PlatformAuthSessionInfoDto(
                Guid.NewGuid(),
                userId,
                "owner",
                "Owner",
                "o@example.com",
                now.AddHours(8),
                now.AddHours(24),
                now,
                null,
                null,
                "none",
                0,
                AccountProfileId: Guid.NewGuid(),
                AccountClass: "Personal")),
            IssueTokenResult = ApiResult<PlatformAccessTokenIssueDto>.Success(new PlatformAccessTokenIssueDto(
                "opaque-token",
                "Bearer",
                Guid.NewGuid(),
                userId,
                "owner",
                "Owner",
                "o@example.com",
                now.AddHours(8),
                null,
                null,
                null,
                "none",
                0,
                null,
                null))
        };
        var sut = CreateSut("Development", access, tokens);
        var result = await sut.SignInWithPlatformSessionTokenAsync("google-session-token");
        Assert.True(result.Succeeded);
        Assert.Equal(userId, result.Session!.UserId);
        Assert.Equal("opaque-token", result.Session.AccessToken);
        Assert.Equal("google-session-token", result.Session.PlatformSessionToken);
        Assert.Equal("google-session-token", await tokens.GetAsync(SecureTokenKeys.PlatformSessionToken));
    }

    [Fact]
    public async Task Logout_preserves_selected_organization_preference()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var tokens = new MemorySecureTokenStore();
        var prefs = new MemoryOnboardingStore();
        await prefs.SetSelectedOrganizationIdAsync(orgId);
        var access = new FakeAccessClient
        {
            UserResult = ApiResult<PlatformUserDto>.Success(User(userId, "Active"))
        };
        var sut = CreateSut("Development", access, tokens, prefs);
        await sut.SignInAsync(new SignInRequest(null, null, userId));
        await sut.LogoutAsync();
        Assert.Equal(orgId, await prefs.GetSelectedOrganizationIdAsync());
        Assert.Null(await tokens.GetAsync(SecureTokenKeys.UserId));
    }

    [Fact]
    public async Task SwitchToPersonal_clears_organization_from_session()
    {
        var userId = Guid.NewGuid();
        var orgId = Guid.NewGuid();
        var tokens = new MemorySecureTokenStore();
        var prefs = new MemoryOnboardingStore();
        var current = new CurrentUserContext();
        var access = new FakeAccessClient
        {
            UserResult = ApiResult<PlatformUserDto>.Success(User(userId, "Active")),
            OrganizationResult = ApiResult<PlatformOrganizationDto>.Success(Org(orgId)),
            EvaluateResult = ApiResult<EffectiveAccessDto>.Success(new EffectiveAccessDto(
                true, "allowed", userId, orgId, PosProductCodes.PinoyBusinessPos,
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow,
                SubscriptionStatus: "Active",
                EnabledFeatureCodes: ["customer-credit-view"]))
        };
        var sut = CreateSut("Development", access, tokens, prefs, current);
        await sut.SignInAsync(new SignInRequest(null, null, userId));
        var select = await sut.SelectOrganizationAsync(orgId);
        Assert.True(select.Succeeded);
        Assert.Equal(orgId, current.Session?.OrganizationId);

        var personal = await sut.SwitchToPersonalAsync();
        Assert.True(personal.Succeeded);
        Assert.Null(current.Session?.OrganizationId);
        Assert.False(current.HasPosAccess);
        Assert.Null(await prefs.GetSelectedOrganizationIdAsync());
    }

    [Fact]
    public void ProductAccessResolver_maps_subscription_ineligible_reason()
    {
        Assert.Equal("Access_SubscriptionIneligible", ProductAccessResolver.MapReasonKey("subscription_ineligible"));
        Assert.Equal("Access_AssignmentRevoked", ProductAccessResolver.MapReasonKey("product_assignment_inactive"));
    }

    [Fact]
    public async Task Server_unreachable_offers_offline_pin_when_grant_valid()
    {
        var harness = await SeedOfflineGrantHarnessAsync();
        harness.Access.IntrospectThrows = true;

        var restore = await harness.Auth.RestoreSessionAsync();
        Assert.False(restore.Succeeded);
        Assert.Equal(AuthFailureReason.Offline, restore.FailureReason);
        Assert.Equal("Offline_PinRequired", restore.SafeMessageKey);
        Assert.Equal("offline_pin_required", harness.Current.Session?.AccessReasonCode);
        Assert.False(harness.Current.HasPosAccess);
    }

    [Fact]
    public async Task Explicit_server_access_denial_clears_offline_grant()
    {
        var harness = await SeedOfflineGrantHarnessAsync();
        harness.Access.IntrospectResult = ApiResult<PlatformAccessTokenIntrospectionDto>.Success(
            new PlatformAccessTokenIntrospectionDto(
                Active: true,
                TokenId: Guid.NewGuid(),
                UserId: harness.UserId,
                Username: "cashier1",
                DisplayName: "Cashier",
                OrganizationId: harness.OrgId,
                OrganizationDisplayName: "Store",
                ProductCode: PosProductCodes.PinoyBusinessPos,
                ExpiresAtUtc: DateTimeOffset.UtcNow.AddHours(1),
                ProductAccessAllowed: false,
                ProductAccessReasonCode: "product_assignment_inactive",
                SubscriptionStatus: "Active",
                EnabledFeatureCodes: []));

        var restore = await harness.Auth.RestoreSessionAsync();
        Assert.True(restore.Succeeded);
        Assert.False(restore.Session!.HasPosAccess);
        Assert.Null(await harness.GrantStore.LoadGrantAsync());
    }

    [Fact]
    public async Task Offline_pin_unlock_restores_permission_snapshot()
    {
        var harness = await SeedOfflineGrantHarnessAsync();
        harness.Access.IntrospectThrows = true;
        await harness.Auth.RestoreSessionAsync();

        var unlock = await harness.Auth.UnlockOfflineWithPinAsync("123456");
        Assert.True(unlock.Succeeded);
        Assert.True(unlock.Session!.HasPosAccess);
        Assert.Equal("offline_grant", unlock.Session.AccessReasonCode);
        Assert.Equal(harness.OrgId, unlock.Session.OrganizationId);
        Assert.Contains("pos.sell", unlock.Session.EnabledFeatureCodes!);
        Assert.True(harness.AccessPolicy.AllowsOfflineMutation);
    }

    [Fact]
    public async Task Offline_wrong_pin_keeps_operate_access_denied()
    {
        var harness = await SeedOfflineGrantHarnessAsync();
        harness.Access.IntrospectThrows = true;
        await harness.Auth.RestoreSessionAsync();

        var unlock = await harness.Auth.UnlockOfflineWithPinAsync("000000");
        Assert.False(unlock.Succeeded);
        Assert.False(harness.Current.HasPosAccess);
    }

    private static async Task<OfflineGrantHarness> SeedOfflineGrantHarnessAsync()
    {
        var userId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var orgId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var tokens = new MemorySecureTokenStore();
        var prefs = new MemoryOnboardingStore();
        await prefs.SetSelectedOrganizationIdAsync(orgId);

        var sessionStore = new SecureSessionStore(tokens);
        var now = DateTimeOffset.UtcNow;
        var shell = new AuthSession(
            userId,
            "Cashier",
            "cashier1",
            "c@example.com",
            orgId,
            "Store",
            now,
            now.AddHours(8),
            HasPosAccess: true,
            AccessReasonCode: "allowed",
            SubscriptionStatus: "Active",
            EnabledFeatureCodes: ["pos.sell"],
            AccessToken: "opaque-token");
        await sessionStore.SaveAsync(shell, Guid.NewGuid().ToString("N"));

        var grantStore = new MemoryOfflineGrantStore();
        var device = new FakeDeviceIdentity("device-a");
        var grantOptions = Options.Create(new OfflineOperatingGrantOptions
        {
            DurationHours = 24,
            PinMinLength = 6,
            MaxFailedPinAttempts = 5,
            PinLockoutMinutes = 15,
            PinHashIterations = 10_000
        });
        var grantService = new OfflineOperatingGrantService(grantStore, device, grantOptions);
        await grantService.EstablishFromOnlineSessionAsync(shell, device.DeviceId, "Cashier");
        Assert.True((await grantService.SetPinAsync("123456")).Succeeded);
        grantService = new OfflineOperatingGrantService(grantStore, device, grantOptions);

        var current = new CurrentUserContext();
        var connectivity = new FakeConnectivity(online: false);
        var accessPolicy = new ProtectedShellAccessPolicy(current, connectivity);
        await accessPolicy.InitializeAsync();

        var access = new FakeAccessClient();
        var events = new LoggingAuthEventSink(NullLogger<LoggingAuthEventSink>.Instance);
        var auth = new AuthenticationService(
            new StubAppInfo("Development"),
            sessionStore,
            current,
            prefs,
            access,
            events,
            localContext: null,
            accessPolicy: accessPolicy,
            timeProvider: null,
            offlineGrant: grantService,
            deviceIdentity: device);

        return new OfflineGrantHarness(auth, access, current, accessPolicy, grantStore, userId, orgId);
    }

    private sealed record OfflineGrantHarness(
        AuthenticationService Auth,
        FakeAccessClient Access,
        CurrentUserContext Current,
        ProtectedShellAccessPolicy AccessPolicy,
        MemoryOfflineGrantStore GrantStore,
        Guid UserId,
        Guid OrgId);

    private sealed class FakeDeviceIdentity(string deviceId) : IDeviceIdentityProvider
    {
        public string DeviceId { get; } = deviceId;

        public Task<string> GetOrCreateDeviceIdAsync(CancellationToken ct = default) =>
            Task.FromResult(DeviceId);
    }

    private sealed class FakeConnectivity(bool online) : IConnectivityService
    {
        public event EventHandler<ConnectivityStatus>? ConnectivityChanged
        {
            add { }
            remove { }
        }

        public Task<bool> IsConnectedAsync(CancellationToken ct = default) => Task.FromResult(online);
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

    private static AuthenticationService CreateSut(
        string environment,
        FakeAccessClient access,
        ISecureTokenStore? tokens = null,
        IOnboardingPreferenceStore? prefs = null,
        ICurrentUserContext? currentUser = null,
        TimeProvider? time = null)
    {
        tokens ??= new MemorySecureTokenStore();
        prefs ??= new MemoryOnboardingStore();
        currentUser ??= new CurrentUserContext();
        var sessionStore = new SecureSessionStore(tokens);
        var events = new LoggingAuthEventSink(NullLogger<LoggingAuthEventSink>.Instance);
        return new AuthenticationService(
            new StubAppInfo(environment),
            sessionStore,
            currentUser,
            prefs,
            access,
            events,
            localContext: null,
            timeProvider: time);
    }

    private static PlatformUserDto User(Guid id, string status) =>
        new(id, "user", "User", "u@example.com", status, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, null, null);

    private static PlatformOrganizationDto Org(Guid id) =>
        new(id, "Store One", "store-one", "Active", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    private sealed class StubAppInfo(string environment) : IAppInfoService
    {
        public string AppName => "Test";
        public string Version => "0";
        public string EnvironmentName { get; } = environment;
    }

    private sealed class FakeAccessClient : IPlatformAccessClient
    {
        public ApiResult<PlatformUserDto> UserResult { get; set; } = ApiResult<PlatformUserDto>.Unavailable();
        public ApiResult<PlatformOrganizationDto> OrganizationResult { get; set; } = ApiResult<PlatformOrganizationDto>.Unavailable();
        public ApiResult<EffectiveAccessDto> EvaluateResult { get; set; } = ApiResult<EffectiveAccessDto>.Unavailable();
        public ApiResult<PlatformAccessTokenIssueDto> IssueTokenResult { get; set; } = ApiResult<PlatformAccessTokenIssueDto>.Unavailable();
        public ApiResult<PlatformAccessTokenIssueDto> BindTokenResult { get; set; } = ApiResult<PlatformAccessTokenIssueDto>.Unavailable();
        public ApiResult<PlatformAccessTokenIntrospectionDto> IntrospectResult { get; set; } =
            ApiResult<PlatformAccessTokenIntrospectionDto>.Unavailable();
        public bool IntrospectThrows { get; set; }
        public ApiResult<PlatformLoginResultDto> LoginResult { get; set; } = ApiResult<PlatformLoginResultDto>.Unavailable();
        public ApiResult<IReadOnlyList<PlatformAuthEligibleOrganizationDto>> AuthEligibleOrganizationsResult { get; set; } =
            ApiResult<IReadOnlyList<PlatformAuthEligibleOrganizationDto>>.Unavailable();

        public Task<ApiResult<PlatformUserDto>> GetUserAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(UserResult);

        public Task<ApiResult<PlatformOrganizationDto>> GetOrganizationAsync(Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult(OrganizationResult);

        public Task<ApiResult<PlatformPagedResult<PlatformMembershipDto>>> GetUserMembershipsAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PlatformPagedResult<PlatformMembershipDto>>.Success(
                new PlatformPagedResult<PlatformMembershipDto>([], 0, 1, 100)));

        public Task<ApiResult<EffectiveAccessDto>> EvaluateAccessAsync(Guid userId, Guid organizationId, string productCode, CancellationToken ct = default) =>
            Task.FromResult(EvaluateResult);

        public Task<ApiResult<PlatformAccessTokenIssueDto>> IssueTokenAsync(IssuePlatformAccessTokenRequest request, CancellationToken ct = default) =>
            Task.FromResult(IssueTokenResult);

        public Task<ApiResult<PlatformAccessTokenIssueDto>> BindTokenAsync(BindPlatformAccessTokenRequest request, CancellationToken ct = default) =>
            Task.FromResult(BindTokenResult);

        public Task<ApiResult<PlatformAccessTokenIntrospectionDto>> IntrospectTokenAsync(string? token = null, CancellationToken ct = default)
        {
            if (IntrospectThrows)
            {
                throw new HttpRequestException("unreachable");
            }

            return Task.FromResult(IntrospectResult);
        }

        public Task<ApiResult<object>> RevokeAccessTokenAsync(CancellationToken ct = default) =>
            Task.FromResult(ApiResult<object>.Success(new object()));

        public Task<ApiResult<IReadOnlyList<PlatformAuthEligibleOrganizationDto>>> GetAuthEligibleOrganizationsAsync(CancellationToken ct = default) =>
            Task.FromResult(AuthEligibleOrganizationsResult);

        public Task<ApiResult<PlatformOrganizationDto>> UpdateOrganizationAsync(Guid organizationId, UpdatePlatformOrganizationRequest request, CancellationToken ct = default) =>
            Task.FromResult(OrganizationResult);

        public Task<ApiResult<PlatformPagedResult<PlatformMembershipDto>>> GetOrganizationMembersAsync(Guid organizationId, int page = 1, int pageSize = 50, string? status = null, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PlatformPagedResult<PlatformMembershipDto>>.Success(new PlatformPagedResult<PlatformMembershipDto>([], 0, page, pageSize)));

        public Task<ApiResult<PlatformMembershipDto>> SuspendMembershipAsync(Guid membershipId, PlatformMembershipLifecycleRequest request, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PlatformMembershipDto>.Unavailable());

        public Task<ApiResult<PlatformMembershipDto>> RevokeMembershipAsync(Guid membershipId, PlatformMembershipLifecycleRequest request, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PlatformMembershipDto>.Unavailable());

        public Task<ApiResult<PersonalRegistrationAckDto>> RegisterPersonalAccountAsync(RegisterPersonalAccountRequest request, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PersonalRegistrationAckDto>.Unavailable());

        public Task<ApiResult<object>> ActivatePersonalAccountAsync(ActivatePersonalAccountRequest request, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<object>.Unavailable());

        public Task<ApiResult<PlatformLoginResultDto>> LoginAsync(PlatformLoginRequest request, CancellationToken ct = default) =>
            Task.FromResult(LoginResult);

        public ApiResult<PlatformAuthSessionInfoDto> AuthMeResult { get; set; } =
            ApiResult<PlatformAuthSessionInfoDto>.Unavailable();

        public Task<ApiResult<PlatformAuthSessionInfoDto>> GetAuthMeAsync(CancellationToken ct = default) =>
            Task.FromResult(AuthMeResult);

        public Task<ApiResult<CredentialWorkflowAckDto>> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<CredentialWorkflowAckDto>.Unavailable());

        public Task<ApiResult<object>> LogoutSessionAsync(CancellationToken ct = default) =>
            Task.FromResult(ApiResult<object>.Success(new object()));

        public Task<ApiResult<PlatformLoginResultDto>> SelectAccountProfileAsync(SelectAccountProfileRequest request, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PlatformLoginResultDto>.Unavailable());

        public Task<ApiResult<StartBusinessResultDto>> StartBusinessAsync(StartBusinessRequest request, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<StartBusinessResultDto>.Unavailable());

        public Task<ApiResult<IReadOnlyList<CommercialPlanDto>>> GetCommercialPlansAsync(string? productCode = null, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<IReadOnlyList<CommercialPlanDto>>.Success(Array.Empty<CommercialPlanDto>()));

        public Task<ApiResult<OrganizationInvitationDto>> CreateOrganizationInvitationAsync(Guid organizationId, CreateInvitationRequest request, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<OrganizationInvitationDto>.Unavailable());

        public Task<ApiResult<IReadOnlyList<ProductLocalRoleGrantDto>>> GetProductLocalRolesAsync(Guid organizationId, string? status = null, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<IReadOnlyList<ProductLocalRoleGrantDto>>.Success(Array.Empty<ProductLocalRoleGrantDto>()));

        public Task<ApiResult<ProductLocalRoleGrantDto>> AssignProductLocalRoleAsync(Guid organizationId, AssignProductLocalRoleRequest request, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<ProductLocalRoleGrantDto>.Unavailable());

        public Task<ApiResult<ProductLocalRoleGrantDto>> RevokeProductLocalRoleAsync(Guid organizationId, Guid grantId, RevokeProductLocalRoleRequest request, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<ProductLocalRoleGrantDto>.Unavailable());

        public Task<ApiResult<PlatformSubscriptionDto>> GetCurrentSubscriptionAsync(Guid organizationId, string productCode, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PlatformSubscriptionDto>.Unavailable());

        public Task<ApiResult<PlatformEntitlementSnapshotDto>> GetLatestEntitlementAsync(Guid organizationId, string productCode, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PlatformEntitlementSnapshotDto>.Unavailable());

        public Task<ApiResult<object>> SetOrganizationContextAsync(SetOrganizationContextRequest request, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<object>.Success(new object()));

        public Task<ApiResult<IReadOnlyList<PlatformAccountProfileDto>>> GetAccountProfilesAsync(CancellationToken ct = default) =>
            Task.FromResult(ApiResult<IReadOnlyList<PlatformAccountProfileDto>>.Success(Array.Empty<PlatformAccountProfileDto>()));

        public Task<ApiResult<IReadOnlyList<PendingOrganizationInvitationDto>>> GetPendingOrganizationInvitationsAsync(CancellationToken ct = default) =>
            Task.FromResult(ApiResult<IReadOnlyList<PendingOrganizationInvitationDto>>.Success(
                Array.Empty<PendingOrganizationInvitationDto>()));

        public Task<ApiResult<AcceptOrganizationInvitationResultDto>> AcceptOrganizationInvitationAsync(
            string token,
            string password,
            CancellationToken ct = default) =>
            Task.FromResult(ApiResult<AcceptOrganizationInvitationResultDto>.Unavailable());

        public Task<ApiResult<PlatformMembershipDto>> AcceptOrganizationInvitationByIdAsync(Guid invitationId, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PlatformMembershipDto>.Unavailable());

        public Task<ApiResult<PersonalDashboardDto>> GetPersonalDashboardAsync(CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PersonalDashboardDto>.Unavailable());

        public Task<ApiResult<PersonalProfileDto>> GetPersonalProfileAsync(CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PersonalProfileDto>.Unavailable());

        public Task<ApiResult<PublicIdentityDto>> GetMyPublicIdentityAsync(CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PublicIdentityDto>.Unavailable());

        public Task<ApiResult<ResolvedPublicUserDto>> ResolvePublicUserIdAsync(ResolvePublicUserIdRequest request, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<ResolvedPublicUserDto>.Unavailable());

        public Task<ApiResult<PersonalAccountSettingsDto>> GetPersonalSettingsAsync(CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PersonalAccountSettingsDto>.Unavailable());

        public Task<ApiResult<PersonalAccountSettingsDto>> UpdatePersonalSettingsAsync(UpdatePersonalAccountSettingsRequest request, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PersonalAccountSettingsDto>.Unavailable());

        public Task<ApiResult<IReadOnlyList<PersonalContactDto>>> GetPersonalContactsAsync(CancellationToken ct = default) =>
            Task.FromResult(ApiResult<IReadOnlyList<PersonalContactDto>>.Success(Array.Empty<PersonalContactDto>()));

        public Task<ApiResult<PersonalContactDto>> CreatePersonalContactAsync(CreatePersonalContactRequest request, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PersonalContactDto>.Unavailable());

        public Task<ApiResult<IReadOnlyList<PersonalDebtRelationshipSummaryDto>>> GetPersonalUtangLentAsync(CancellationToken ct = default) =>
            Task.FromResult(ApiResult<IReadOnlyList<PersonalDebtRelationshipSummaryDto>>.Success(Array.Empty<PersonalDebtRelationshipSummaryDto>()));

        public Task<ApiResult<IReadOnlyList<PersonalDebtRelationshipSummaryDto>>> GetPersonalUtangBorrowedAsync(CancellationToken ct = default) =>
            Task.FromResult(ApiResult<IReadOnlyList<PersonalDebtRelationshipSummaryDto>>.Success(Array.Empty<PersonalDebtRelationshipSummaryDto>()));

        public Task<ApiResult<PersonalDebtRelationshipSummaryDto>> CreatePersonalDebtRelationshipAsync(CreatePersonalDebtRelationshipRequest request, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PersonalDebtRelationshipSummaryDto>.Unavailable());

        public Task<ApiResult<PersonalDebtRelationshipSummaryDto>> GetPersonalDebtRelationshipAsync(Guid relationshipId, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PersonalDebtRelationshipSummaryDto>.Unavailable());

        public Task<ApiResult<PersonalUtangBalanceDto>> GetPersonalUtangBalanceAsync(Guid relationshipId, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PersonalUtangBalanceDto>.Unavailable());

        public Task<ApiResult<IReadOnlyList<PersonalUtangEntryDto>>> GetPersonalUtangHistoryAsync(Guid relationshipId, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<IReadOnlyList<PersonalUtangEntryDto>>.Success(Array.Empty<PersonalUtangEntryDto>()));

        public Task<ApiResult<PersonalUtangEntryDto>> RecordPersonalUtangEntryAsync(Guid relationshipId, RecordPersonalUtangEntryRequest request, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PersonalUtangEntryDto>.Unavailable());

        public Task<ApiResult<IReadOnlyList<PersonalUtangInvitationDto>>> GetPersonalUtangInvitationsAsync(CancellationToken ct = default) =>
            Task.FromResult(ApiResult<IReadOnlyList<PersonalUtangInvitationDto>>.Success(Array.Empty<PersonalUtangInvitationDto>()));

        public Task<ApiResult<PersonalUtangInvitationDto>> CreatePersonalUtangInvitationAsync(Guid relationshipId, CreatePersonalUtangInvitationRequest request, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PersonalUtangInvitationDto>.Unavailable());

        public Task<ApiResult<PersonalUtangInvitationAcceptResultDto>> AcceptPersonalUtangInvitationAsync(string token, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PersonalUtangInvitationAcceptResultDto>.Unavailable());

        public Task<ApiResult<PersonalUtangInvitationDto>> DeclinePersonalUtangInvitationAsync(string token, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PersonalUtangInvitationDto>.Unavailable());

        public Task<ApiResult<IReadOnlyList<LocalValidationQuickLoginIdentityDto>>> GetLocalValidationQuickLoginIdentitiesAsync(
            CancellationToken ct = default) =>
            Task.FromResult(ApiResult<IReadOnlyList<LocalValidationQuickLoginIdentityDto>>.Success(
                Array.Empty<LocalValidationQuickLoginIdentityDto>()));
    }

    private sealed class MemorySecureTokenStore : ISecureTokenStore
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
            foreach (var key in _values.Keys.ToList())
            {
                _values.Remove(key);
            }

            return Task.CompletedTask;
        }
    }

    private sealed class MemoryOnboardingStore : IOnboardingPreferenceStore
    {
        private bool _completed;
        private string? _step;
        private Guid? _org;
        private bool _devConfirmed;

        public string? PeekSecretLeak() => null;

        public Task<bool> GetOnboardingCompletedAsync(CancellationToken ct = default) => Task.FromResult(_completed);
        public Task SetOnboardingCompletedAsync(bool completed, CancellationToken ct = default) { _completed = completed; return Task.CompletedTask; }
        public Task<string?> GetOnboardingStepAsync(CancellationToken ct = default) => Task.FromResult(_step);
        public Task SetOnboardingStepAsync(string step, CancellationToken ct = default) { _step = step; return Task.CompletedTask; }
        public Task<Guid?> GetSelectedOrganizationIdAsync(CancellationToken ct = default) => Task.FromResult(_org);
        public Task SetSelectedOrganizationIdAsync(Guid? organizationId, CancellationToken ct = default) { _org = organizationId; return Task.CompletedTask; }
        public Task<bool> GetDevEnvironmentConfirmedAsync(CancellationToken ct = default) => Task.FromResult(_devConfirmed);
        public Task SetDevEnvironmentConfirmedAsync(bool confirmed, CancellationToken ct = default) { _devConfirmed = confirmed; return Task.CompletedTask; }
        public Task ClearOrganizationPreferenceAsync(CancellationToken ct = default) { _org = null; return Task.CompletedTask; }
    }

    private sealed class FakeTimeProvider(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _utc = start;
        public override DateTimeOffset GetUtcNow() => _utc;
        public void SetUtcNow(DateTimeOffset value) => _utc = value;
    }
}
