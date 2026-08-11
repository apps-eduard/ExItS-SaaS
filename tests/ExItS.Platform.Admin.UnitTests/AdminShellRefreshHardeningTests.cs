using System.Net;
using System.Reflection;
using ExItS.Platform.Admin.Models;
using ExItS.Platform.Admin.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace ExItS.Platform.Admin.UnitTests;

public sealed class AdminShellRefreshHardeningTests
{
    [Fact]
    public async Task Failed_auth_me_recovers_platform_shell_from_authorization()
    {
        var api = CreateApiProxy(out var state);
        state.FailMe = true;
        state.AuthorizationActorType = "Platform";

        var env = new HostingEnvironment { EnvironmentName = Environments.Development };
        var permissions = new PlatformPermissionState(api, env);
        var shell = new AdminShellContext(api, permissions);

        await shell.EnsureLoadedAsync();

        Assert.True(shell.Loaded);
        Assert.True(shell.IsPlatformShell);
        Assert.True(permissions.Loaded);
        Assert.True(permissions.HasPermission(PlatformPermissionCodes.ViewPortfolio));
        Assert.True(state.AuthorizationCalls >= 1);
    }

    [Fact]
    public async Task Failed_auth_me_does_not_cache_limited_shell_or_poison_permissions()
    {
        var api = CreateApiProxy(out var state);
        state.FailMe = true;
        state.AuthorizationActorType = null; // force hard failure path

        var env = new HostingEnvironment { EnvironmentName = Environments.Development };
        var permissions = new PlatformPermissionState(api, env);
        var shell = new AdminShellContext(api, permissions);

        await shell.EnsureLoadedAsync();

        Assert.False(shell.Loaded);
        Assert.Equal(AdminShellMode.Limited, shell.Mode);

        state.FailMe = false;
        state.Me = new AuthSessionInfoDto(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "platform.admin",
            "Platform Admin",
            "platform.admin@example.com",
            DateTimeOffset.UtcNow.AddHours(1),
            DateTimeOffset.UtcNow.AddHours(8),
            DateTimeOffset.UtcNow,
            null,
            null,
            "None",
            0,
            AccountProfileId: null,
            AccountClass: "Platform",
            AllowedScope: "Platform");

        // EnsureLoadedAsync must retry after a transient auth/me failure (not only RefreshAsync).
        await shell.EnsureLoadedAsync();

        Assert.True(shell.Loaded);
        Assert.True(shell.IsPlatformShell);
        Assert.True(permissions.Loaded);
        Assert.True(permissions.HasPermission(PlatformPermissionCodes.ViewPortfolio));
    }

    [Fact]
    public async Task Shell_changed_fires_when_authorization_recovery_loads_platform()
    {
        var api = CreateApiProxy(out var state);
        state.FailMe = true;
        state.AuthorizationActorType = "Platform";

        var env = new HostingEnvironment { EnvironmentName = Environments.Development };
        var permissions = new PlatformPermissionState(api, env);
        var shell = new AdminShellContext(api, permissions);

        var changed = 0;
        shell.Changed += () => changed++;

        await shell.EnsureLoadedAsync();

        Assert.True(shell.Loaded);
        Assert.True(shell.IsPlatformShell);
        Assert.True(changed >= 1);
    }

    [Fact]
    public void Commercial_list_pages_and_nav_use_shell_ready_hardening()
    {
        var root = FindRepoRoot();
        foreach (var relative in new[]
                 {
                     Path.Combine("src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "Organizations.razor"),
                     Path.Combine("src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "Plans.razor"),
                     Path.Combine("src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "Subscriptions.razor")
                 })
        {
            var text = File.ReadAllText(Path.Combine(root, relative));
            Assert.Contains("AdminShellContext Shell", text, StringComparison.Ordinal);
            Assert.Contains("Shell.EnsureLoadedAsync()", text, StringComparison.Ordinal);
            Assert.Contains("_suppressInitialTableChange", text, StringComparison.Ordinal);
            Assert.Contains("_pageReady", text, StringComparison.Ordinal);
            Assert.Contains("Common_Retry", text, StringComparison.Ordinal);
        }

        foreach (var relative in new[]
                 {
                     Path.Combine("src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "GlobalCatalogBusinessTypes.razor"),
                     Path.Combine("src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "GlobalCatalogTemplates.razor"),
                     Path.Combine("src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "GlobalCatalogCategories.razor"),
                     Path.Combine("src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "GlobalCatalogProducts.razor"),
                     Path.Combine("src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "GlobalCatalogImports.razor"),
                     Path.Combine("src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "LocalValidationTestPayments.razor")
                 })
        {
            var text = File.ReadAllText(Path.Combine(root, relative));
            Assert.Contains("Permissions.EnsureLoadedAsync()", text, StringComparison.Ordinal);
        }

        foreach (var relative in new[]
                 {
                     Path.Combine("src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "GlobalCatalogBusinessTypes.razor"),
                     Path.Combine("src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "GlobalCatalogTemplates.razor")
                 })
        {
            var text = File.ReadAllText(Path.Combine(root, relative));
            Assert.Contains("AdminShellContext Shell", text, StringComparison.Ordinal);
            Assert.Contains("Shell.EnsureLoadedAsync()", text, StringComparison.Ordinal);
            Assert.Contains("_pageReady", text, StringComparison.Ordinal);
            Assert.Contains("_suppressInitialTableChange", text, StringComparison.Ordinal);
        }

        var nav = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Layout", "AdminNav.razor"));
        Assert.Contains("!Shell.Loaded", nav, StringComparison.Ordinal);
        Assert.Contains("RetryShellAsync", nav, StringComparison.Ordinal);
        Assert.Contains("_shellLoadFailed", nav, StringComparison.Ordinal);
        Assert.Contains("if (!_ready)", nav, StringComparison.Ordinal);
        Assert.Contains("Shell.Changed += OnShellChanged", nav, StringComparison.Ordinal);
        Assert.Contains("ApplyNavReadyFromShellAsync", nav, StringComparison.Ordinal);

        var layout = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Layout", "MainLayout.razor"));
        Assert.Contains("Shell.Changed += OnShellChanged", layout, StringComparison.Ordinal);

        Assert.Contains("TryToDomain", File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Infrastructure", "Persistence", "GlobalCatalogEntityMapper.cs")), StringComparison.Ordinal);
        Assert.Contains("TryToDomain", File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Infrastructure", "Persistence", "Repositories", "GlobalCatalogRepositories.cs")), StringComparison.Ordinal);

        var shell = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Services", "AdminShellContext.cs"));
        Assert.Contains("TryRecoverFromAuthorizationAsync", shell, StringComparison.Ordinal);
        Assert.Contains("_loadGeneration", shell, StringComparison.Ordinal);
        Assert.Contains("event Action? Changed", shell, StringComparison.Ordinal);
        Assert.Contains("RaiseChanged()", shell, StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }

    private static IPlatformApiClient CreateApiProxy(out ProxyState state)
    {
        state = new ProxyState();
        var proxy = DispatchProxy.Create<IPlatformApiClient, ApiDispatchProxy>();
        ((ApiDispatchProxy)(object)proxy).State = state;
        return proxy;
    }

    private sealed class ProxyState
    {
        public bool FailMe { get; set; } = true;
        public string? AuthorizationActorType { get; set; } = "Platform";
        public AuthSessionInfoDto? Me { get; set; }
        public int AuthorizationCalls { get; set; }
    }

    private class ApiDispatchProxy : DispatchProxy
    {
        public ProxyState State { get; set; } = new();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == nameof(IPlatformApiClient.GetAuthMeAsync))
            {
                var result = State.FailMe || State.Me is null
                    ? ApiCallResult<AuthSessionInfoDto>.Failed(
                        new PlatformApiException(HttpStatusCode.Unauthorized, "Unauthorized", "session"))
                    : ApiCallResult<AuthSessionInfoDto>.Success(State.Me);
                return Task.FromResult(result);
            }

            if (targetMethod.Name == nameof(IPlatformApiClient.GetMyAuthorizationAsync))
            {
                State.AuthorizationCalls++;
                if (string.IsNullOrWhiteSpace(State.AuthorizationActorType))
                {
                    var failed = ApiCallResult<ResolvedPermissionsDto>.Failed(
                        new PlatformApiException(HttpStatusCode.Unauthorized, "Unauthorized", "authz"));
                    return Task.FromResult(failed);
                }

                var result = ApiCallResult<ResolvedPermissionsDto>.Success(
                    new ResolvedPermissionsDto(
                        "platform.admin",
                        State.AuthorizationActorType,
                        null,
                        null,
                        [.. PlatformPermissionCodes.All]));
                return Task.FromResult(result);
            }

            if (targetMethod.Name == nameof(IPlatformApiClient.GetEligibleOrganizationsAsync))
            {
                var result = ApiCallResult<IReadOnlyList<EligibleOrganizationDto>>.Success(
                    Array.Empty<EligibleOrganizationDto>());
                return Task.FromResult(result);
            }

            throw new NotSupportedException($"Unexpected API call in shell test: {targetMethod.Name}");
        }
    }
}
