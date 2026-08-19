namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class StaffBranchAuthorizationGuardTests
{
    [Fact]
    public void Mobile_staff_page_exposes_compact_branch_assignment_management()
    {
        var staff = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Organization", "OrgStaff.razor"));
        var css = File.ReadAllText(Path.Combine(MauiProject(), "wwwroot", "app.css"));
        var en = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.fil-PH.resx"));

        Assert.Contains("GetMembershipBranchAssignmentsAsync", staff, StringComparison.Ordinal);
        Assert.Contains("SetMembershipBranchAssignmentsAsync", staff, StringComparison.Ordinal);
        Assert.Contains("pos-staff__branches", staff, StringComparison.Ordinal);
        Assert.Contains("pos-denom-sheet", staff, StringComparison.Ordinal);
        Assert.Contains("Org_StaffBranchesSection", staff, StringComparison.Ordinal);
        Assert.Contains(".pos-staff__branch-option", css, StringComparison.Ordinal);
        Assert.Contains("min-height: 2.75rem", css, StringComparison.Ordinal);

        foreach (var key in new[]
                 {
                     "Org_StaffBranchesSection",
                     "Org_StaffBranchesEdit",
                     "Org_StaffBranchesSave",
                     "Org_StaffBranchesAll"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Web_staff_detail_exposes_branch_assignment_matrix()
    {
        var root = FindRepoRoot();
        var detail = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Components",
            "Pages",
            "Staff",
            "StaffDetail.razor"));
        var css = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "wwwroot",
            "org-web.css"));

        Assert.Contains("Staff_BranchesTitle", detail, StringComparison.Ordinal);
        Assert.Contains("SetMembershipBranchAssignmentsAsync", detail, StringComparison.Ordinal);
        Assert.Contains("org-branch-matrix", detail, StringComparison.Ordinal);
        Assert.Contains(".org-branch-matrix__row", css, StringComparison.Ordinal);
        Assert.Contains("min-height: 44px", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Session_restore_clears_revoked_branch_selection()
    {
        var auth = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Auth",
            "AuthenticationService.cs"));

        Assert.Contains("EnsureSelectedBranchAccessibleAsync", auth, StringComparison.Ordinal);
        Assert.Contains("branch_access_revoked", auth, StringComparison.Ordinal);
        Assert.Contains("application.branch.access_denied", auth, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Repository root not found.");
    }
}
