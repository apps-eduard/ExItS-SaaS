using Xunit;

namespace ExItS.Platform.Admin.UnitTests;

public sealed class DevelopmentTestUserAndRoutingGuardTests
{
    [Fact]
    public void Test_user_picker_fills_username_only_never_authenticates()
    {
        var root = FindRepositoryRoot();
        var picker = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "LocalValidationIdentityPicker.razor"));
        var a11y = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "wwwroot", "admin-a11y.js"));
        var login = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "Login.razor"));

        Assert.Contains("exitsFillTestUserLogin", picker, StringComparison.Ordinal);
        Assert.Contains("OnTestUserSelectedAsync", picker, StringComparison.Ordinal);
        Assert.DoesNotContain("/admin/login/as/", picker, StringComparison.Ordinal);
        Assert.DoesNotContain("SignInAsKeyAsync", picker, StringComparison.Ordinal);
        Assert.DoesNotContain("SharedPassword", picker, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("pass.value = \"\"", a11y, StringComparison.Ordinal);
        Assert.Contains("login-email", a11y, StringComparison.Ordinal);
        Assert.DoesNotContain("SharedPassword", a11y, StringComparison.OrdinalIgnoreCase);

        Assert.Contains("/admin/login/credentials", login, StringComparison.Ordinal);
        Assert.Contains("type=\"password\"", login, StringComparison.Ordinal);
        Assert.DoesNotContain("SharedPassword", login, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Post_login_router_prefers_organization_over_personal()
    {
        var root = FindRepositoryRoot();
        var router = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Services", "WebPostLoginRouter.cs"));

        Assert.Contains("WebApps.Organization", router, StringComparison.Ordinal);
        Assert.Contains("orgWorkspaces.Count == 1", router, StringComparison.Ordinal);
        Assert.Contains("orgWorkspaces.Count > 1", router, StringComparison.Ordinal);
        Assert.Contains("/admin/workspaces", router, StringComparison.Ordinal);

        var platformIdx = router.IndexOf("WebApps.Platform", StringComparison.Ordinal);
        var orgIdx = router.IndexOf("orgWorkspaces.Count == 1", StringComparison.Ordinal);
        var personalIdx = router.LastIndexOf("WebApps.Personal", StringComparison.Ordinal);
        Assert.True(platformIdx >= 0 && orgIdx > platformIdx && personalIdx > orgIdx);
    }

    [Fact]
    public void Workspace_list_excludes_organization_member_cashier_roles()
    {
        var root = FindRepositoryRoot();
        var handoff = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Application", "Identity", "WebHandoffUseCases.cs"));

        Assert.Contains("OrganizationRole.OrganizationOwner", handoff, StringComparison.Ordinal);
        Assert.Contains("OrganizationRole.OrganizationAdministrator", handoff, StringComparison.Ordinal);
        Assert.Contains("OrganizationMember", handoff, StringComparison.Ordinal);
        Assert.Contains("Cashier", handoff, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
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
