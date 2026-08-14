using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class PosOfflineCapabilityPolicyTests
{
    private readonly PosOfflineCapabilityPolicy _policy = new();

    [Theory]
    [InlineData("/sales/new", PosConnectivityRequirement.Queueable)]
    [InlineData("/customers", PosConnectivityRequirement.Queueable)]
    [InlineData("/offline-pin", PosConnectivityRequirement.OfflineCapable)]
    [InlineData("/sales", PosConnectivityRequirement.OfflineCapable)]
    [InlineData("/more", PosConnectivityRequirement.OfflineCapable)]
    [InlineData("/catalog", PosConnectivityRequirement.OnlineRequired)]
    [InlineData("/catalog/import", PosConnectivityRequirement.OnlineRequired)]
    [InlineData("/permissions", PosConnectivityRequirement.OnlineRequired)]
    [InlineData("/organization-select", PosConnectivityRequirement.OnlineRequired)]
    [InlineData("/inventory", PosConnectivityRequirement.OnlineRequired)]
    [InlineData("/reports", PosConnectivityRequirement.OnlineRequired)]
    [InlineData("/purchasing/new", PosConnectivityRequirement.Queueable)]
    [InlineData("/suppliers/11111111-1111-1111-1111-111111111111/linked-products", PosConnectivityRequirement.OfflineCapable)]
    [InlineData("/suppliers/11111111-1111-1111-1111-111111111111/connected-catalog", PosConnectivityRequirement.OnlineRequired)]
    public void Important_routes_have_expected_classification(string route, PosConnectivityRequirement expected)
    {
        Assert.Equal(expected, _policy.GetRouteRequirement(route));
    }

    [Theory]
    [InlineData("/personal", PosConnectivityRequirement.OfflineCapable)]
    [InlineData("/personal/more", PosConnectivityRequirement.OfflineCapable)]
    [InlineData("/personal/settings", PosConnectivityRequirement.OfflineCapable)]
    [InlineData("/personal/profile", PosConnectivityRequirement.OfflineCapable)]
    [InlineData("/personal/utang/people", PosConnectivityRequirement.Queueable)]
    [InlineData("/personal/utang/lent", PosConnectivityRequirement.Queueable)]
    [InlineData("/personal/utang/borrowed", PosConnectivityRequirement.Queueable)]
    [InlineData("/personal/utang/relationships/11111111-1111-1111-1111-111111111111", PosConnectivityRequirement.Queueable)]
    [InlineData("/personal/utang/invitations", PosConnectivityRequirement.OnlineRequired)]
    [InlineData("/personal/explore-pos", PosConnectivityRequirement.OnlineRequired)]
    [InlineData("/start-business", PosConnectivityRequirement.OnlineRequired)]
    [InlineData("/personal/resolve-user", PosConnectivityRequirement.OnlineRequired)]
    [InlineData("/personal/my-qr", PosConnectivityRequirement.OnlineRequired)]
    public void Personal_routes_have_expected_classification(string route, PosConnectivityRequirement expected)
    {
        Assert.Equal(expected, _policy.GetRouteRequirement(route));
    }

    [Fact]
    public void Personal_actions_are_classified()
    {
        Assert.Equal(
            PosConnectivityRequirement.OnlineRequired,
            _policy.GetActionRequirement(PosOfflineActionKeys.PersonalInvite));
        Assert.Equal(
            PosConnectivityRequirement.OnlineRequired,
            _policy.GetActionRequirement(PosOfflineActionKeys.PersonalLinkUser));
        Assert.Equal(
            PosConnectivityRequirement.OnlineRequired,
            _policy.GetActionRequirement(PosOfflineActionKeys.PersonalStartBusiness));
        Assert.Equal(
            PosConnectivityRequirement.Queueable,
            _policy.GetActionRequirement(PosOfflineActionKeys.PersonalContactCreate));
        Assert.Equal(
            PosConnectivityRequirement.Queueable,
            _policy.GetActionRequirement(PosOfflineActionKeys.PersonalEntryRecord));
    }

    [Fact]
    public void Nested_online_required_routes_inherit_prefix()
    {
        Assert.Equal(PosConnectivityRequirement.OnlineRequired, _policy.GetRouteRequirement("/inventory/adjust"));
        Assert.Equal(PosConnectivityRequirement.OnlineRequired, _policy.GetRouteRequirement("/permissions/assignments/new"));
        Assert.Equal(PosConnectivityRequirement.Queueable, _policy.GetRouteRequirement("/customers/new"));
        Assert.Equal(PosConnectivityRequirement.OfflineCapable, _policy.GetRouteRequirement("/sales/local/abc/receipt"));
        // Mixed-tree overrides: server history / reporting under otherwise offline/queueable parents.
        Assert.Equal(
            PosConnectivityRequirement.OnlineRequired,
            _policy.GetRouteRequirement("/sales/11111111-1111-1111-1111-111111111111"));
        Assert.Equal(
            PosConnectivityRequirement.OnlineRequired,
            _policy.GetRouteRequirement("/sales/11111111-1111-1111-1111-111111111111/receipt"));
        Assert.Equal(
            PosConnectivityRequirement.OnlineRequired,
            _policy.GetRouteRequirement("/customers/11111111-1111-1111-1111-111111111111/ledger"));
        Assert.Equal(
            PosConnectivityRequirement.OnlineRequired,
            _policy.GetRouteRequirement("/customers/11111111-1111-1111-1111-111111111111/statement"));
    }

    [Fact]
    public void Unclassified_operational_route_fails_closed_as_online_required()
    {
        Assert.Equal(
            PosConnectivityRequirement.OnlineRequired,
            _policy.GetRouteRequirement("/future-unknown-ops-surface"));
    }

    [Fact]
    public void Important_routes_and_actions_are_explicitly_classified()
    {
        Assert.True(_policy.ImportantRoutes.Count >= 30);
        Assert.True(_policy.ImportantActions.Count >= 15);
        Assert.Contains(PosOfflineActionKeys.SwitchOrganization, _policy.ImportantActions.Keys);
        Assert.Equal(
            PosConnectivityRequirement.OnlineRequired,
            _policy.GetActionRequirement(PosOfflineActionKeys.SwitchOrganization));
        Assert.Equal(
            PosConnectivityRequirement.Queueable,
            _policy.GetActionRequirement("sale.checkout.cash"));
        Assert.Equal(PosConnectivityRequirement.OfflineCapable,
            _policy.GetActionRequirement(PosOfflineActionKeys.ConnectedSupplierLinkedProductsView));
        Assert.Equal(PosConnectivityRequirement.OnlineRequired,
            _policy.GetActionRequirement(PosOfflineActionKeys.ConnectedSupplierCatalogSearch));
        Assert.Equal(PosConnectivityRequirement.Queueable,
            _policy.GetActionRequirement(PosOfflineActionKeys.ConnectedSupplierDraftSave));
        Assert.Equal(PosConnectivityRequirement.OnlineRequired,
            _policy.GetActionRequirement(PosOfflineActionKeys.ConnectedSupplierOrderSubmit));
    }

    [Fact]
    public async Task Online_required_route_shows_shared_dialog_and_does_not_imply_reconnect()
    {
        var connectivity = new FakeConnectivity(false);
        var guard = new OnlineRequiredGuard(connectivity, _policy);

        Assert.False(await guard.EnsureOnlineForRouteAsync("/catalog"));
        Assert.True(guard.IsDialogVisible);
        Assert.Equal("OnlineRequired_Title", guard.DialogTitleKey);
        Assert.Equal("OnlineRequired_Message", guard.DialogMessageKey);

        // OfflineCapable route does not open the dialog.
        await guard.DismissAsync();
        Assert.True(await guard.EnsureOnlineForRouteAsync("/sales/new"));
        Assert.False(guard.IsDialogVisible);
    }

    [Fact]
    public async Task Org_switch_uses_special_message_and_preserves_offline_session_semantics()
    {
        var connectivity = new FakeConnectivity(false);
        var guard = new OnlineRequiredGuard(connectivity, _policy);

        Assert.False(await guard.EnsureOnlineForActionAsync(
            PosOfflineActionKeys.SwitchOrganization,
            "ABC Sari-Sari Store"));
        Assert.True(guard.IsDialogVisible);
        Assert.Equal("OnlineRequired_OrgSwitchMessage", guard.DialogMessageKey);
        Assert.Equal("ABC Sari-Sari Store", guard.DialogMessageArg);
        Assert.DoesNotContain("reconnect", guard.DialogMessageKey, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Retry_connection_dismisses_dialog_when_back_online()
    {
        var connectivity = new FakeConnectivity(false);
        var guard = new OnlineRequiredGuard(connectivity, _policy);
        Assert.False(await guard.EnsureOnlineAsync());
        Assert.True(guard.IsDialogVisible);

        connectivity.Connected = true;
        Assert.True(await guard.RetryConnectionAsync());
        Assert.False(guard.IsDialogVisible);
    }

    [Fact]
    public void Navigation_and_menu_wiring_uses_central_policy_helpers()
    {
        var more = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "MoreHub.razor"));
        var menu = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "ShellAccountMenu.razor"));
        var switcher = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "AccountContextSwitcher.razor"));
        var shell = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Layout", "PosShell.razor"));
        var salesList = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Sales", "SalesList.razor"));
        var dialog = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "OnlineRequiredDialogHost.razor"));
        var mauiProgram = File.ReadAllText(Path.Combine(MauiProject(), "MauiProgram.cs"));

        Assert.Contains("OfflineAwareNavigation", more, StringComparison.Ordinal);
        Assert.Contains("OfflineNav.NavigateAsync", more, StringComparison.Ordinal);
        Assert.Contains("PosOfflineActionKeys.SwitchOrganization", menu, StringComparison.Ordinal);
        Assert.Contains("PosOfflineActionKeys.SwitchOrganization", switcher, StringComparison.Ordinal);
        Assert.Contains("GoCatalogAsync", shell, StringComparison.Ordinal);
        Assert.Contains("OfflineNav.NavigateAsync(\"/catalog\")", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("disabled=\"@(!canCreate || _isOffline)\"", salesList, StringComparison.Ordinal);

        var customerDetail = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Customers", "CustomerDetail.razor"));
        Assert.Contains("PosOfflineActionKeys.CustomerLedger", customerDetail, StringComparison.Ordinal);
        Assert.Contains("PosOfflineActionKeys.CustomerStatement", customerDetail, StringComparison.Ordinal);
        Assert.Contains("PosOfflineActionKeys.CustomerOverdue", customerDetail, StringComparison.Ordinal);
        Assert.Contains("SyncStatus_RetryConnection", dialog, StringComparison.Ordinal);
        Assert.DoesNotContain("/reconnect", dialog, StringComparison.Ordinal);
        Assert.Contains("IPosOfflineCapabilityPolicy", mauiProgram, StringComparison.Ordinal);
        Assert.Contains("AddScoped<OfflineAwareNavigation>", mauiProgram, StringComparison.Ordinal);
        Assert.Contains("IOfflineReconnectAutoSync", mauiProgram, StringComparison.Ordinal);
        Assert.Contains("OfflineReconnectAutoSyncService", mauiProgram, StringComparison.Ordinal);
    }

    [Fact]
    public void Important_operational_page_files_exist_for_classified_areas()
    {
        // Future-safe: classified operational areas must keep a page entry point on disk.
        string[] requiredFiles =
        [
            Path.Combine("Components", "Pages", "Sales", "SaleCheckout.razor"),
            Path.Combine("Components", "Pages", "Catalog", "CatalogImport.razor"),
            Path.Combine("Components", "Pages", "Permissions", "PermissionsHub.razor"),
            Path.Combine("Components", "Pages", "OrganizationSelect.razor"),
            Path.Combine("Components", "Pages", "MoreHub.razor"),
            Path.Combine("Components", "Pages", "Inventory", "InventoryList.razor"),
            Path.Combine("Components", "Pages", "Reporting", "ReportsHub.razor"),
        ];

        foreach (var relative in requiredFiles)
        {
            Assert.True(File.Exists(Path.Combine(MauiProject(), relative)), relative);
        }
    }

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
}
