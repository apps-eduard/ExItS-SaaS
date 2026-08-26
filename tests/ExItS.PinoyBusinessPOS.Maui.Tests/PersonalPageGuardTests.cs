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
        Assert.Contains("OfflineAwareNavigation", shell, StringComparison.Ordinal);
        Assert.Contains("OfflineNav.NavigateAsync", shell, StringComparison.Ordinal);
        Assert.Contains("replace: true", shell, StringComparison.Ordinal);
        Assert.Contains("CloseOverlays", shell, StringComparison.Ordinal);
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
        Assert.Contains("pos-personal-stats", home, StringComparison.Ordinal);
        Assert.Contains("pos-personal-row", home, StringComparison.Ordinal);
        Assert.Contains("EnsurePersonalAccountProfileAsync", home, StringComparison.Ordinal);
        Assert.Contains("Personal_DashboardSection", home, StringComparison.Ordinal);
        Assert.Contains("Personal_HomeTitle", home, StringComparison.Ordinal);
        Assert.Contains("pos-personal-home__header", home, StringComparison.Ordinal);
        Assert.Contains("OnAfterRenderAsync", home, StringComparison.Ordinal);
        Assert.DoesNotContain("await PersonalSync.TrySyncPendingAsync();", home, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", home, StringComparison.Ordinal);

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
        Assert.Contains("Personal_Nav_LinkedMerchants", more, StringComparison.Ordinal);
        Assert.Contains("/personal/linked-merchants", more, StringComparison.Ordinal);
        Assert.Contains("/personal/rewards", File.ReadAllText(Path.Combine(PersonalPagesDirectory(), "PersonalLinkedMerchantStatement.razor")), StringComparison.Ordinal);
        Assert.Contains("Personal_ProfileLink", more, StringComparison.Ordinal);
        Assert.Contains("Personal_SettingsLink", more, StringComparison.Ordinal);
        Assert.Contains("Personal_ExplorePos", more, StringComparison.Ordinal);
        Assert.Contains("OfflineAwareNavigation", more, StringComparison.Ordinal);
        Assert.Contains("OfflineNav.NavigateAsync", more, StringComparison.Ordinal);
        Assert.DoesNotContain("Auth_Logout", more, StringComparison.Ordinal);
        Assert.Contains("pos-settings__nav", more, StringComparison.Ordinal);
        Assert.Contains("pos-personal-more__header", more, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"qr\")", more, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", more, StringComparison.Ordinal);
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
        Assert.Contains("exits://qr/v1/personal/", myQr, StringComparison.Ordinal);
        Assert.Contains("exits://user/v1/", myQr, StringComparison.Ordinal);
        Assert.Contains("ApiCallStatus.Unauthorized", myQr, StringComparison.Ordinal);
        Assert.Contains("EnsurePlatformSessionAsync", myQr, StringComparison.Ordinal);
        Assert.Contains("pos-my-qr", myQr, StringComparison.Ordinal);
        Assert.Contains("pos-personal-my-qr__header", myQr, StringComparison.Ordinal);
        Assert.Contains("pos-action-grid--three", myQr, StringComparison.Ordinal);
        Assert.Contains("Personal_MyQrRenderErrorTitle", myQr, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", myQr, StringComparison.Ordinal);
        Assert.DoesNotContain("PosBusinessApi", myQr, StringComparison.Ordinal);

        var en = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));
        Assert.Contains("<value>My QR</value>", en, StringComparison.Ordinal);
        Assert.Contains("Use this to connect with me on ExItS.", en, StringComparison.Ordinal);

        var resolve = File.ReadAllText(Path.Combine(personal, "PublicUserResolve.razor"));
        Assert.Contains("@page \"/personal/resolve-user\"", resolve, StringComparison.Ordinal);
        Assert.Contains("@layout Layout.PersonalShell", resolve, StringComparison.Ordinal);
        Assert.Contains("ResolvePublicUserIdAsync", resolve, StringComparison.Ordinal);
        Assert.Contains("pos-personal-resolve__header", resolve, StringComparison.Ordinal);
        Assert.Contains("pos-resolve-actions", resolve, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", resolve, StringComparison.Ordinal);
        Assert.Contains("pos-action-grid--three", resolve, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"qr\")", resolve, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"image\")", resolve, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"search\")", resolve, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"check\")", resolve, StringComparison.Ordinal);
        Assert.Contains("EnsurePlatformSessionAsync", resolve, StringComparison.Ordinal);
        Assert.Contains("Personal_ResolveSessionRequired", resolve, StringComparison.Ordinal);
        Assert.Contains("Personal_ResolveConfirm", resolve, StringComparison.Ordinal);
        Assert.Contains("IsSelf", resolve, StringComparison.Ordinal);
        Assert.Contains("IQrCodeScanService", resolve, StringComparison.Ordinal);
        Assert.Contains("Personal_ScanQr", resolve, StringComparison.Ordinal);
        Assert.Contains("ScanQrAsync", resolve, StringComparison.Ordinal);
        Assert.DoesNotContain("Personal_ResolveCameraDeferred", resolve, StringComparison.Ordinal);
        Assert.Contains("purpose=utang-people",
            File.ReadAllText(Path.Combine(personal, "PersonalPeople.razor")), StringComparison.Ordinal);

        var create = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Pages", "Customers", "CustomerCreate.razor"));
        Assert.Contains("GoScanExItsQr", create, StringComparison.Ordinal);
        Assert.Contains("scan=1", create, StringComparison.Ordinal);
        Assert.Contains("Personal_ScanQr", create, StringComparison.Ordinal);
        Assert.Contains("IsSaleCheckoutReturn", create, StringComparison.Ordinal);
        Assert.Contains("SupplyParameterFromQuery(Name = \"return\")", create, StringComparison.Ordinal);

        var qrScan = File.ReadAllText(Path.Combine(MauiProject(), "Services", "MauiQrCodeScanService.cs"));
        Assert.Contains("CapturePhotoAsync", qrScan, StringComparison.Ordinal);
        Assert.Contains("BarcodeReaderGeneric", qrScan, StringComparison.Ordinal);
        Assert.Contains("BarcodeFormat.QR_CODE", qrScan, StringComparison.Ordinal);

        var client = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.ApiClient",
            "PlatformAccessClient.cs"));
        Assert.Contains("/api/v1/me/public-identity", client, StringComparison.Ordinal);
        Assert.Contains("/api/v1/users/resolve-public-id", client, StringComparison.Ordinal);
        Assert.Contains("/api/v1/qr/resolve", client, StringComparison.Ordinal);
        Assert.Contains("registration-tokens", client, StringComparison.Ordinal);

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
        Assert.Contains("pos-personal-explore__header", explore, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", explore, StringComparison.Ordinal);
        Assert.DoesNotContain("StartBusinessAsync", explore, StringComparison.Ordinal);

        var start = File.ReadAllText(Path.Combine(personal, "StartBusiness.razor"));
        Assert.Contains("@page \"/start-business\"", start, StringComparison.Ordinal);
        Assert.Contains("@layout Layout.AuthShell", start, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderBack", start, StringComparison.Ordinal);
        Assert.Contains("Href=\"/personal/explore-pos\"", start, StringComparison.Ordinal);
        Assert.Contains("pos-personal-start-business__header", start, StringComparison.Ordinal);
        Assert.Contains("pos-start-business-plan", start, StringComparison.Ordinal);
        Assert.Contains("OrganizationSlug.SuggestFromDisplayName", start, StringComparison.Ordinal);
        Assert.Contains("GetCommercialPlansAsync", start, StringComparison.Ordinal);
        Assert.Contains("SelectedPrice", start, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", start, StringComparison.Ordinal);
        Assert.DoesNotContain("Personal_BackHome", start, StringComparison.Ordinal);
        Assert.DoesNotContain("Personal_ExplorePosBack", start, StringComparison.Ordinal);
        Assert.Contains("StartBusiness_PlanRequired", start, StringComparison.Ordinal);
        Assert.Contains("StartBusiness_ConfirmSubmit", start, StringComparison.Ordinal);
        Assert.Contains("StartBusinessAsync", start, StringComparison.Ordinal);
        Assert.Contains("EnsurePlatformSessionAvailableAsync", start, StringComparison.Ordinal);
        Assert.Contains("GetOnboardingBusinessTypesAsync", start, StringComparison.Ordinal);
        Assert.Contains("SetBusinessTemplatePromptPendingAsync", start, StringComparison.Ordinal);
        Assert.Contains("SetBusinessTypeActivationPromptPendingAsync", start, StringComparison.Ordinal);
        Assert.Contains("Gate.ResolveStartRouteAsync", start, StringComparison.Ordinal);
        Assert.DoesNotContain("NavigateTo(\"/org\"", start, StringComparison.Ordinal);

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
    public void Personal_primary_tabs_paint_local_before_server_hydrate()
    {
        var personal = PersonalPagesDirectory();
        foreach (var page in new[] { "PersonalHome.razor", "PersonalPeople.razor", "PersonalLent.razor", "PersonalBorrowed.razor" })
        {
            var text = File.ReadAllText(Path.Combine(personal, page));
            Assert.Contains("OnAfterRenderAsync", text, StringComparison.Ordinal);
            Assert.Contains("_remoteStarted", text, StringComparison.Ordinal);
        }

        var auth = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Auth",
            "AuthenticationService.cs"));
        Assert.Contains("HasActivePersonalGrantFor", auth, StringComparison.Ordinal);
    }

    [Fact]
    public void Org_and_product_switching_remains_available_outside_personal_home()
    {
        var settings = File.ReadAllText(Path.Combine(PersonalPagesDirectory(), "PersonalSettings.razor"));
        Assert.Contains("AccountContextSwitcher", settings, StringComparison.Ordinal);
        Assert.Contains("Settings_SwitchOrganization", settings, StringComparison.Ordinal);
        Assert.Contains("Offline_PinSetupTitle", settings, StringComparison.Ordinal);
        Assert.Contains("/offline-pin-setup?mode=", settings, StringComparison.Ordinal);
        Assert.Contains("pos-settings", settings, StringComparison.Ordinal);
        Assert.Contains("pos-settings__panel", settings, StringComparison.Ordinal);
        Assert.Contains("pos-personal-settings__header", settings, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderBack", settings, StringComparison.Ordinal);
        Assert.Contains("Href=\"/personal/more\"", settings, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"customers\")", settings, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", settings, StringComparison.Ordinal);
        Assert.DoesNotContain("Personal_BackMore", settings, StringComparison.Ordinal);
        Assert.DoesNotContain("Auth_Logout", settings, StringComparison.Ordinal);

        var workspaceSelect = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "WorkspaceSelect.razor"));
        var orgSelect = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "OrganizationSelect.razor"));
        Assert.Contains("/workspace-select", orgSelect, StringComparison.Ordinal);
        Assert.Contains("WorkspaceSelect_NoneTitle", workspaceSelect, StringComparison.Ordinal);
        Assert.DoesNotContain("GoStartBusiness", workspaceSelect, StringComparison.Ordinal);
        Assert.DoesNotContain("Auth_Logout", workspaceSelect, StringComparison.Ordinal);
        Assert.Contains("pos-workspace-select-empty", workspaceSelect, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"home\")", workspaceSelect, StringComparison.Ordinal);
        Assert.DoesNotContain("pos-org-role__row--personal", workspaceSelect, StringComparison.Ordinal);
        Assert.DoesNotContain("OrgSelect_PersonalLabel", workspaceSelect, StringComparison.Ordinal);
    }

    [Fact]
    public void Personal_utang_routes_and_invitation_empty_state()
    {
        var personal = PersonalPagesDirectory();

        var people = File.ReadAllText(Path.Combine(personal, "PersonalPeople.razor"));
        Assert.Contains("@page \"/personal/utang/people\"", people, StringComparison.Ordinal);
        Assert.Contains("ILocalPersonalUtangStore", people, StringComparison.Ordinal);
        Assert.Contains("PersistContactAndEnqueueAsync", people, StringComparison.Ordinal);
        Assert.Contains("Personal_PeopleEmailConflict", people, StringComparison.Ordinal);
        Assert.Contains("LocalPersonalStoreErrors.EmailConflict", people, StringComparison.Ordinal);
        Assert.Contains("pos-personal-people__header", people, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", people, StringComparison.Ordinal);

        var lent = File.ReadAllText(Path.Combine(personal, "PersonalLent.razor"));
        Assert.Contains("@page \"/personal/utang/lent\"", lent, StringComparison.Ordinal);
        Assert.Contains("PersistRelationshipAndEnqueueAsync", lent, StringComparison.Ordinal);
        Assert.Contains("pos-personal-utang__header", lent, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", lent, StringComparison.Ordinal);

        var borrowed = File.ReadAllText(Path.Combine(personal, "PersonalBorrowed.razor"));
        Assert.Contains("@page \"/personal/utang/borrowed\"", borrowed, StringComparison.Ordinal);
        Assert.Contains("PersistRelationshipAndEnqueueAsync", borrowed, StringComparison.Ordinal);
        Assert.Contains("pos-personal-utang__header", borrowed, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", borrowed, StringComparison.Ordinal);

        var detail = File.ReadAllText(Path.Combine(personal, "PersonalRelationshipDetail.razor"));
        Assert.Contains("@page \"/personal/utang/relationships/{RelationshipId:guid}\"", detail, StringComparison.Ordinal);
        Assert.Contains("PersistEntryAndEnqueueAsync", detail, StringComparison.Ordinal);
        Assert.Contains("PosOfflineActionKeys.PersonalInvite", detail, StringComparison.Ordinal);
        Assert.Contains("pos-personal-relationship__header", detail, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", detail, StringComparison.Ordinal);

        var invites = File.ReadAllText(Path.Combine(personal, "PersonalUtangInvitations.razor"));
        Assert.Contains("@page \"/personal/utang/invitations\"", invites, StringComparison.Ordinal);
        Assert.Contains("OnlineRequiredGuard", invites, StringComparison.Ordinal);
        Assert.Contains("EnsureOnlineForRouteAsync", invites, StringComparison.Ordinal);
        Assert.Contains("Personal_NoUtangInvitationsTitle", invites, StringComparison.Ordinal);
        Assert.Contains("pos-personal-invitations__header", invites, StringComparison.Ordinal);
        Assert.Contains("ErrorState", invites, StringComparison.Ordinal);
        Assert.Contains("Common_Retry", invites, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", invites, StringComparison.Ordinal);

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
                     "PersonalLinkedMerchants.razor", "PersonalLinkedMerchantStatement.razor",
                     "PersonalLinkedMerchantReceipt.razor", "PersonalRewards.razor",
                     "PersonalMerchantShop.razor", "PersonalMerchantShopReview.razor",
                     "PersonalProfile.razor", "PersonalSettings.razor", "PersonalExplorePos.razor",
                     "PersonalPeopleDetail.razor", "PersonalMyQr.razor", "PublicUserResolve.razor",
                     "PersonalSupportDiagnosticsPage.razor"
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
        var accept = File.ReadAllText(Path.Combine(PersonalPagesDirectory(), "PersonalInvitationAccept.razor"));
        Assert.Contains("@layout Layout.AuthShell", accept, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderBack", accept, StringComparison.Ordinal);
        Assert.Contains("pos-personal-invitation-accept__header", accept, StringComparison.Ordinal);
        Assert.Contains("AcceptOrganizationInvitationAsPersonalAsync", accept, StringComparison.Ordinal);
        Assert.Contains("ICurrentUserContext", accept, StringComparison.Ordinal);

        var profile = File.ReadAllText(Path.Combine(PersonalPagesDirectory(), "PersonalProfile.razor"));
        Assert.Contains("pos-personal-profile__header", profile, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", profile, StringComparison.Ordinal);

        var peopleDetail = File.ReadAllText(Path.Combine(PersonalPagesDirectory(), "PersonalPeopleDetail.razor"));
        Assert.Contains("pos-personal-people-detail__header", peopleDetail, StringComparison.Ordinal);
        Assert.Contains("ILocalPersonalUtangStore", peopleDetail, StringComparison.Ordinal);
        Assert.Contains("GetContactAsync", peopleDetail, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", peopleDetail, StringComparison.Ordinal);

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
        Assert.Contains("/api/v1/platform/auth/organization-invitations/accept-as-personal", client, StringComparison.Ordinal);
        Assert.Contains("/api/v1/personal/utang/invitations/decline", client, StringComparison.Ordinal);
        Assert.Contains("/api/v1/personal/linked-merchants", client, StringComparison.Ordinal);
        Assert.Contains("/api/v1/personal/reward-points/balance", client, StringComparison.Ordinal);
        Assert.Contains("/api/v1/personal/features/", client, StringComparison.Ordinal);
        Assert.Contains("/redeem", client, StringComparison.Ordinal);
    }

    [Fact]
    public void Personal_linked_merchants_pages_are_read_projection_only()
    {
        var personal = PersonalPagesDirectory();
        var list = File.ReadAllText(Path.Combine(personal, "PersonalLinkedMerchants.razor"));
        var statement = File.ReadAllText(Path.Combine(personal, "PersonalLinkedMerchantStatement.razor"));
        var shop = File.ReadAllText(Path.Combine(personal, "PersonalMerchantShop.razor"));
        var review = File.ReadAllText(Path.Combine(personal, "PersonalMerchantShopReview.razor"));
        var linkedClient = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.ApiClient", "PosLinkedCustomerClient.cs"));
        var orderClient = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.ApiClient", "PosCustomerOrderClient.cs"));

        Assert.Contains("@page \"/personal/linked-merchants\"", list, StringComparison.Ordinal);
        Assert.Contains("GetLinkedMerchantsAsync", list, StringComparison.Ordinal);
        Assert.Contains("CanCustomerOrder", list, StringComparison.Ordinal);
        Assert.Contains("Personal_ShopAt", list, StringComparison.Ordinal);
        Assert.Contains("@onclick:stopPropagation", list, StringComparison.Ordinal);
        Assert.Contains("/shop", list, StringComparison.Ordinal);
        Assert.Contains("@page \"/personal/linked-merchants/{OrganizationId:guid}/{PlatformBusinessCustomerId:guid}\"", statement, StringComparison.Ordinal);
        Assert.Contains("IPosLinkedCustomerClient", statement, StringComparison.Ordinal);
        Assert.Contains("GetStatementAsync", statement, StringComparison.Ordinal);
        Assert.Contains("GetRecentActivityAsync", statement, StringComparison.Ordinal);
        Assert.Contains("GetOpenDebtActivityAsync", statement, StringComparison.Ordinal);
        Assert.DoesNotContain("RecordPersonalUtangEntryAsync", statement, StringComparison.Ordinal);
        Assert.DoesNotContain("ILocalPersonalUtangStore", statement, StringComparison.Ordinal);
        Assert.Contains("@page \"/personal/linked-merchants/{OrganizationId:guid}/shop\"", shop, StringComparison.Ordinal);
        Assert.Contains("GetStorefrontAsync", shop, StringComparison.Ordinal);
        Assert.Contains("PersonalMerchantCart", shop, StringComparison.Ordinal);
        Assert.Contains("@page \"/personal/linked-merchants/{OrganizationId:guid}/shop/review\"", review, StringComparison.Ordinal);
        Assert.Contains("PlaceAsCustomerAsync", review, StringComparison.Ordinal);
        Assert.Contains("/personal/orders/", review, StringComparison.Ordinal);
        Assert.Contains("/api/v1/pos/personal/linked-customers/", linkedClient, StringComparison.Ordinal);
        Assert.Contains("open-debt-activity", linkedClient, StringComparison.Ordinal);
        Assert.Contains("older-activity", linkedClient, StringComparison.Ordinal);
        Assert.Contains("/receipts/", linkedClient, StringComparison.Ordinal);
        Assert.Contains("/storefront", orderClient, StringComparison.Ordinal);
        Assert.Contains("QuoteDeliveryAsCustomerAsync", orderClient, StringComparison.Ordinal);
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
        Assert.Contains("EnsurePersonalAccountProfileAsync", gate, StringComparison.Ordinal);
        Assert.Contains("/offline-pin-setup", gate, StringComparison.Ordinal);

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
