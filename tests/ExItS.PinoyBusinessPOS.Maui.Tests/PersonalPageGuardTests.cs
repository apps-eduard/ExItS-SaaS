namespace ExItS.PinoyBusinessPOS.Maui.Tests;

/// <summary>
/// Static regression guards for Personal-first bottom tabs, Explore POS, and Utang surfaces.
/// </summary>
public sealed class PersonalPageGuardTests
{
    [Fact]
    public void Personal_shell_exposes_bottom_tabs_without_pos_chrome()
    {
        var shell = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Layout", "PersonalShell.razor"));
        Assert.Contains("pos-shell--personal", shell, StringComparison.Ordinal);
        Assert.Contains("pos-bottom-nav", shell, StringComparison.Ordinal);
        Assert.Contains("pos-nav-item--active", shell, StringComparison.Ordinal);
        Assert.Contains("href=\"/personal\"", shell, StringComparison.Ordinal);
        Assert.Contains("href=\"/personal/utang/people\"", shell, StringComparison.Ordinal);
        Assert.Contains("href=\"/personal/utang/lent\"", shell, StringComparison.Ordinal);
        Assert.Contains("href=\"/personal/utang/borrowed\"", shell, StringComparison.Ordinal);
        Assert.Contains("href=\"/personal/more\"", shell, StringComparison.Ordinal);
        Assert.Contains("replace: true", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("/sales", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("/catalog", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("/catalog/global", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("/catalog/import", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("SellingMode", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("Brand_Name", shell, StringComparison.Ordinal);
        Assert.DoesNotContain("Env_Development", shell, StringComparison.Ordinal);
        Assert.Contains("StoreHeader", shell, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderIdentity.Personal", shell, StringComparison.Ordinal);
        Assert.Contains("ShowSync=\"true\"", shell, StringComparison.Ordinal);
    }

    [Fact]
    public void Personal_home_is_summary_only_without_primary_nav_buttons()
    {
        var personal = PersonalPagesDirectory();
        var home = File.ReadAllText(Path.Combine(personal, "PersonalHome.razor"));

        Assert.Contains("@page \"/personal\"", home, StringComparison.Ordinal);
        Assert.Contains("@layout Layout.PersonalShell", home, StringComparison.Ordinal);
        Assert.Contains("GetPersonalDashboardAsync", home, StringComparison.Ordinal);
        Assert.Contains("Personal_Stat_People", home, StringComparison.Ordinal);
        Assert.Contains("Personal_RecentActivitySection", home, StringComparison.Ordinal);
        Assert.Contains("EnsurePersonalAccountProfileAsync", home, StringComparison.Ordinal);
        Assert.Contains("Personal_DashboardSection", home, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", home, StringComparison.Ordinal);
        Assert.DoesNotContain("Personal_HomeTitle", home, StringComparison.Ordinal);

        Assert.DoesNotContain("Personal_Nav_People", home, StringComparison.Ordinal);
        Assert.DoesNotContain("Personal_Nav_Lent", home, StringComparison.Ordinal);
        Assert.DoesNotContain("Personal_Nav_Borrowed", home, StringComparison.Ordinal);
        Assert.DoesNotContain("Personal_Nav_UtangInvitations", home, StringComparison.Ordinal);
        Assert.DoesNotContain("Personal_Nav_PaymentsSoon", home, StringComparison.Ordinal);
        Assert.DoesNotContain("Personal_ExplorePos", home, StringComparison.Ordinal);
        Assert.DoesNotContain("Auth_Logout", home, StringComparison.Ordinal);
        Assert.DoesNotContain("AccountContextSwitcher", home, StringComparison.Ordinal);
        Assert.DoesNotContain("Personal_OrganizationsSection", home, StringComparison.Ordinal);
        Assert.DoesNotContain("Personal_StartBusiness", home, StringComparison.Ordinal);
        Assert.DoesNotContain("/start-business", home, StringComparison.Ordinal);
        Assert.DoesNotContain("@page \"/sales", home, StringComparison.Ordinal);
    }

    [Fact]
    public void Personal_more_hub_hosts_secondary_actions()
    {
        var more = File.ReadAllText(Path.Combine(PersonalPagesDirectory(), "PersonalMore.razor"));
        Assert.Contains("@page \"/personal/more\"", more, StringComparison.Ordinal);
        Assert.Contains("@layout Layout.PersonalShell", more, StringComparison.Ordinal);
        Assert.Contains("Personal_MyQrLink", more, StringComparison.Ordinal);
        Assert.Contains("/personal/my-qr", more, StringComparison.Ordinal);
        Assert.Contains("Personal_Nav_UtangInvitations", more, StringComparison.Ordinal);
        Assert.Contains("Personal_ProfileLink", more, StringComparison.Ordinal);
        Assert.Contains("Personal_SettingsLink", more, StringComparison.Ordinal);
        Assert.Contains("Personal_ExplorePos", more, StringComparison.Ordinal);
        Assert.Contains("Auth_Logout", more, StringComparison.Ordinal);
        Assert.DoesNotContain("AccountContextSwitcher", more, StringComparison.Ordinal);
        Assert.DoesNotContain("/sales", more, StringComparison.Ordinal);
    }

    [Fact]
    public void Public_user_qr_and_resolve_require_confirm_before_link()
    {
        var personal = PersonalPagesDirectory();
        var myQr = File.ReadAllText(Path.Combine(personal, "PersonalMyQr.razor"));
        Assert.Contains("@page \"/personal/my-qr\"", myQr, StringComparison.Ordinal);
        Assert.Contains("GetMyPublicIdentityAsync", myQr, StringComparison.Ordinal);
        Assert.Contains("LocalQrCodeRenderer", myQr, StringComparison.Ordinal);
        Assert.DoesNotContain("api.qrserver.com", myQr, StringComparison.Ordinal);
        Assert.Contains("Personal_MyQrLoading", myQr, StringComparison.Ordinal);
        Assert.Contains("LocalQrCodeRenderer.TryToPngDataUrl", myQr, StringComparison.Ordinal);
        Assert.Contains("IsSafeIdentity", myQr, StringComparison.Ordinal);
        Assert.Contains("exits://user/v1/", myQr, StringComparison.Ordinal);
        Assert.Contains("ApiCallStatus.Unauthorized", myQr, StringComparison.Ordinal);
        Assert.Contains("Personal_MyQrRenderErrorTitle", myQr, StringComparison.Ordinal);
        Assert.DoesNotContain("PosBusinessApi", myQr, StringComparison.Ordinal);

        var resolve = File.ReadAllText(Path.Combine(personal, "PublicUserResolve.razor"));
        Assert.Contains("@page \"/personal/resolve-user\"", resolve, StringComparison.Ordinal);
        Assert.Contains("ResolvePublicUserIdAsync", resolve, StringComparison.Ordinal);
        Assert.Contains("Personal_ResolveConfirm", resolve, StringComparison.Ordinal);
        Assert.Contains("IsSelf", resolve, StringComparison.Ordinal);
        Assert.Contains("purpose=utang-people",
            File.ReadAllText(Path.Combine(personal, "PersonalPeople.razor")), StringComparison.Ordinal);

        var client = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.ApiClient",
            "PlatformAccessClient.cs"));
        Assert.Contains("/api/v1/me/public-identity", client, StringComparison.Ordinal);
        Assert.Contains("/api/v1/users/resolve-public-id", client, StringComparison.Ordinal);

        var sessionHandler = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.ApiClient",
            "PlatformSessionHeaderHandler.cs"));
        Assert.Contains("/api/v1/me/public-identity", sessionHandler, StringComparison.Ordinal);
        Assert.Contains("/api/v1/users/resolve-public-id", sessionHandler, StringComparison.Ordinal);
        Assert.Contains("PlatformSession", sessionHandler, StringComparison.Ordinal);

        var renderer = File.ReadAllText(Path.Combine(MauiProject(), "Services", "LocalQrCodeRenderer.cs"));
        Assert.Contains("TryToPngDataUrl", renderer, StringComparison.Ordinal);
        Assert.Contains("data:image/png;base64,", renderer, StringComparison.Ordinal);
    }

    [Fact]
    public void Explore_pos_loads_catalog_plans_and_defers_org_creation()
    {
        var personal = PersonalPagesDirectory();
        var explore = File.ReadAllText(Path.Combine(personal, "PersonalExplorePos.razor"));
        Assert.Contains("@page \"/personal/explore-pos\"", explore, StringComparison.Ordinal);
        Assert.Contains("GetCommercialPlansAsync", explore, StringComparison.Ordinal);
        Assert.Contains("PosProductCodes.PinoyBusinessPos", explore, StringComparison.Ordinal);
        Assert.Contains("/start-business?planKey=", explore, StringComparison.Ordinal);
        Assert.DoesNotContain("StartBusinessAsync", explore, StringComparison.Ordinal);

        var start = File.ReadAllText(Path.Combine(personal, "StartBusiness.razor"));
        Assert.Contains("@page \"/start-business\"", start, StringComparison.Ordinal);
        Assert.Contains("@layout Layout.AuthShell", start, StringComparison.Ordinal);
        Assert.Contains("StartBusiness_PlanRequired", start, StringComparison.Ordinal);
        Assert.Contains("StartBusiness_ConfirmSubmit", start, StringComparison.Ordinal);
        Assert.Contains("StartBusinessAsync", start, StringComparison.Ordinal);

        var submitIndex = start.IndexOf("SubmitAsync", StringComparison.Ordinal);
        var createIndex = start.IndexOf("StartBusinessAsync", StringComparison.Ordinal);
        Assert.True(submitIndex > 0 && createIndex > submitIndex);

        var client = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.ApiClient",
            "PlatformAccessClient.cs"));
        Assert.Contains("/api/v1/commercial/plans", client, StringComparison.Ordinal);
        Assert.Contains("GetCommercialPlansAsync", client, StringComparison.Ordinal);
    }

    [Fact]
    public void Org_and_product_switching_remains_available_outside_personal_home()
    {
        var settings = File.ReadAllText(Path.Combine(PersonalPagesDirectory(), "PersonalSettings.razor"));
        Assert.Contains("AccountContextSwitcher", settings, StringComparison.Ordinal);
        Assert.Contains("Settings_SwitchOrganization", settings, StringComparison.Ordinal);

        var orgSelect = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "OrganizationSelect.razor"));
        Assert.Contains("/personal/explore-pos", orgSelect, StringComparison.Ordinal);
        Assert.DoesNotContain("GoStartBusiness", orgSelect, StringComparison.Ordinal);
    }

    [Fact]
    public void Personal_utang_routes_and_invitation_empty_state()
    {
        var personal = PersonalPagesDirectory();

        Assert.Contains("@page \"/personal/utang/people\"",
            File.ReadAllText(Path.Combine(personal, "PersonalPeople.razor")), StringComparison.Ordinal);
        Assert.Contains("CreatePersonalContactAsync",
            File.ReadAllText(Path.Combine(personal, "PersonalPeople.razor")), StringComparison.Ordinal);

        Assert.Contains("@page \"/personal/utang/lent\"",
            File.ReadAllText(Path.Combine(personal, "PersonalLent.razor")), StringComparison.Ordinal);
        Assert.Contains("@page \"/personal/utang/borrowed\"",
            File.ReadAllText(Path.Combine(personal, "PersonalBorrowed.razor")), StringComparison.Ordinal);

        var detail = File.ReadAllText(Path.Combine(personal, "PersonalRelationshipDetail.razor"));
        Assert.Contains("@page \"/personal/utang/relationships/{RelationshipId:guid}\"", detail, StringComparison.Ordinal);
        Assert.Contains("RecordPersonalUtangEntryAsync", detail, StringComparison.Ordinal);

        var invites = File.ReadAllText(Path.Combine(personal, "PersonalUtangInvitations.razor"));
        Assert.Contains("@page \"/personal/utang/invitations\"", invites, StringComparison.Ordinal);
        Assert.Contains("Personal_NoUtangInvitationsTitle", invites, StringComparison.Ordinal);
        Assert.Contains("ErrorState", invites, StringComparison.Ordinal);
        Assert.Contains("Common_Retry", invites, StringComparison.Ordinal);

        var resx = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));
        Assert.Contains("<value>No pending Utang invitations</value>", resx, StringComparison.Ordinal);
    }

    [Fact]
    public void Personal_tab_pages_use_personal_shell_and_auth_shell_stays_for_onboarding()
    {
        foreach (var name in new[]
                 {
                     "PersonalHome.razor", "PersonalMore.razor", "PersonalPeople.razor", "PersonalLent.razor",
                     "PersonalBorrowed.razor", "PersonalRelationshipDetail.razor", "PersonalUtangInvitations.razor",
                     "PersonalProfile.razor", "PersonalSettings.razor", "PersonalExplorePos.razor",
                     "PersonalPeopleDetail.razor", "PersonalMyQr.razor"
                 })
        {
            var text = File.ReadAllText(Path.Combine(PersonalPagesDirectory(), name));
            Assert.Contains("@layout Layout.PersonalShell", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MainLayout", text, StringComparison.Ordinal);
            Assert.DoesNotContain("/sales/new", text, StringComparison.Ordinal);
            Assert.DoesNotContain("/registers", text, StringComparison.Ordinal);
        }

        Assert.Contains("@layout Layout.AuthShell",
            File.ReadAllText(Path.Combine(PersonalPagesDirectory(), "StartBusiness.razor")), StringComparison.Ordinal);
        Assert.Contains("@layout Layout.AuthShell",
            File.ReadAllText(Path.Combine(PersonalPagesDirectory(), "PersonalInvitationAccept.razor")), StringComparison.Ordinal);

        foreach (var name in new[]
                 {
                     "PersonalPeople.razor", "PersonalLent.razor", "PersonalBorrowed.razor",
                     "PersonalRelationshipDetail.razor", "PersonalUtangInvitations.razor", "PersonalHome.razor"
                 })
        {
            var text = File.ReadAllText(Path.Combine(PersonalPagesDirectory(), name));
            Assert.Contains("EnsurePersonalAccountProfileAsync", text, StringComparison.Ordinal);
            Assert.Contains("ErrorState", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Personal_client_exposes_utang_apis_without_org_requirement()
    {
        var client = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.ApiClient",
            "PlatformAccessClient.cs"));
        Assert.Contains("/api/v1/personal/dashboard", client, StringComparison.Ordinal);
        Assert.Contains("/api/v1/personal/utang/contacts", client, StringComparison.Ordinal);
        Assert.Contains("/api/v1/personal/utang/relationships/lent", client, StringComparison.Ordinal);
        Assert.Contains("/api/v1/personal/utang/relationships/borrowed", client, StringComparison.Ordinal);
        Assert.Contains("/api/v1/personal/utang/invitations/accept", client, StringComparison.Ordinal);
        Assert.Contains("/api/v1/personal/utang/invitations/decline", client, StringComparison.Ordinal);
    }

    [Fact]
    public void Switch_to_personal_ensures_personal_account_profile()
    {
        var auth = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Auth",
            "AuthenticationService.cs"));
        Assert.Contains("EnsurePersonalAccountProfileAsync", auth, StringComparison.Ordinal);
        Assert.Contains("SwitchToPersonalAsync", auth, StringComparison.Ordinal);
        Assert.Contains("SelectAccountProfileAsync", auth, StringComparison.Ordinal);
    }

    [Fact]
    public void Navigation_gate_restores_personal_without_organization()
    {
        var gate = File.ReadAllText(Path.Combine(MauiProject(), "Services", "NavigationGate.cs"));
        Assert.Contains("OrganizationId is null", gate, StringComparison.Ordinal);
        Assert.Contains("RoleHomeResolver.PersonalHome", gate, StringComparison.Ordinal);
        Assert.Contains("RestoreSessionAsync", gate, StringComparison.Ordinal);

        var policy = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Offline",
            "ProtectedShellAccessPolicy.cs"));
        Assert.Contains("OrganizationId is not null", policy, StringComparison.Ordinal);
        Assert.Contains("HasPosAccess", policy, StringComparison.Ordinal);
    }

    [Fact]
    public void Direct_pos_sales_pages_still_deny_personal_shell()
    {
        var sales = Path.Combine(MauiProject(), "Components", "Pages", "Sales");
        foreach (var file in Directory.EnumerateFiles(sales, "*.razor"))
        {
            var text = File.ReadAllText(file);
            Assert.Contains("Gate.CanEnterProtectedShell", text, StringComparison.Ordinal);
            Assert.Contains("ResolveStartRouteAsync", text, StringComparison.Ordinal);
        }
    }

    private static string PersonalPagesDirectory() => Path.Combine(
        MauiProject(),
        "Components",
        "Pages",
        "Personal");

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
