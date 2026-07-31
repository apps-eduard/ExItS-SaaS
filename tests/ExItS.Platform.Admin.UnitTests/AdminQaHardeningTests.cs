namespace ExItS.Platform.Admin.UnitTests;

public sealed class AdminQaHardeningTests
{
    [Fact]
    public void Confirm_dialog_implements_focus_trap_and_return()
    {
        var root = FindRepositoryRoot();
        var confirm = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Shared", "ConfirmDialog.razor"));
        Assert.Contains("exitsAdminA11y.dialogOpen", confirm, StringComparison.Ordinal);
        Assert.Contains("exitsAdminA11y.dialogClose", confirm, StringComparison.Ordinal);
        Assert.Contains("FocusAsync", confirm, StringComparison.Ordinal);
        Assert.Contains("Escape", confirm, StringComparison.Ordinal);

        var js = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "wwwroot", "admin-a11y.js"));
        Assert.Contains("dialogOpen", js, StringComparison.Ordinal);
        Assert.Contains("dialogClose", js, StringComparison.Ordinal);
        Assert.Contains("Tab", js, StringComparison.Ordinal);
        Assert.Contains("aria-expanded", js, StringComparison.Ordinal);
        Assert.Contains("bindDrawer", js, StringComparison.Ordinal);
    }

    [Fact]
    public void App_loads_a11y_helper_and_shell_exposes_drawer_controls()
    {
        var root = FindRepositoryRoot();
        var app = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "App.razor"));
        Assert.Contains("admin-a11y.js", app, StringComparison.Ordinal);
        Assert.Contains("theme-boot.js", app, StringComparison.Ordinal);

        var layout = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Layout", "MainLayout.razor"));
        Assert.Contains("id=\"app-sidebar\"", layout, StringComparison.Ordinal);
        Assert.Contains("aria-controls=\"app-sidebar\"", layout, StringComparison.Ordinal);
        Assert.Contains("role=\"banner\"", layout, StringComparison.Ordinal);
        Assert.Contains("skip-link", layout, StringComparison.Ordinal);
        Assert.DoesNotContain("data-permanent", layout, StringComparison.Ordinal);
    }

    [Fact]
    public void Css_guards_touch_targets_header_overflow_and_reduced_motion()
    {
        var root = FindRepositoryRoot();
        var css = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "wwwroot", "app.css"));
        Assert.Contains("min-width: 2.75rem", css, StringComparison.Ordinal);
        Assert.Contains("min-height: 2.75rem", css, StringComparison.Ordinal);
        Assert.Contains("app-header-controls", css, StringComparison.Ordinal);
        Assert.Contains("flex-wrap: wrap", css, StringComparison.Ordinal);
        Assert.Contains("text-overflow: ellipsis", css, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion", css, StringComparison.Ordinal);
        Assert.DoesNotContain("@tailwind", css, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Product_access_page_uses_localized_strings_and_shared_table()
    {
        var root = FindRepositoryRoot();
        var page = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Pages", "OrganizationProductAccess.razor"));
        Assert.Contains("OrgProductAccess_RevokeConfirmMessage", page, StringComparison.Ordinal);
        Assert.Contains("OrgMembers_UserIdInvalid", page, StringComparison.Ordinal);
        Assert.Contains("OrgProductAccess_ToastGranted", page, StringComparison.Ordinal);
        Assert.Contains("OrgProductAccess_ToastRevoked", page, StringComparison.Ordinal);
        Assert.Contains("AdminDataTable", page, StringComparison.Ordinal);
        Assert.Contains("ToastService", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Product access granted.", page, StringComparison.Ordinal);
        Assert.DoesNotContain("User ID must be a valid GUID.", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Status_badge_keeps_non_color_semantics()
    {
        var root = FindRepositoryRoot();
        var badge = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Components", "Shared", "StatusBadge.razor"));
        Assert.Contains("status-badge-text", badge, StringComparison.Ordinal);
        Assert.Contains("aria-label", badge, StringComparison.Ordinal);
        Assert.Contains("data-tone", badge, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Could not locate ExItS.slnx.");
    }
}
