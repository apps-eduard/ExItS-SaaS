namespace ExItS.PinoyBusinessPOS.Maui.Tests;

/// <summary>
/// Static regression guards for Personal MVP Mobile surfaces (routes, shell, auth, POS denial).
/// </summary>
public sealed class PersonalPageGuardTests
{
    [Fact]
    public void Personal_routes_cover_home_profile_settings_invitations_and_start_business()
    {
        var personal = PersonalPagesDirectory();

        var home = File.ReadAllText(Path.Combine(personal, "PersonalHome.razor"));
        Assert.Contains("@page \"/personal\"", home, StringComparison.Ordinal);
        Assert.Contains("@layout Layout.AuthShell", home, StringComparison.Ordinal);
        Assert.Contains("Personal_OrganizationsSection", home, StringComparison.Ordinal);
        Assert.Contains("Personal_InvitationsSection", home, StringComparison.Ordinal);
        Assert.Contains("AcceptOrganizationInvitationByIdAsync", home, StringComparison.Ordinal);
        Assert.Contains("StartBusiness", home, StringComparison.Ordinal);
        Assert.Contains("AccountContextSwitcher", home, StringComparison.Ordinal);
        Assert.Contains("SwitchToPersonalAsync", home, StringComparison.Ordinal);
        Assert.Contains("ErrorState", home, StringComparison.Ordinal);
        Assert.Contains("EmptyState", home, StringComparison.Ordinal);
        Assert.Contains("LogoutAsync", home, StringComparison.Ordinal);
        Assert.DoesNotContain("@page \"/sales", home, StringComparison.Ordinal);
        Assert.DoesNotContain("UtangCapability", home, StringComparison.Ordinal);

        var profile = File.ReadAllText(Path.Combine(personal, "PersonalProfile.razor"));
        Assert.Contains("@page \"/personal/profile\"", profile, StringComparison.Ordinal);
        Assert.Contains("@layout Layout.AuthShell", profile, StringComparison.Ordinal);

        var settings = File.ReadAllText(Path.Combine(personal, "PersonalSettings.razor"));
        Assert.Contains("@page \"/personal/settings\"", settings, StringComparison.Ordinal);
        Assert.Contains("@layout Layout.AuthShell", settings, StringComparison.Ordinal);
        Assert.Contains("LogoutAsync", settings, StringComparison.Ordinal);
        Assert.Contains("ThemeCtl", settings, StringComparison.Ordinal);

        var invite = File.ReadAllText(Path.Combine(personal, "PersonalInvitationAccept.razor"));
        Assert.Contains("@page \"/personal/invitations/accept\"", invite, StringComparison.Ordinal);
        Assert.Contains("AcceptOrganizationInvitationAsync", invite, StringComparison.Ordinal);
        Assert.Contains("EnsureOrganizationAccountProfileAsync", invite, StringComparison.Ordinal);

        var start = File.ReadAllText(Path.Combine(personal, "StartBusiness.razor"));
        Assert.Contains("@page \"/start-business\"", start, StringComparison.Ordinal);
        Assert.Contains("StartBusinessAsync", start, StringComparison.Ordinal);
        Assert.Contains("ContinueAfterStartBusinessAsync", start, StringComparison.Ordinal);
    }

    [Fact]
    public void Personal_mode_does_not_host_pos_operational_pages()
    {
        foreach (var file in Directory.EnumerateFiles(PersonalPagesDirectory(), "*.razor"))
        {
            var text = File.ReadAllText(file);
            Assert.Contains("@layout Layout.AuthShell", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MainLayout", text, StringComparison.Ordinal);
            Assert.DoesNotContain("pos-bottom-nav", text, StringComparison.Ordinal);
            Assert.DoesNotContain("/sales/new", text, StringComparison.Ordinal);
            Assert.DoesNotContain("/registers", text, StringComparison.Ordinal);
            Assert.DoesNotContain("/shifts", text, StringComparison.Ordinal);
            Assert.DoesNotContain("/inventory", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Protected_pos_pages_deny_entry_without_organization_shell()
    {
        var sales = Path.Combine(MauiProject(), "Components", "Pages", "Sales");
        foreach (var file in Directory.EnumerateFiles(sales, "*.razor"))
        {
            var text = File.ReadAllText(file);
            Assert.Contains("Gate.CanEnterProtectedShell", text, StringComparison.Ordinal);
            Assert.Contains("ResolveStartRouteAsync", text, StringComparison.Ordinal);
        }

        var cashier = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Pages", "Dashboards", "CashierHome.razor"));
        Assert.Contains("Gate.CanEnterProtectedShell", cashier, StringComparison.Ordinal);
        Assert.Contains("ResolveStartRouteAsync", cashier, StringComparison.Ordinal);

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
    public void Navigation_gate_and_auth_shell_support_personal_session_restore()
    {
        var gate = File.ReadAllText(Path.Combine(MauiProject(), "Services", "NavigationGate.cs"));
        Assert.Contains("OrganizationId is null", gate, StringComparison.Ordinal);
        Assert.Contains("RoleHomeResolver.PersonalHome", gate, StringComparison.Ordinal);
        Assert.Contains("RestoreSessionAsync", gate, StringComparison.Ordinal);

        var authShell = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Layout", "AuthShell.razor"));
        Assert.Contains("pos-shell--auth", authShell, StringComparison.Ordinal);

        var css = File.ReadAllText(Path.Combine(MauiProject(), "wwwroot", "app.css"));
        Assert.Contains(".pos-shell--auth .pos-content", css, StringComparison.Ordinal);

        var settings = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Settings.razor"));
        Assert.Contains("/personal/settings", settings, StringComparison.Ordinal);

        var switcher = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Shared", "AccountContextSwitcher.razor"));
        Assert.Contains("SwitchToPersonalAsync", switcher, StringComparison.Ordinal);
        Assert.Contains("SelectOrganizationAsync", switcher, StringComparison.Ordinal);
    }

    [Fact]
    public void Authentication_service_ensures_organization_profile_before_org_select()
    {
        var auth = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Auth",
            "AuthenticationService.cs"));
        Assert.Contains("EnsureOrganizationAccountProfileAsync", auth, StringComparison.Ordinal);
        Assert.Contains("SelectAccountProfileAsync", auth, StringComparison.Ordinal);
        Assert.Contains("SwitchToPersonalAsync", auth, StringComparison.Ordinal);

        var accessClient = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.ApiClient",
            "PlatformAccessClient.cs"));
        Assert.Contains("organization-invitations/pending", accessClient, StringComparison.Ordinal);
        Assert.Contains("organization-invitations/accept", accessClient, StringComparison.Ordinal);
    }

    [Fact]
    public void Platform_auth_exposes_pending_invitation_apis_for_personal_mvp()
    {
        var endpoints = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Platform",
            "ExItS.Platform.Api",
            "Identity",
            "AuthEndpoints.cs"));
        Assert.Contains("/api/v1/platform/auth/organization-invitations/pending", endpoints, StringComparison.Ordinal);
        Assert.Contains("/api/v1/platform/auth/organization-invitations/accept", endpoints, StringComparison.Ordinal);
        Assert.Contains("AcceptOrganizationInvitationByIdForInvitee", endpoints, StringComparison.Ordinal);

        var orgContext = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Platform",
            "ExItS.Platform.Application",
            "Identity",
            "OrganizationContextUseCases.cs"));
        Assert.Contains("AccountClass.Personal", orgContext, StringComparison.Ordinal);
        Assert.Contains("AccountClass.Organization", orgContext, StringComparison.Ordinal);
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
