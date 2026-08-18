using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Platform;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class BranchOperationalContextGuardTests
{
    [Fact]
    public void Header_shows_branch_subtitle_and_opens_switcher_sheet()
    {
        var maui = MauiProject();
        var identity = File.ReadAllText(Path.Combine(maui, "Components", "Shared", "ShellOrganizationIdentity.razor"));
        var switcher = File.ReadAllText(Path.Combine(maui, "Components", "Shared", "ShellBranchSwitcher.razor"));
        var css = File.ReadAllText(Path.Combine(maui, "wwwroot", "app.css"));
        var en = File.ReadAllText(Path.Combine(maui, "Localization", "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(maui, "Localization", "PosResources.fil-PH.resx"));

        Assert.Contains("ShellBranchSwitcher", identity, StringComparison.Ordinal);
        Assert.Contains("pos-topbar__subtitle--visible", identity, StringComparison.Ordinal);
        Assert.Contains("SelectBranchAsync", switcher, StringComparison.Ordinal);
        Assert.Contains("min-height: 2.75rem", css, StringComparison.Ordinal);
        Assert.Contains("pos-branch-sheet", css, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.CreateSale", File.ReadAllText(Path.Combine(maui, "Components", "Pages", "Organization", "OrgSummary.razor")), StringComparison.Ordinal);
        Assert.DoesNotContain("CreateSale", switcher, StringComparison.Ordinal);
        foreach (var key in new[]
                 {
                     "BranchContext_SwitchTitle",
                     "BranchContext_Current",
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
        Assert.Contains("Org_EnterPosRoleRequired", org, StringComparison.Ordinal);
        Assert.Contains("BranchContext_DeviceMismatch", org, StringComparison.Ordinal);
        Assert.Contains("UtangCapability.CreateSale", org, StringComparison.Ordinal);
        Assert.Contains("OpenBranchSwitchAsync", org, StringComparison.Ordinal);
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
        Assert.Contains("pos.session.selectedBranchId", keys, StringComparison.Ordinal);

        var deviceId = Guid.NewGuid();
        var selectedId = Guid.NewGuid();
        var session = new AuthSession(
            Guid.NewGuid(),
            "Owner",
            "owner",
            "o@example.com",
            Guid.NewGuid(),
            "Kizy Store",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(1),
            true,
            "allowed",
            BranchId: deviceId,
            PosDeviceId: Guid.NewGuid(),
            SelectedBranchId: selectedId);

        Assert.Equal(selectedId, AuthSessionBranchContext.GetSelectedBranchId(session));
        Assert.False(AuthSessionBranchContext.DeviceMatchesSelected(session));
        Assert.True(AuthSessionBranchContext.DeviceMatchesSelected(session with { SelectedBranchId = deviceId }));
    }

    [Fact]
    public async Task Secure_session_store_round_trips_selected_branch()
    {
        var tokens = new MemoryTokenStore();
        var store = new SecureSessionStore(tokens);
        var selected = Guid.NewGuid();
        var session = new AuthSession(
            Guid.NewGuid(),
            "Owner",
            "owner",
            "o@example.com",
            Guid.NewGuid(),
            "Kizy Store",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddHours(1),
            true,
            "allowed",
            AccessToken: "token",
            BranchId: Guid.NewGuid(),
            PosDeviceId: Guid.NewGuid(),
            SelectedBranchId: selected);

        await store.SaveAsync(session, "marker");
        var loaded = await store.LoadAsync();
        Assert.Equal(selected, loaded.Session?.SelectedBranchId);
        Assert.Equal(session.BranchId, loaded.Session?.BranchId);
    }

    private static string MauiProject()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Products", "PinoyBusinessPOS",
                "ExItS.PinoyBusinessPOS.Maui");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate PinoyBusinessPOS.Maui project.");
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

        throw new InvalidOperationException("Could not locate repository root.");
    }

    private sealed class MemoryTokenStore : ExItS.PinoyBusinessPOS.Application.Abstractions.ISecureTokenStore
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
