namespace ExItS.PinoyBusinessPOS.Maui.Tests;

/// <summary>
/// Guards for contextual top-bar identity (no env badge / static brand in authenticated shells).
/// </summary>
public sealed class ShellContextIdentityGuardTests
{
    [Fact]
    public void Authenticated_shells_use_session_identity_without_env_badge_or_static_brand()
    {
        var layout = Path.Combine(MauiProject(), "Components", "Layout");
        var personal = File.ReadAllText(Path.Combine(layout, "PersonalShell.razor"));
        var pos = File.ReadAllText(Path.Combine(layout, "PosShell.razor"));
        var identity = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "ShellContextIdentity.razor"));

        Assert.Contains("StoreHeader", personal, StringComparison.Ordinal);
        Assert.Contains("ShellContextIdentity", File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "StoreHeader.razor")), StringComparison.Ordinal);
        Assert.Contains("UseOrganizationContext=\"false\"", File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "StoreHeader.razor")), StringComparison.Ordinal);
        Assert.DoesNotContain("Brand_Name", personal, StringComparison.Ordinal);
        Assert.DoesNotContain("Env_Development", personal, StringComparison.Ordinal);
        Assert.DoesNotContain("Badge", personal, StringComparison.Ordinal);
        Assert.DoesNotContain("IAppInfoService", personal, StringComparison.Ordinal);

        Assert.Contains("StoreHeader", pos, StringComparison.Ordinal);
        Assert.Contains("ShellOrganizationIdentity", File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "StoreHeader.razor")), StringComparison.Ordinal);
        Assert.Contains("ShellAccountMenu", File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "StoreHeader.razor")), StringComparison.Ordinal);
        Assert.DoesNotContain("ShellUserIdentity", pos, StringComparison.Ordinal);
        Assert.Contains("IconName=\"menu\"", File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "ShellAccountMenu.razor")), StringComparison.Ordinal);
        var accountMenu = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "ShellAccountMenu.razor"));
        Assert.Contains("ShowOrganizationSwitcherItem", accountMenu, StringComparison.Ordinal);
        Assert.Contains("ShowDashboardItem", accountMenu, StringComparison.Ordinal);
        Assert.Contains("Shell_ChangeOrganization", accountMenu, StringComparison.Ordinal);
        Assert.Contains("GoDashboardAsync", accountMenu, StringComparison.Ordinal);
        Assert.Contains("IsOnOrganizationSelect", accountMenu, StringComparison.Ordinal);

        var syncStatus = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "ShellSyncStatus.razor"));
        Assert.Contains("InvokeAsync(StateHasChanged)", syncStatus, StringComparison.Ordinal);
        Assert.Contains("OnPanelOpened.HasDelegate", syncStatus, StringComparison.Ordinal);

        var header = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "StoreHeader.razor"));
        Assert.Contains("_accountMenu?.CloseAsync()", header, StringComparison.Ordinal);
        Assert.Contains("pos-topbar__back", header, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"back\")", header, StringComparison.Ordinal);

        var headerBack = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "StoreHeaderBack.razor"));
        Assert.Contains("HeaderState.SetBack", headerBack, StringComparison.Ordinal);
        Assert.Contains("HeaderState.ReleaseBack", headerBack, StringComparison.Ordinal);

        var headerState = File.ReadAllText(Path.Combine(MauiProject(), "Services", "StoreHeaderState.cs"));
        Assert.Contains("void ReleaseBack(", headerState, StringComparison.Ordinal);
        Assert.Contains("int SetBack(", headerState, StringComparison.Ordinal);
        Assert.Contains("_activeClaim", headerState, StringComparison.Ordinal);

        var posShell = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Layout", "PosShell.razor"));
        Assert.DoesNotContain("HeaderState.Reset()", posShell, StringComparison.Ordinal);
        Assert.Contains("_firstTimeSetupIncomplete == false", posShell, StringComparison.Ordinal);

        var personalShell = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Layout", "PersonalShell.razor"));
        Assert.DoesNotContain("HeaderState.Reset()", personalShell, StringComparison.Ordinal);

        var authShell = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Layout", "AuthShell.razor"));
        Assert.DoesNotContain("HeaderState.Reset()", authShell, StringComparison.Ordinal);
        Assert.DoesNotContain("Brand_Name", pos, StringComparison.Ordinal);
        Assert.DoesNotContain("Env_Development", pos, StringComparison.Ordinal);
        Assert.DoesNotContain("Badge Tone", pos, StringComparison.Ordinal);
        Assert.DoesNotContain("IAppInfoService", pos, StringComparison.Ordinal);

        Assert.Contains("CurrentUser.Changed", identity, StringComparison.Ordinal);
        Assert.Contains("Session?.DisplayName", identity, StringComparison.Ordinal);
        Assert.Contains("OrganizationDisplayName", identity, StringComparison.Ordinal);
        Assert.Contains("Avatar", identity, StringComparison.Ordinal);
        Assert.Contains("GetOrganizationAsync", identity, StringComparison.Ordinal);
        Assert.Contains("Branding?.LogoUrl", identity, StringComparison.Ordinal);
        Assert.Contains("Shell_UserFallback", identity, StringComparison.Ordinal);
        Assert.Contains("Shell_OrganizationFallback", identity, StringComparison.Ordinal);
        Assert.Contains("AvatarShape", identity, StringComparison.Ordinal);
        var css = File.ReadAllText(Path.Combine(MauiProject(), "wwwroot", "app.css"));
        Assert.Contains("text-overflow", css, StringComparison.Ordinal);
        Assert.Contains("pos-topbar__user", css, StringComparison.Ordinal);
        Assert.Contains("pos-topbar__subtitle--visible", css, StringComparison.Ordinal);
        Assert.Contains("pos-topbar__overflow", css, StringComparison.Ordinal);

        var orgIdentity = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "ShellOrganizationIdentity.razor"));
        var userIdentity = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "ShellUserIdentity.razor"));
        Assert.Contains("_logoInFlight", identity, StringComparison.Ordinal);
        Assert.Contains("_logoInFlight", orgIdentity, StringComparison.Ordinal);
        Assert.Contains("_roleResolved", userIdentity, StringComparison.Ordinal);
        Assert.Contains("AvatarShape.SoftSquare", orgIdentity, StringComparison.Ordinal);
        Assert.Contains("AvatarShape.Circle", userIdentity, StringComparison.Ordinal);
        Assert.Contains("OrgSelect_EnterOwner", userIdentity, StringComparison.Ordinal);
    }

    [Fact]
    public void Auth_and_personal_shells_use_account_menu_overflow()
    {
        var auth = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Layout", "AuthShell.razor"));
        var header = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "StoreHeader.razor"));
        Assert.Contains("Brand_Name", header, StringComparison.Ordinal);
        Assert.Contains("IsAuthenticated", auth, StringComparison.Ordinal);
        Assert.Contains("StoreHeader", auth, StringComparison.Ordinal);
        Assert.Contains("ShellOrganizationIdentity", header, StringComparison.Ordinal);
        Assert.Contains("ShellAccountMenu", header, StringComparison.Ordinal);
        Assert.DoesNotContain("ShellUserIdentity", auth, StringComparison.Ordinal);
        Assert.Contains("AuthShellIdentityState", auth, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderIdentity.Personal", auth, StringComparison.Ordinal);
        Assert.Contains("HasOrganizationIdentity", auth, StringComparison.Ordinal);
        Assert.DoesNotContain("Env_Development", auth, StringComparison.Ordinal);
        Assert.DoesNotContain("Badge Tone", auth, StringComparison.Ordinal);
        Assert.DoesNotContain("IAppInfoService", auth, StringComparison.Ordinal);

        var personal = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Layout", "PersonalShell.razor"));
        Assert.Contains("StoreHeader", personal, StringComparison.Ordinal);
        Assert.Contains("ShellAccountMenu", header, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderIdentity.Personal", personal, StringComparison.Ordinal);
    }

    [Fact]
    public void Organization_select_uses_dual_top_bar_preview_with_role_and_staff_headers()
    {
        var orgSelect = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "OrganizationSelect.razor"));

        // Dual top-bar preview remains the org identity surface.
        Assert.Contains("AuthShellIdentityState", orgSelect, StringComparison.Ordinal);
        Assert.Contains("SetOrganizationPreview", orgSelect, StringComparison.Ordinal);
        Assert.Contains("AvatarShape.SoftSquare", orgSelect, StringComparison.Ordinal);
        Assert.Contains("FriendlyMembershipRole", orgSelect, StringComparison.Ordinal);
        Assert.Contains("OrgSelect_EnterOwner", orgSelect, StringComparison.Ordinal);
        Assert.Contains("SelectOrganizationAsync", orgSelect, StringComparison.Ordinal);
        Assert.Contains("Org_StaffUnknownName", orgSelect, StringComparison.Ordinal);
        Assert.DoesNotContain("pos-org-role__row--personal", orgSelect, StringComparison.Ordinal);
        Assert.DoesNotContain("Auth_Logout", orgSelect, StringComparison.Ordinal);
        Assert.Contains("pos-org-select-empty", orgSelect, StringComparison.Ordinal);

        // Accepted org-select polish keeps densified section headers for owner role bind and staff list.
        Assert.DoesNotContain("PageHeader", orgSelect, StringComparison.Ordinal);
        Assert.Contains("pos-org-role__header", orgSelect, StringComparison.Ordinal);
        Assert.Contains("OrgSelect_RoleTitle", orgSelect, StringComparison.Ordinal);
        Assert.Contains("OrgSelect_Title", orgSelect, StringComparison.Ordinal);
        Assert.Contains("OrgSelect_SubtitleStaff", orgSelect, StringComparison.Ordinal);
    }

    [Fact]
    public void Personal_home_starts_with_utang_summary_not_page_header()
    {
        var home = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Personal", "PersonalHome.razor"));
        Assert.Contains("Personal_DashboardSection", home, StringComparison.Ordinal);
        Assert.Contains("Personal_RecentActivitySection", home, StringComparison.Ordinal);
        Assert.Contains("pos-personal-home__title", home, StringComparison.Ordinal);
        Assert.Contains("Personal_HomeTitle", home, StringComparison.Ordinal);
        // Ant Design PageHeader is forbidden; native personal-home header is allowed.
        Assert.DoesNotContain("PageHeader", home, StringComparison.Ordinal);

        var en = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));
        Assert.Contains("Personal Utang summary", en, StringComparison.Ordinal);
        Assert.Contains("name=\"Shell_UserAria\"", en, StringComparison.Ordinal);
        Assert.Contains("name=\"Shell_OrganizationAria\"", en, StringComparison.Ordinal);
    }

    [Fact]
    public void Role_homes_and_org_summary_do_not_duplicate_organization_name_under_top_bar()
    {
        var dashboards = Path.Combine(MauiProject(), "Components", "Pages", "Dashboards");
        foreach (var file in new[] { "OwnerDashboard.razor", "ManagerDashboard.razor", "CashierHome.razor" })
        {
            var text = File.ReadAllText(Path.Combine(dashboards, file));
            Assert.DoesNotContain("OrganizationDisplayName", text, StringComparison.Ordinal);
        }

        var org = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Organization", "OrgSummary.razor"));
        Assert.Contains("Org_SummaryTitle", org, StringComparison.Ordinal);
        Assert.DoesNotContain("Subtitle=\"@(_org?.DisplayName", org, StringComparison.Ordinal);
    }

    [Fact]
    public void Avatar_falls_back_safely_when_image_fails()
    {
        var avatar = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Shared", "ExItS.DesignSystem",
            "Components", "Primitives", "Avatar.razor"));
        Assert.Contains("onerror", avatar, StringComparison.Ordinal);
        Assert.Contains("_imageFailed", avatar, StringComparison.Ordinal);
        Assert.Contains("Initials", avatar, StringComparison.Ordinal);
    }

    private static string MauiProject() => Path.Combine(
        FindRepoRoot(),
        "src",
        "Products",
        "PinoyBusinessPOS",
        "ExItS.PinoyBusinessPOS.Maui");

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
