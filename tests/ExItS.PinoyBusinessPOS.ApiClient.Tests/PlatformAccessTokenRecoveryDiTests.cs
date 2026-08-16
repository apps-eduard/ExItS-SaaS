using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Options;
using ExItS.PinoyBusinessPOS.Application.Platform;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExItS.PinoyBusinessPOS.ApiClient.Tests;

/// <summary>
/// Guards against Auth ↔ HttpClient DI cycles that crash MAUI Android at startup.
/// </summary>
public sealed class PlatformAccessTokenRecoveryDiTests
{
    [Fact]
    public void AuthenticationService_and_PosApiClient_resolve_without_circular_dependency()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["PosApi:BaseUrl"] = "http://127.0.0.1:8091",
                ["PosApi:TimeoutSeconds"] = "15",
                ["PosBusinessApi:BaseUrl"] = "http://127.0.0.1:8092",
                ["PosBusinessApi:TimeoutSeconds"] = "15"
            })
            .Build();

        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton<IConnectivityService, AlwaysOnline>();
        services.AddSingleton<ICurrentUserContext, CurrentUserContext>();
        services.AddSingleton<ISecureTokenStore, MemoryTokens>();
        services.AddSingleton<ISessionStore, SecureSessionStore>();
        services.AddSingleton<IOnboardingPreferenceStore, MemoryPrefs>();
        services.AddSingleton<IAppInfoService, StubAppInfo>();
        services.AddSingleton<IAuthEventSink>(_ => new LoggingAuthEventSink(NullLogger<LoggingAuthEventSink>.Instance));
        services.AddSingleton<IPosSyncStatusService, NoopSyncStatus>();
        services.AddSingleton<IDeviceIdentityProvider, StubDevice>();
        services.AddSingleton<AuthenticationService>();
        services.AddSingleton<IAuthenticationService>(sp => sp.GetRequiredService<AuthenticationService>());
        services.AddSingleton<IPlatformAccessTokenRecovery>(sp => sp.GetRequiredService<AuthenticationService>());
        services.AddPosApiClient(config);

        // Do not ValidateOnBuild — business-client handlers need more host wiring.
        // Resolving Auth then PosApiClient is the startup order that previously circular-crashed Android.
        using var sp = services.BuildServiceProvider();

        var auth = sp.GetRequiredService<IAuthenticationService>();
        var api = sp.GetRequiredService<IPosApiClient>();
        var recovery = sp.GetRequiredService<IPlatformAccessTokenRecovery>();
        // Force HttpClient pipeline construction (handlers resolve here).
        _ = api.GetType();

        Assert.NotNull(auth);
        Assert.NotNull(api);
        Assert.Same(auth, recovery);
    }

    private sealed class StubDevice : IDeviceIdentityProvider
    {
        public Task<string> GetOrCreateDeviceIdAsync(CancellationToken ct = default) =>
            Task.FromResult("test-device");
    }

    private sealed class AlwaysOnline : IConnectivityService
    {
        public event EventHandler<ConnectivityStatus>? ConnectivityChanged
        {
            add { }
            remove { }
        }

        public Task<bool> IsConnectedAsync(CancellationToken ct = default) => Task.FromResult(true);
    }

    private sealed class MemoryTokens : ISecureTokenStore
    {
        private readonly Dictionary<string, string> _values = new(StringComparer.Ordinal);

        public Task<string?> GetAsync(string key, CancellationToken ct = default) =>
            Task.FromResult(_values.TryGetValue(key, out var v) ? v : null);

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

    private sealed class MemoryPrefs : IOnboardingPreferenceStore
    {
        public Task<bool> GetOnboardingCompletedAsync(CancellationToken ct = default) => Task.FromResult(false);
        public Task SetOnboardingCompletedAsync(bool completed, CancellationToken ct = default) => Task.CompletedTask;
        public Task<string?> GetOnboardingStepAsync(CancellationToken ct = default) => Task.FromResult<string?>(null);
        public Task SetOnboardingStepAsync(string step, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Guid?> GetSelectedOrganizationIdAsync(CancellationToken ct = default) => Task.FromResult<Guid?>(null);
        public Task SetSelectedOrganizationIdAsync(Guid? organizationId, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> GetDevEnvironmentConfirmedAsync(CancellationToken ct = default) => Task.FromResult(false);
        public Task SetDevEnvironmentConfirmedAsync(bool confirmed, CancellationToken ct = default) => Task.CompletedTask;
        public Task ClearOrganizationPreferenceAsync(CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> GetBusinessTemplatePromptPendingAsync(Guid organizationId, CancellationToken ct = default) => Task.FromResult(false);
        public Task SetBusinessTemplatePromptPendingAsync(Guid organizationId, bool pending, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> GetBusinessTypeActivationPromptPendingAsync(Guid organizationId, CancellationToken ct = default) => Task.FromResult(false);
        public Task SetBusinessTypeActivationPromptPendingAsync(Guid organizationId, bool pending, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class StubAppInfo : IAppInfoService
    {
        public string AppName => "Test";
        public string Version => "0";
        public string EnvironmentName => "Development";
    }

    private sealed class NoopSyncStatus : IPosSyncStatusService
    {
        public PosSyncStatusSnapshot Current { get; } = new(PosSyncStatusKind.Online);
        public event Func<Task>? Changed
        {
            add { }
            remove { }
        }

        public Task InitializeAsync(CancellationToken ct = default) => Task.CompletedTask;
        public void SetReconnectRequired(bool required) { }
        public void SetRecoveryRequired(bool required) { }
        public void NotifyApiReachability(bool reachable) { }
        public void Refresh() { }
    }
}
