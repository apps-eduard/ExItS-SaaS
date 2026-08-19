using System.Xml.Linq;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

/// <summary>
/// Static regression guards for context-aware Home routing: Organization Owners without POS access
/// must land on the Organization overview instead of a POS dashboard or the commercial denied page.
/// </summary>
public sealed class HomeDestinationRoutingGuardTests
{
    [Fact]
    public void Role_home_resolver_gates_on_pos_access_before_any_dashboard()
    {
        var resolver = File.ReadAllText(Path.Combine(
            ApplicationProject(), "Auth", "RoleRoutingServices.cs"));

        Assert.Contains("if (!currentUser.HasPosAccess)", resolver, StringComparison.Ordinal);
        Assert.Contains("IsRevokedAssignmentStatus", resolver, StringComparison.Ordinal);

        // The POS-access gate must run before the effective-role lookup and before any role home.
        var accessGate = resolver.IndexOf("if (!currentUser.HasPosAccess)", StringComparison.Ordinal);
        var roleLookup = resolver.IndexOf("permissions.GetEffectiveAsync", StringComparison.Ordinal);
        Assert.InRange(accessGate, 0, roleLookup);
    }

    [Fact]
    public void Navigation_gate_keeps_org_essentials_for_organization_context_without_pos_access()
    {
        var gate = File.ReadAllText(Path.Combine(MauiProject(), "Services", "NavigationGate.cs"));

        Assert.Contains("if (!currentUser.HasPosAccess)", gate, StringComparison.Ordinal);
        Assert.Contains("RoleHomeResolver.OrgEssentials", gate, StringComparison.Ordinal);
        Assert.Contains("RoleHomeResolver.PersonalHome", gate, StringComparison.Ordinal);
        Assert.DoesNotContain("RoleHomeResolver.AccessDenied", gate, StringComparison.Ordinal);
    }

    [Fact]
    public void Personal_default_routing_ignores_stale_organization_context()
    {
        var gate = File.ReadAllText(Path.Combine(MauiProject(), "Services", "NavigationGate.cs"));
        var resolver = File.ReadAllText(Path.Combine(
            ApplicationProject(), "Auth", "RoleRoutingServices.cs"));
        var auth = File.ReadAllText(Path.Combine(ApplicationProject(), "Auth", "AuthenticationService.cs"));
        var models = File.ReadAllText(Path.Combine(ApplicationProject(), "Auth", "AuthModels.cs"));

        Assert.Contains("class AuthSessionWorkspace", models, StringComparison.Ordinal);
        Assert.Contains("IsPersonalDefault", models, StringComparison.Ordinal);
        Assert.Contains("AuthSessionWorkspace.IsPersonalDefault", gate, StringComparison.Ordinal);
        Assert.Contains("AuthSessionWorkspace.IsPersonalDefault", resolver, StringComparison.Ordinal);
        Assert.Contains("NormalizePersonalDefaultSessionAsync", auth, StringComparison.Ordinal);
        Assert.Contains("ClearOrganizationPreferenceAsync", auth, StringComparison.Ordinal);

        var sessionStore = File.ReadAllText(Path.Combine(ApplicationProject(), "Auth", "SecureSessionStore.cs"));
        Assert.Contains("SecureTokenKeys.AccountClass", sessionStore, StringComparison.Ordinal);
        Assert.Contains("SecureTokenKeys.OrganizationContextLocked", sessionStore, StringComparison.Ordinal);
        Assert.Contains("SecureTokenKeys.BranchId", sessionStore, StringComparison.Ordinal);
        Assert.Contains("SecureTokenKeys.PosDeviceId", sessionStore, StringComparison.Ordinal);

        var mauiStore = File.ReadAllText(Path.Combine(MauiProject(), "Services", "MauiSecureTokenStore.cs"));
        Assert.Contains("SecureTokenKeys.AccountClass", mauiStore, StringComparison.Ordinal);
        Assert.Contains("SecureTokenKeys.OrganizationContextLocked", mauiStore, StringComparison.Ordinal);
        Assert.Contains("SecureTokenKeys.BranchId", mauiStore, StringComparison.Ordinal);
        Assert.Contains("SecureTokenKeys.PosDeviceId", mauiStore, StringComparison.Ordinal);

        // Personal default must be evaluated before the org-essentials / POS-access branch.
        var personalGate = gate.IndexOf("AuthSessionWorkspace.IsPersonalDefault", StringComparison.Ordinal);
        var orgEssentials = gate.IndexOf("RoleHomeResolver.OrgEssentials", StringComparison.Ordinal);
        Assert.InRange(personalGate, 0, orgEssentials);
    }

    [Fact]
    public void Pos_shell_home_tab_uses_centralized_resolver_and_reacts_to_access_changes()
    {
        var shell = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Layout", "PosShell.razor"));

        Assert.Contains("href=\"@_homeHref\"", shell, StringComparison.Ordinal);
        Assert.Contains("RoleHome.ResolvePosHomeAsync()", shell, StringComparison.Ordinal);
        Assert.Contains("RefreshHomeDestinationAsync", shell, StringComparison.Ordinal);
        Assert.Contains("CurrentUser.Changed += OnAccessChangedAsync", shell, StringComparison.Ordinal);
        Assert.Contains("CurrentUser.Changed -= OnAccessChangedAsync", shell, StringComparison.Ordinal);
        Assert.Contains("SellingMode.Changed += OnAccessChangedAsync", shell, StringComparison.Ordinal);

        // Home must never be hard-coded to a POS dashboard route.
        Assert.DoesNotContain("href=\"/owner\"", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/manager\"", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/cashier\"", shell, StringComparison.Ordinal);
    }

    [Fact]
    public void Pos_shell_hides_pos_tabs_when_pos_access_is_unavailable()
    {
        var shell = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Layout", "PosShell.razor"));

        Assert.Contains("@if (_hasPosAccess)", shell, StringComparison.Ordinal);
        Assert.Contains("_hasPosAccess = CurrentUser.HasPosAccess", shell, StringComparison.Ordinal);

        // Products / Sales / Customers only exist inside the POS-access branch.
        var posTabs = shell[shell.IndexOf("@if (_hasPosAccess)", StringComparison.Ordinal)..];
        var afterBranch = posTabs[(posTabs.IndexOf("}", posTabs.IndexOf("href=\"/customers\"", StringComparison.Ordinal), StringComparison.Ordinal))..];
        Assert.DoesNotContain("href=\"/catalog\"", afterBranch, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/sales\"", afterBranch, StringComparison.Ordinal);
        Assert.DoesNotContain("href=\"/customers\"", afterBranch, StringComparison.Ordinal);

        // Home already resolves to the organization overview, so no tab may repeat that destination.
        Assert.DoesNotContain("href=\"/org\"", shell, StringComparison.Ordinal);
        Assert.Contains("href=\"/more\"", shell, StringComparison.Ordinal);
    }

    [Fact]
    public void More_hub_stays_reachable_without_pos_access()
    {
        var more = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "MoreHub.razor"));

        // The More tab must not bounce back to Home when only POS access is missing.
        Assert.Contains("if (!CurrentUser.HasPosAccess)", more, StringComparison.Ordinal);
        Assert.Contains("@if (CurrentUser.HasPosAccess)", more, StringComparison.Ordinal);
        Assert.Contains("CurrentUser.Session?.OrganizationId is not null && CurrentUser.HasPosAccess", more, StringComparison.Ordinal);
        Assert.Contains("AccountContextSwitcher", more, StringComparison.Ordinal);
        Assert.Contains("Org_SummaryTitle", more, StringComparison.Ordinal);

        var accessGate = more.IndexOf("if (!CurrentUser.HasPosAccess)", StringComparison.Ordinal);
        var shellGate = more.IndexOf("if (!Gate.CanEnterProtectedShell)", StringComparison.Ordinal);
        Assert.InRange(accessGate, 0, shellGate);
    }

    [Theory]
    [InlineData("OwnerDashboard.razor")]
    [InlineData("ManagerDashboard.razor")]
    [InlineData("CashierHome.razor")]
    public void Role_dashboards_require_pos_access_before_trusting_working_as(string page)
    {
        var text = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Pages", "Dashboards", page));

        Assert.Contains("if (!CurrentUser.HasPosAccess)", text, StringComparison.Ordinal);
        Assert.Contains("RoleHomeResolver.OrgEssentials", text, StringComparison.Ordinal);

        // Organization Owner working-as must never be honored ahead of the POS-access check.
        var accessGate = text.IndexOf("if (!CurrentUser.HasPosAccess)", StringComparison.Ordinal);
        var preferred = text.IndexOf("SellingMode.PreferredHomeRoute", StringComparison.Ordinal);
        Assert.InRange(accessGate, 0, preferred);
    }

    [Fact]
    public void Cashier_home_uses_icon_action_tiles()
    {
        var text = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Pages", "Dashboards", "CashierHome.razor"));

        Assert.Contains("pos-cashier", text, StringComparison.Ordinal);
        Assert.Contains("pos-dash__header", text, StringComparison.Ordinal);
        Assert.Contains("pos-dash__title", text, StringComparison.Ordinal);
        Assert.Contains("pos-action-grid--three", text, StringComparison.Ordinal);
        Assert.Contains("pos-action-tile--primary", text, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"plus\")", text, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"refresh\")", text, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"close\")", text, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"sales\")", text, StringComparison.Ordinal);
        Assert.Contains("Cashier_ShiftIdLabel", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Auth_Logout", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Access_denied_offers_organization_overview_without_a_navigation_loop()
    {
        var denied = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "AccessDenied.razor"));

        Assert.Contains("@page \"/access-denied\"", denied, StringComparison.Ordinal);
        Assert.Contains("Org_OverviewLink", denied, StringComparison.Ordinal);
        Assert.Contains("RoleHomeResolver.OrgEssentials", denied, StringComparison.Ordinal);
        Assert.Contains("OrgSelect_Title", denied, StringComparison.Ordinal);
        Assert.Contains("Auth_Logout", denied, StringComparison.Ordinal);
        Assert.Contains("CurrentUser.Session?.OrganizationId is not null", denied, StringComparison.Ordinal);

        // Org overview must never bounce back to the denied page.
        var orgSummary = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Pages", "Organization", "OrgSummary.razor"));
        Assert.DoesNotContain("/access-denied", orgSummary, StringComparison.Ordinal);
        Assert.DoesNotContain("RoleHomeResolver.AccessDenied", orgSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void Org_summary_explains_missing_pos_role_and_refreshes_state_after_enabling_access()
    {
        var orgSummary = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Pages", "Organization", "OrgSummary.razor"));

        Assert.Contains("Org_EnablePos", orgSummary, StringComparison.Ordinal);
        Assert.Contains("Org_PosRoleRequired", orgSummary, StringComparison.Ordinal);
        Assert.Contains("Permissions.GetEffectiveAsync", orgSummary, StringComparison.Ordinal);
        Assert.Contains("LoadAccessStateAsync", orgSummary, StringComparison.Ordinal);
        Assert.Contains("Gate.ResolveStartRouteAsync", orgSummary, StringComparison.Ordinal);

        // Duplicate activation guard, and no optimistic POS-access flag before server confirmation.
        Assert.Contains("_enablingPos || _enteringPos", orgSummary, StringComparison.Ordinal);
        Assert.Contains("if (!CurrentUser.HasPosAccess)", orgSummary, StringComparison.Ordinal);
    }

    [Fact]
    public void Selecting_organization_aligns_platform_session_org_context()
    {
        var auth = File.ReadAllText(Path.Combine(ApplicationProject(), "Auth", "AuthenticationService.cs"));
        var orgAuthz = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Platform",
            "ExItS.Platform.Api",
            "Common",
            "PlatformOrganizationAuthz.cs"));

        Assert.Contains("AlignPlatformOrganizationContextAsync", auth, StringComparison.Ordinal);
        Assert.Contains("SetOrganizationContextAsync", auth, StringComparison.Ordinal);
        Assert.Contains("PersistOrganizationSelectionAsync", auth, StringComparison.Ordinal);

        // View org details must not require SelectedOrganizationId (Mobile Org Summary).
        Assert.Contains("requireSelectedOrganization: false", orgAuthz, StringComparison.Ordinal);
        Assert.Contains("EnsureCanViewOrganizationAsync", orgAuthz, StringComparison.Ordinal);
    }

    [Fact]
    public void Context_switcher_clears_working_as_when_bound_organization_has_no_pos_access()
    {
        var switcher = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Shared", "AccountContextSwitcher.razor"));

        Assert.Contains("result.Session is not { HasPosAccess: true }", switcher, StringComparison.Ordinal);
        Assert.Contains("SellingMode.Clear();", switcher, StringComparison.Ordinal);
        Assert.Contains("RoleHomeResolver.OrgEssentials", switcher, StringComparison.Ordinal);
    }

    [Fact]
    public void Org_workspace_routing_uses_workspace_selection_and_personal_switch_goes_to_select()
    {
        var signIn = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "SignIn.razor"));
        var workspaceSelect = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Pages", "WorkspaceSelect.razor"));
        var orgSelect = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Pages", "OrganizationSelect.razor"));
        var switcher = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Shared", "AccountContextSwitcher.razor"));
        var menu = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Shared", "ShellAccountMenu.razor"));

        Assert.Contains("NavigateAfterSignInAsync", signIn, StringComparison.Ordinal);
        Assert.Contains("if (orgs.Count == 0)", signIn, StringComparison.Ordinal);
        Assert.Contains("ResolveRoutingPlanAsync", signIn, StringComparison.Ordinal);
        Assert.Contains("WorkspaceRoutingOutcome.AutoSelect", signIn, StringComparison.Ordinal);
        Assert.Contains("Nav.NavigateTo(\"/workspace-select\", replace: true)", signIn, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectOrganizationAsync(orgs[0].OrganizationId", signIn, StringComparison.Ordinal);

        Assert.Contains("/workspace-select", orgSelect, StringComparison.Ordinal);
        Assert.Contains("WorkspaceRoutingOutcome.AutoSelect", workspaceSelect, StringComparison.Ordinal);
        Assert.Contains("SelectWorkspaceAsync", workspaceSelect, StringComparison.Ordinal);

        Assert.Contains("if (IsPersonalActive)", switcher, StringComparison.Ordinal);
        Assert.Contains("Nav.NavigateTo(\"/workspace-select\", replace: true)", switcher, StringComparison.Ordinal);

        Assert.Contains("WorkspaceSelect_SwitchMenu", menu, StringComparison.Ordinal);
        Assert.Contains("/workspace-select", menu, StringComparison.Ordinal);
    }

    [Fact]
    public void Home_routing_localization_is_complete_in_both_cultures()
    {
        string[] keys = ["Org_OverviewLink", "Org_PosRoleRequired", "Org_SummaryTitle", "Access_RoleMissing"];

        foreach (var file in new[] { "PosResources.resx", "PosResources.fil-PH.resx" })
        {
            var doc = XDocument.Load(Path.Combine(MauiProject(), "Localization", file));
            var names = doc.Root!.Elements("data")
                .Select(d => d.Attribute("name")?.Value)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var key in keys)
            {
                Assert.Contains(key, names);
            }
        }
    }

    [Fact]
    public void Bind_denial_inference_maps_embedded_entitlement_stale_not_assignment_missing()
    {
        var auth = File.ReadAllText(Path.Combine(ApplicationProject(), "Auth", "AuthenticationService.cs"));

        Assert.Contains("KnownProductEntryReasonCodes", auth, StringComparison.Ordinal);
        Assert.Contains("\"entitlement_stale\"", auth, StringComparison.Ordinal);

        var infer = auth.IndexOf("InferDeniedReasonCode", StringComparison.Ordinal);
        var stale = auth.IndexOf("\"entitlement_stale\"", infer, StringComparison.Ordinal);
        var fallback = auth.IndexOf("return \"product_assignment_missing\"", infer, StringComparison.Ordinal);
        Assert.InRange(stale, 0, fallback);
    }

    private static string MauiProject() => Path.Combine(
        FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui");

    private static string ApplicationProject() => Path.Combine(
        FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Application");

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
}
