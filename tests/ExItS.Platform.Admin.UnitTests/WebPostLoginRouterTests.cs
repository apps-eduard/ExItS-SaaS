using ExItS.Platform.Admin.Services;
using ExItS.Web.UI;

namespace ExItS.Platform.Admin.UnitTests;

public sealed class WebPostLoginRouterTests
{
    [Fact]
    public void Platform_user_from_org_login_goes_to_admin_not_overview()
    {
        Assert.Equal(
            "/admin",
            WebPostLoginRouter.ResolveReturnPath(WebApps.Platform, WebApps.Organization, "/overview"));
    }

    [Fact]
    public void Platform_user_from_personal_login_goes_to_admin_not_home()
    {
        Assert.Equal(
            "/admin",
            WebPostLoginRouter.ResolveReturnPath(WebApps.Platform, WebApps.Personal, "/home"));
    }

    [Fact]
    public void Platform_user_keeps_admin_return_path()
    {
        Assert.Equal(
            "/admin/workspaces",
            WebPostLoginRouter.ResolveReturnPath(WebApps.Platform, WebApps.Platform, "/admin/workspaces"));
    }

    [Fact]
    public void Organization_login_keeps_overview_return_path()
    {
        Assert.Equal(
            "/overview",
            WebPostLoginRouter.ResolveReturnPath(WebApps.Organization, WebApps.Organization, "/overview"));
    }

    [Fact]
    public void Personal_user_from_org_login_uses_personal_default()
    {
        Assert.Equal(
            "/",
            WebPostLoginRouter.ResolveReturnPath(WebApps.Personal, WebApps.Organization, "/overview"));
    }
}
