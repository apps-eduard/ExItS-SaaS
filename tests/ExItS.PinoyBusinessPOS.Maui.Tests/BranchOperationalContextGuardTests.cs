using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class BranchOperationalContextGuardTests
{
    [Fact]
    public void Header_shows_org_and_branch_without_switch_control()
    {
        var maui = MauiProject();
        var identity = File.ReadAllText(Path.Combine(maui, "Components", "Shared", "ShellOrganizationIdentity.razor"));
        var menu = File.ReadAllText(Path.Combine(maui, "Components", "Shared", "ShellAccountMenu.razor"));
        var workspace = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "WorkspaceSelect.razor"));
        var css = File.ReadAllText(Path.Combine(maui, "wwwroot", "app.css"));
        var en = File.ReadAllText(Path.Combine(maui, "Localization", "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(maui, "Localization", "PosResources.fil-PH.resx"));

        Assert.DoesNotContain("ShellBranchSwitcher", identity, StringComparison.Ordinal);
        Assert.DoesNotContain("pos-topbar__brand--switch", identity, StringComparison.Ordinal);
        Assert.Contains("pos-topbar__subtitle--visible", identity, StringComparison.Ordinal);
        Assert.Contains("WorkspaceSelect_SwitchMenu", menu, StringComparison.Ordinal);
        Assert.Contains("/workspace-select", menu, StringComparison.Ordinal);
        Assert.Contains("SelectWorkspaceAsync", workspace, StringComparison.Ordinal);
        Assert.Contains("pos-workspace-select__org-row", css, StringComparison.Ordinal);
        Assert.Contains("min-height: 2.75rem", css, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.CreateSale", File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Organization", "OrgSummary.razor")), StringComparison.Ordinal);
        foreach (var key in new[]
                 {
                     "WorkspaceSelect_Title",
                     "WorkspaceSelect_SwitchMenu",
                     "BranchContext_DeviceMismatch",
                     "BranchContext_ShiftOpen",
                     "Org_EnterPosRoleRequired"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Org_summary_and_checkout_show_current_branch_without_faking_selling_permission()
    {
        var maui = MauiProject();
        var org = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Organization", "OrgSummary.razor"));
        var checkout = File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Sales", "SaleCheckout.razor"));

        Assert.Contains("BranchContext_Current", org, StringComparison.Ordinal);
        Assert.Contains("WorkspaceSelect_SwitchMenu", org, StringComparison.Ordinal);
        Assert.Contains("Org_EnterPosRoleRequired", org, StringComparison.Ordinal);
        Assert.Contains("BranchContext_DeviceMismatch", org, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.CreateSale", org, StringComparison.Ordinal);
        Assert.DoesNotContain("ShellBranchSwitcher", org, StringComparison.Ordinal);
        Assert.Contains("pos-sell-branch", checkout, StringComparison.Ordinal);
        Assert.Contains("BranchContext_SellingAt", checkout, StringComparison.Ordinal);
    }

    [Fact]
    public void Header_handler_sends_selected_branch_not_only_device_branch()
    {
        var root = FindRepoRoot();
        var handler = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.ApiClient",
            "PosOrganizationHeaderHandler.cs"));
        Assert.Contains("AuthSessionBranchContext.GetSelectedBranchId", handler, StringComparison.Ordinal);
    }

    [Fact]
    public void Selected_branch_is_distinct_from_device_branch_and_persists()
    {
        var keys = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Auth",
            "AuthModels.cs"));
        Assert.Contains("SelectedBranchId", keys, StringComparison.Ordinal);
        Assert.Contains("GetSelectedBranchId", keys, StringComparison.Ordinal);
        Assert.Contains("DeviceMatchesSelected", keys, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Secure_session_store_round_trips_selected_branch()
    {
        var branchId = Guid.NewGuid();
        var tokens = new MemorySecureTokenStore();
        var store = new SecureSessionStore(tokens);
        var session = new AuthSession(
            Guid.NewGuid(),
            "Owner",
            "owner",
            "o@example.com",
            Guid.NewGuid(),
            "Store",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(8),
            true,
            "allowed",
            BranchId: Guid.NewGuid(),
            PosDeviceId: Guid.NewGuid(),
            SelectedBranchId: branchId);
        await store.SaveAsync(session, Guid.NewGuid().ToString("N"));
        var (loaded, _) = await store.LoadAsync();
        Assert.Equal(branchId, loaded?.SelectedBranchId);
    }

    private static string MauiProject() =>
        Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui");

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

        throw new InvalidOperationException("Could not locate repository root.");
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
}
