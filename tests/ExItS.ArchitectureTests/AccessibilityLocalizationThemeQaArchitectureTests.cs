using System.Text.RegularExpressions;

namespace ExItS.ArchitectureTests;

/// <summary>P9-WP04: accessibility, localization, and theme QA architecture guards.</summary>
public sealed class AccessibilityLocalizationThemeQaArchitectureTests
{
    private static string RepoRoot()
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

    [Fact]
    public void Phase_marker_is_accessibility_localization_theme_qa()
    {
        var root = RepoRoot();
        var pos = File.ReadAllText(Path.Combine(root, "src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Api/Program.cs"));
        var platform = File.ReadAllText(Path.Combine(root, "src/Platform/ExItS.Platform.Api/Program.cs"));
        Assert.Contains("P10-WP08-phase-10-closeout", pos, StringComparison.Ordinal);
        Assert.Contains("P10-WP08-phase-10-closeout", platform, StringComparison.Ordinal);
    }

    [Fact]
    public void Skip_links_exist_for_admin_and_pos_shells()
    {
        var root = RepoRoot();
        var admin = File.ReadAllText(Path.Combine(root, "src/Platform/ExItS.Platform.Admin/Components/Layout/MainLayout.razor"));
        var pos = File.ReadAllText(Path.Combine(root, "src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/Components/Layout/PosShell.razor"));
        Assert.Contains("skip-link", admin, StringComparison.Ordinal);
        Assert.Contains("Nav_SkipToContent", admin, StringComparison.Ordinal);
        Assert.Contains("skip-link", pos, StringComparison.Ordinal);
        Assert.Contains("Nav_SkipToContent", pos, StringComparison.Ordinal);
        Assert.Contains("id=\"main-content\"", admin, StringComparison.Ordinal);
        Assert.Contains("id=\"pos-main\"", pos, StringComparison.Ordinal);
    }

    [Fact]
    public void Dialogs_use_aria_labelledby_and_escape_handling()
    {
        var root = RepoRoot();
        var dialog = File.ReadAllText(Path.Combine(root, "src/Shared/ExItS.DesignSystem/Components/Overlay/Dialog.razor"));
        var confirm = File.ReadAllText(Path.Combine(root, "src/Shared/ExItS.DesignSystem/Components/Overlay/ConfirmDialog.razor"));
        Assert.Contains("aria-labelledby", dialog, StringComparison.Ordinal);
        Assert.Contains("role=\"dialog\"", dialog, StringComparison.Ordinal);
        Assert.Contains("Escape", dialog, StringComparison.Ordinal);
        Assert.Contains("aria-labelledby", confirm, StringComparison.Ordinal);
        Assert.Contains("role=\"alertdialog\"", confirm, StringComparison.Ordinal);
        Assert.Contains("aria-describedby", confirm, StringComparison.Ordinal);
    }

    [Fact]
    public void Status_presentation_is_not_color_alone()
    {
        var root = RepoRoot();
        var badge = File.ReadAllText(Path.Combine(root, "src/Platform/ExItS.Platform.Admin/Components/Shared/StatusBadge.razor"));
        var css = File.ReadAllText(Path.Combine(root, "src/Platform/ExItS.Platform.Admin/wwwroot/app.css"));
        Assert.Contains("DisplayValue", badge, StringComparison.Ordinal);
        Assert.Contains("data-status", badge, StringComparison.Ordinal);
        Assert.Contains(".status-badge::before", css, StringComparison.Ordinal);
        Assert.Contains("exds-badge", File.ReadAllText(Path.Combine(root, "src/Shared/ExItS.DesignSystem/Components/Primitives/Badge.razor")), StringComparison.Ordinal);
    }

    [Fact]
    public void Themes_define_light_dark_and_reduced_motion()
    {
        var root = RepoRoot();
        var ds = File.ReadAllText(Path.Combine(root, "src/Shared/ExItS.DesignSystem/wwwroot/exits-design-system.css"));
        var admin = File.ReadAllText(Path.Combine(root, "src/Platform/ExItS.Platform.Admin/wwwroot/app.css"));
        Assert.Contains("[data-theme=\"dark\"]", ds, StringComparison.Ordinal);
        Assert.Contains("prefers-color-scheme", ds, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion", ds, StringComparison.Ordinal);
        Assert.Contains("[data-theme=\"dark\"]", admin, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion", admin, StringComparison.Ordinal);
        Assert.Contains("--exits-touch-target-min: 2.75rem", ds, StringComparison.Ordinal);
    }

    [Fact]
    public void Admin_pages_do_not_hard_code_page_header_english_titles()
    {
        var root = RepoRoot();
        var pages = Path.Combine(root, "src/Platform/ExItS.Platform.Admin/Components/Pages");
        foreach (var file in Directory.EnumerateFiles(pages, "*.razor"))
        {
            var name = Path.GetFileName(file);
            if (name is "Home.razor")
            {
                continue;
            }

            var text = File.ReadAllText(file);
            Assert.DoesNotMatch(@"PageHeader\s+Title=""(?!@L\[)", text);
            if (Regex.IsMatch(text, @"<PageHeader\b"))
            {
                Assert.Contains("Title=\"@L[", text, StringComparison.Ordinal);
            }
        }
    }

    [Fact]
    public void No_healthcare_ui_references_in_admin_or_pos_maui()
    {
        var root = RepoRoot();
        var paths = new[]
        {
            Path.Combine(root, "src/Platform/ExItS.Platform.Admin"),
            Path.Combine(root, "src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui")
        };
        foreach (var path in paths)
        {
            foreach (var file in Directory.EnumerateFiles(path, "*.razor", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                Assert.DoesNotContain("HealthCare/", text, StringComparison.Ordinal);
                Assert.DoesNotMatch(new Regex(@"\bPHI\b"), text);
            }
        }
    }

    [Fact]
    public void Rtl_is_not_claimed_in_theme_boot()
    {
        var root = RepoRoot();
        var adminBoot = File.ReadAllText(Path.Combine(root, "src/Platform/ExItS.Platform.Admin/wwwroot/theme-boot.js"));
        var posBoot = File.ReadAllText(Path.Combine(root, "src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui/wwwroot/theme-boot.js"));
        Assert.DoesNotContain("dir=\"rtl\"", adminBoot, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dir=\"rtl\"", posBoot, StringComparison.OrdinalIgnoreCase);
    }
}
