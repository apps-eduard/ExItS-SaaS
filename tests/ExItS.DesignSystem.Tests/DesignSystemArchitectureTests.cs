using System.Reflection;
using System.Text.RegularExpressions;
using ExItS.DesignSystem.Abstractions;
using ExItS.DesignSystem.Localization;

namespace ExItS.DesignSystem.Tests;

public sealed class DesignSystemArchitectureTests
{
    [Fact]
    public void DesignSystem_assembly_has_no_infrastructure_ef_npgsql_maui_or_product_deps()
    {
        var names = typeof(DesignSystemResources).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(names, n => n.Contains("Infrastructure", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Contains("Npgsql", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Contains("Microsoft.Maui", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Contains("PinoyBusinessPOS", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Contains("HealthCare", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Contains("AntDesign", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(names, n => n.Contains("Tailwind", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Theme_and_culture_preference_stores_are_abstractions_only()
    {
        Assert.True(typeof(IThemePreferenceStore).IsInterface);
        Assert.True(typeof(ICulturePreferenceStore).IsInterface);
        Assert.Equal(new[] { ThemePreference.System, ThemePreference.Light, ThemePreference.Dark },
            Enum.GetValues<ThemePreference>());
    }

    [Fact]
    public void Css_defines_semantic_tokens_and_theme_markers()
    {
        var css = ReadDesignSystemCss();
        foreach (var token in new[]
                 {
                     "--exits-bg", "--exits-surface", "--exits-text", "--exits-primary",
                     "--exits-danger", "--exits-shadow-sm", "--exits-radius-md", "--exits-motion-fast"
                 })
        {
            Assert.Contains(token, css, StringComparison.Ordinal);
        }

        Assert.Contains("[data-theme=\"dark\"]", css, StringComparison.Ordinal);
        Assert.Contains("[data-theme=\"system\"]", css, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion", css, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("#512BD4", css, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("@import url(\"bootstrap", css, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("bootstrap.min.css", css, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Required_foundation_components_exist()
    {
        var root = FindRepoRoot();
        var components = Path.Combine(root, "src", "Shared", "ExItS.DesignSystem", "Components");
        var required = new[]
        {
            "Primitives/Button.razor", "Primitives/IconButton.razor", "Primitives/TextInput.razor",
            "Primitives/Select.razor", "Primitives/Switch.razor", "Primitives/Label.razor",
            "Primitives/Badge.razor", "Primitives/Avatar.razor", "Primitives/Spinner.razor",
            "Primitives/Skeleton.razor", "Primitives/Divider.razor",
            "Layout/Stack.razor", "Layout/Grid.razor", "Layout/Card.razor", "Layout/Surface.razor",
            "Layout/Page.razor", "Layout/Section.razor", "Layout/Toolbar.razor", "Layout/PageHeader.razor",
            "Overlay/Tabs.razor", "Overlay/Drawer.razor", "Overlay/Dialog.razor", "Overlay/ToastHost.razor",
            "Overlay/Alert.razor", "Overlay/LoadingOverlay.razor",
            "Feedback/EmptyState.razor", "Feedback/ErrorState.razor", "Feedback/SearchBox.razor"
        };

        foreach (var relative in required)
        {
            Assert.True(File.Exists(Path.Combine(components, relative)), $"Missing component: {relative}");
        }
    }

    [Fact]
    public void Components_do_not_call_maui_or_browser_storage_apis()
    {
        var root = FindRepoRoot();
        var componentsDir = Path.Combine(root, "src", "Shared", "ExItS.DesignSystem");
        var forbidden = new[]
        {
            "Preferences.Default", "SecureStorage.", "window.localStorage",
            "Microsoft.Maui.Storage", "DbContext", "EntityFrameworkCore", "Npgsql."
        };

        foreach (var file in Directory.EnumerateFiles(componentsDir, "*.*", SearchOption.AllDirectories)
                     .Where(f => f.EndsWith(".razor", StringComparison.OrdinalIgnoreCase)
                                 || f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var text = File.ReadAllText(file);
            foreach (var phrase in forbidden)
            {
                Assert.DoesNotContain(phrase, text, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Fact]
    public void Localization_resources_exist_for_english_and_tagalog()
    {
        var root = FindRepoRoot();
        var loc = Path.Combine(root, "src", "Shared", "ExItS.DesignSystem", "Localization");
        Assert.True(File.Exists(Path.Combine(loc, "DesignSystemResources.resx")));
        Assert.True(File.Exists(Path.Combine(loc, "DesignSystemResources.fil-PH.resx")));
        var en = File.ReadAllText(Path.Combine(loc, "DesignSystemResources.resx"));
        Assert.Contains("Empty_DefaultTitle", en, StringComparison.Ordinal);
        Assert.Contains("Error_Retry", en, StringComparison.Ordinal);
    }

    [Fact]
    public void Button_supports_loading_and_disabled_states()
    {
        var button = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Shared", "ExItS.DesignSystem",
            "Components", "Primitives", "Button.razor"));
        Assert.Contains("IsLoading", button, StringComparison.Ordinal);
        Assert.Contains("Disabled", button, StringComparison.Ordinal);
        Assert.Contains("exds-", button, StringComparison.Ordinal);
    }

    private static string ReadDesignSystemCss()
    {
        var path = Path.Combine(FindRepoRoot(), "src", "Shared", "ExItS.DesignSystem", "wwwroot", "exits-design-system.css");
        Assert.True(File.Exists(path));
        return File.ReadAllText(path);
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
