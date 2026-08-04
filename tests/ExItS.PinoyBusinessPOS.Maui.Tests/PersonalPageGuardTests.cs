namespace ExItS.PinoyBusinessPOS.Maui.Tests;

/// <summary>
/// Static regression guards for Personal MVP + Personal Utang Mobile surfaces.
/// </summary>
public sealed class PersonalPageGuardTests
{
    [Fact]
    public void Personal_routes_cover_dashboard_utang_profile_settings_and_start_business()
    {
        var personal = PersonalPagesDirectory();

        var home = File.ReadAllText(Path.Combine(personal, "PersonalHome.razor"));
        Assert.Contains("@page \"/personal\"", home, StringComparison.Ordinal);
        Assert.Contains("GetPersonalDashboardAsync", home, StringComparison.Ordinal);
        Assert.Contains("Personal_Stat_People", home, StringComparison.Ordinal);
        Assert.Contains("Personal_Nav_People", home, StringComparison.Ordinal);
        Assert.Contains("Personal_Nav_Lent", home, StringComparison.Ordinal);
        Assert.Contains("Personal_Nav_Borrowed", home, StringComparison.Ordinal);
        Assert.Contains("Personal_Nav_UtangInvitations", home, StringComparison.Ordinal);
        Assert.Contains("Personal_Nav_PaymentsSoon", home, StringComparison.Ordinal);
        Assert.Contains("EnsurePersonalAccountProfileAsync", home, StringComparison.Ordinal);
        Assert.Contains("EmptyState", home, StringComparison.Ordinal);
        Assert.Contains("Personal_NoInvitationsTitle", home, StringComparison.Ordinal);
        Assert.DoesNotContain("@page \"/sales", home, StringComparison.Ordinal);

        Assert.Contains("@page \"/personal/utang/people\"",
            File.ReadAllText(Path.Combine(personal, "PersonalPeople.razor")), StringComparison.Ordinal);
        Assert.Contains("CreatePersonalContactAsync",
            File.ReadAllText(Path.Combine(personal, "PersonalPeople.razor")), StringComparison.Ordinal);
        Assert.Contains("Personal_PeopleEmptyTitle",
            File.ReadAllText(Path.Combine(personal, "PersonalPeople.razor")), StringComparison.Ordinal);

        Assert.Contains("@page \"/personal/utang/lent\"",
            File.ReadAllText(Path.Combine(personal, "PersonalLent.razor")), StringComparison.Ordinal);
        Assert.Contains("CreatePersonalDebtRelationshipAsync",
            File.ReadAllText(Path.Combine(personal, "PersonalLent.razor")), StringComparison.Ordinal);
        Assert.Contains("Personal_LentEmptyTitle",
            File.ReadAllText(Path.Combine(personal, "PersonalLent.razor")), StringComparison.Ordinal);

        Assert.Contains("@page \"/personal/utang/borrowed\"",
            File.ReadAllText(Path.Combine(personal, "PersonalBorrowed.razor")), StringComparison.Ordinal);
        Assert.Contains("Personal_BorrowedEmptyTitle",
            File.ReadAllText(Path.Combine(personal, "PersonalBorrowed.razor")), StringComparison.Ordinal);

        var detail = File.ReadAllText(Path.Combine(personal, "PersonalRelationshipDetail.razor"));
        Assert.Contains("@page \"/personal/utang/relationships/{RelationshipId:guid}\"", detail, StringComparison.Ordinal);
        Assert.Contains("RecordPersonalUtangEntryAsync", detail, StringComparison.Ordinal);
        Assert.Contains("GetPersonalUtangHistoryAsync", detail, StringComparison.Ordinal);

        var invites = File.ReadAllText(Path.Combine(personal, "PersonalUtangInvitations.razor"));
        Assert.Contains("@page \"/personal/utang/invitations\"", invites, StringComparison.Ordinal);
        Assert.Contains("AcceptPersonalUtangInvitationAsync", invites, StringComparison.Ordinal);
        Assert.Contains("DeclinePersonalUtangInvitationAsync", invites, StringComparison.Ordinal);
        Assert.Contains("Personal_NoUtangInvitationsTitle", invites, StringComparison.Ordinal);

        Assert.Contains("GetPersonalProfileAsync",
            File.ReadAllText(Path.Combine(personal, "PersonalProfile.razor")), StringComparison.Ordinal);
        Assert.Contains("@page \"/personal/settings\"",
            File.ReadAllText(Path.Combine(personal, "PersonalSettings.razor")), StringComparison.Ordinal);
        Assert.Contains("StartBusinessAsync",
            File.ReadAllText(Path.Combine(personal, "StartBusiness.razor")), StringComparison.Ordinal);
    }

    [Fact]
    public void Personal_utang_pages_use_auth_shell_and_personal_profile_gate()
    {
        foreach (var file in Directory.EnumerateFiles(PersonalPagesDirectory(), "*.razor"))
        {
            var text = File.ReadAllText(file);
            Assert.Contains("@layout Layout.AuthShell", text, StringComparison.Ordinal);
            Assert.DoesNotContain("MainLayout", text, StringComparison.Ordinal);
            Assert.DoesNotContain("/sales/new", text, StringComparison.Ordinal);
            Assert.DoesNotContain("/registers", text, StringComparison.Ordinal);
        }

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
        Assert.Contains("GetPersonalContactsAsync", client, StringComparison.Ordinal);
        Assert.Contains("CreatePersonalDebtRelationshipAsync", client, StringComparison.Ordinal);
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
