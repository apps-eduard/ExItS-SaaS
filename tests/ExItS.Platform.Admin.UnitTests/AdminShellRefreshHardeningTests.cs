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
    public async Task Failed_auth_me_does_not_cache_limited_shell_or_poison_permissions()
    {
        var api = CreateApiProxy(out var state);
        state.FailMe = true;

        var env = new HostingEnvironment { EnvironmentName = Environments.Development };
        var permissions = new PlatformPermissionState(api, env);
        var shell = new AdminShellContext(api, permissions);

        await shell.EnsureLoadedAsync();

        Assert.False(shell.Loaded);
        Assert.Equal(AdminShellMode.Limited, shell.Mode);
        Assert.False(permissions.Loaded);
        Assert.Equal(0, state.AuthorizationCalls);

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
        Assert.Equal(1, state.AuthorizationCalls);
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

        var nav = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Layout", "AdminNav.razor"));
        Assert.Contains("!Shell.Loaded", nav, StringComparison.Ordinal);
        Assert.Contains("RetryShellAsync", nav, StringComparison.Ordinal);
        Assert.Contains("_shellLoadFailed", nav, StringComparison.Ordinal);

        var shell = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Services", "AdminShellContext.cs"));
        Assert.Contains("Do NOT cache Limited forever", shell, StringComparison.Ordinal);
        Assert.Contains("_loadTask = null", shell, StringComparison.Ordinal);
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
                var result = ApiCallResult<ResolvedPermissionsDto>.Success(
                    new ResolvedPermissionsDto(
                        "platform.admin",
                        "Platform",
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
