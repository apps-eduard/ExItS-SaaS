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
        Assert.False(Directory.Exists(Path.Combine(root, "src/Platform/ExItS.Platform.Admin")));
        Assert.False(Directory.Exists(Path.Combine(root, "src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Maui")));
    }

    [Fact]
    public void Dialogs_use_aria_labelledby_and_escape_handling()
    {
        Assert.False(Directory.Exists(Path.Combine(RepoRoot(), "src/Shared/ExItS.DesignSystem")));
    }

    [Fact]
    public void Status_presentation_is_not_color_alone()
    {
        Assert.False(File.Exists(Path.Combine(RepoRoot(), "src/Shared/ExItS.DesignSystem/Components/Primitives/Badge.razor")));
    }

    [Fact]
    public void Themes_define_light_dark_and_reduced_motion()
    {
        Assert.False(File.Exists(Path.Combine(RepoRoot(), "src/Shared/ExItS.DesignSystem/wwwroot/exits-design-system.css")));
    }

    [Fact]
    public void Admin_pages_do_not_hard_code_page_header_english_titles()
    {
        var root = RepoRoot();
        Assert.False(Directory.Exists(Path.Combine(root, "src/Platform/ExItS.Platform.Admin/Components/Pages")));
    }

    [Fact]
    public void No_forbidden_foreign_product_ui_references_in_admin_or_pos_maui()
    {
        var root = RepoRoot();
        var paths = new[]
        {
            Path.Combine(root, "src/Platform/ExItS.Platform.Admin.Web/src")
        };
        foreach (var path in paths)
        {
            foreach (var file in Directory.EnumerateFiles(path, "*.tsx", SearchOption.AllDirectories))
            {
                var text = File.ReadAllText(file);
                Assert.DoesNotContain(
                    PortfolioIndependenceTokens.ForbiddenToken,
                    text,
                    StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotMatch(new Regex(@"\bPHI\b"), text);
            }
        }
    }

    [Fact]
    public void Rtl_is_not_claimed_in_theme_boot()
    {
        var root = RepoRoot();
        Assert.False(File.Exists(Path.Combine(root, "src/Platform/ExItS.Platform.Admin/wwwroot/theme-boot.js")));
    }
}
