using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Web.Services;
using Xunit;

namespace ExItS.PinoyBusinessPOS.Web.Tests;

public sealed class OrgWebNavigationIconGuardTests
{
    [Fact]
    public void OrgWebTopLevelNavigationHasIcons()
    {
        var layout = ReadMainLayout();
        Assert.Contains("Icon=\"dashboard\"", layout, StringComparison.Ordinal);
        foreach (var icon in new[] { "shop", "team", "appstore", "database", "dollar", "control", "setting" })
        {
            Assert.Contains($"SubMenuTitle(\"{icon}\"", layout, StringComparison.Ordinal);
        }

        Assert.Contains("<SubMenu Key=\"business\" TitleTemplate=\"@BusinessTitle\">", layout, StringComparison.Ordinal);
        Assert.Contains("ShowCollapsedTooltip=\"true\"", layout, StringComparison.Ordinal);
    }

    [Fact]
    public void OrgWebSubMenuUsesSupportedAntDesignApi()
    {
        var layout = ReadMainLayout();
        // AntDesign 1.6.2 SubMenu has no Icon parameter — TitleTemplate only.
        foreach (var line in layout.Split('\n'))
        {
            if (line.Contains("<SubMenu", StringComparison.Ordinal)
                && !line.Contains("</SubMenu", StringComparison.Ordinal))
            {
                Assert.DoesNotContain(" Icon=", line, StringComparison.Ordinal);
                Assert.Contains("TitleTemplate=", line, StringComparison.Ordinal);
            }
        }

        Assert.Contains("OpenComponent<Icon>", layout, StringComparison.Ordinal);
        Assert.Contains("exits-submenu-title", layout, StringComparison.Ordinal);
        Assert.Contains("exits-submenu-label", layout, StringComparison.Ordinal);

        // Other AntDesign hosts must not reintroduce SubMenu Icon=.
        foreach (var relative in new[]
                 {
                     Path.Combine("src", "Platform", "ExItS.Platform.Admin", "Components", "Layout", "AdminNav.razor"),
                     Path.Combine("src", "Platform", "ExItS.Personal.Web", "Components", "Layout", "MainLayout.razor")
                 })
        {
            var path = Path.Combine(FindRepoRoot(), relative);
            if (!File.Exists(path))
            {
                continue;
            }

            var text = File.ReadAllText(path);
            Assert.DoesNotMatch(
                new System.Text.RegularExpressions.Regex(@"<SubMenu\b[^>]*\bIcon\s*=", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                text);
        }
    }

    [Fact]
    public void OrgWebCollapsedNavigationHasLabels()
    {
        var layout = ReadMainLayout();
        Assert.Contains("InlineCollapsed=\"_collapsed\"", layout, StringComparison.Ordinal);
        Assert.Contains("CollapsedWidth=\"64\"", layout, StringComparison.Ordinal);
        Assert.Contains("ShowCollapsedTooltip=\"true\"", layout, StringComparison.Ordinal);
        Assert.Contains("Title=\"@L[\"Nav_Overview\"]\"", layout, StringComparison.Ordinal);
        Assert.Contains("Title=\"@L[\"Nav_Branches\"]\"", layout, StringComparison.Ordinal);
        Assert.Contains("Nav_Business", layout, StringComparison.Ordinal);
    }

    [Fact]
    public void OrgWebCollapsedNavigationRoutes()
    {
        var layout = ReadMainLayout();
        foreach (var route in new[]
                 {
                     "/overview",
                     "/organization/branches",
                     "/staff",
                     "/products",
                     "/inventory",
                     "/sales",
                     "/operations/shifts",
                     "/settings"
                 })
        {
            Assert.Contains($"RouterLink=\"{route}\"", layout, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Hydrator_restores_ambient_on_circuit_inbound_activity()
    {
        var services = ReadFile(
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Services",
            "WebHostServices.cs");

        Assert.Contains("CreateInboundActivityHandler", services, StringComparison.Ordinal);
        Assert.Contains("ApplyToAmbient", services, StringComparison.Ordinal);
        Assert.Contains("circuitSession.AccessToken", services, StringComparison.Ordinal);
    }

    [Fact]
    public void Branches_hides_mutations_until_authorized()
    {
        var page = ReadFile(
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Components",
            "Pages",
            "Organization",
            "Branches.razor");

        Assert.Contains("_authorized && !_loading", page, StringComparison.Ordinal);
        Assert.Contains("_authorized = true", page, StringComparison.Ordinal);
    }

    [Fact]
    public void OrgWebUi_distinguishes_auth_permission_and_plan()
    {
        var auth = OrgWebUi.Error(new ApiError(
            "Unauthorized",
            "Development-stage commercial headers required",
            "unauthorized",
            null,
            401));
        Assert.Contains("session", auth, StringComparison.OrdinalIgnoreCase);

        var permission = OrgWebUi.Error(new ApiError(
            "Forbidden",
            "Actor 'x' does not hold permission 'platform.permission.manage_memberships'.",
            "forbidden",
            null,
            403));
        Assert.Contains("permission", permission, StringComparison.OrdinalIgnoreCase);

        var sessionGap = OrgWebUi.Error(new ApiError(
            "Forbidden",
            "Actor 'x' does not hold permission 'platform.permission.view_portfolio'.",
            "forbidden",
            null,
            403));
        Assert.Contains("verify your access", sessionGap, StringComparison.OrdinalIgnoreCase);

        var plan = OrgWebUi.Error(new ApiError(
            "Forbidden",
            "Active subscription entitlement is required.",
            "commercial_denied",
            null,
            403));
        Assert.Contains("plan", plan, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadMainLayout() =>
        ReadFile(
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Components",
            "Layout",
            "MainLayout.razor");

    private static string ReadFile(params string[] parts)
    {
        var root = FindRepoRoot();
        return File.ReadAllText(Path.Combine(new[] { root }.Concat(parts).ToArray()));
    }

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
