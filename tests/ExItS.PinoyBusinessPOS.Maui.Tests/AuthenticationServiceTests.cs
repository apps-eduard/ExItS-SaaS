using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Platform;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class AuthenticationServiceTests
{
    [Fact]
    public async Task SignIn_is_blocked_outside_development_testing()
    {
        var sut = CreateSut(environment: "Production", access: new FakeAccessClient());
        var result = await sut.SignInAsync(new SignInRequest(Guid.NewGuid()));
        Assert.False(result.Succeeded);
        Assert.Equal(AuthFailureReason.ProductionAuthUnavailable, result.FailureReason);
    }

    [Fact]
    public async Task SignIn_rejects_unknown_user()
    {
        var access = new FakeAccessClient { UserResult = ApiResult<PlatformUserDto>.NotFound() };
        var sut = CreateSut("Development", access);
        var result = await sut.SignInAsync(new SignInRequest(Guid.NewGuid()));
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
        var result = await sut.SignInAsync(new SignInRequest(userId));
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
        var result = await sut.SignInAsync(new SignInRequest(userId));
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
        await sut.SignInAsync(new SignInRequest(userId));
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
        await sut.SignInAsync(new SignInRequest(userId));
        clock.SetUtcNow(DateTimeOffset.Parse("2026-01-02T00:00:00Z"));
        var restore = await sut.RestoreSessionAsync();
        Assert.False(restore.Succeeded);
        Assert.Equal(AuthFailureReason.SessionExpired, restore.FailureReason);
        Assert.Null(await tokens.GetAsync(SecureTokenKeys.UserId));
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
        await sut.SignInAsync(new SignInRequest(userId));
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
                Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow))
        };
        var sut = CreateSut("Development", access, tokens, prefs);
        await sut.SignInAsync(new SignInRequest(userId));
        var select = await sut.SelectOrganizationAsync(orgId);
        Assert.True(select.Succeeded);
        Assert.Equal(orgId, await prefs.GetSelectedOrganizationIdAsync());
        Assert.True(select.Session!.HasPosAccess);
    }

    [Fact]
    public void ProductAccessResolver_maps_subscription_ineligible_reason()
    {
        Assert.Equal("Access_SubscriptionIneligible", ProductAccessResolver.MapReasonKey("subscription_ineligible"));
        Assert.Equal("Access_AssignmentRevoked", ProductAccessResolver.MapReasonKey("product_assignment_inactive"));
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
        var resolver = new ProductAccessResolver(access);
        var events = new LoggingAuthEventSink(NullLogger<LoggingAuthEventSink>.Instance);
        return new AuthenticationService(
            new StubAppInfo(environment),
            sessionStore,
            currentUser,
            prefs,
            access,
            resolver,
            events,
            time);
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

        public Task<ApiResult<PlatformUserDto>> GetUserAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(UserResult);

        public Task<ApiResult<PlatformOrganizationDto>> GetOrganizationAsync(Guid organizationId, CancellationToken ct = default) =>
            Task.FromResult(OrganizationResult);

        public Task<ApiResult<PlatformPagedResult<PlatformMembershipDto>>> GetUserMembershipsAsync(Guid userId, CancellationToken ct = default) =>
            Task.FromResult(ApiResult<PlatformPagedResult<PlatformMembershipDto>>.Success(
                new PlatformPagedResult<PlatformMembershipDto>([], 0, 1, 100)));

        public Task<ApiResult<EffectiveAccessDto>> EvaluateAccessAsync(Guid userId, Guid organizationId, string productCode, CancellationToken ct = default) =>
            Task.FromResult(EvaluateResult);
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
