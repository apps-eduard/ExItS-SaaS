using System.Globalization;
using System.Text.RegularExpressions;
using ExItS.DesignSystem.Abstractions;

namespace ExItS.DesignSystem.Tests;

/// <summary>P9-WP04: theme token contrast and a11y token guards (engineering checks, not certification).</summary>
public sealed class ThemeContrastAndAccessibilityTests
{
    private static string CssPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Shared", "ExItS.DesignSystem", "wwwroot", "exits-design-system.css");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("DesignSystem CSS not found.");
    }

    [Fact]
    public void Light_theme_text_on_surface_meets_wcag_aa_contrast()
    {
        var tokens = ParseRootTokens(File.ReadAllText(CssPath()));
        var ratio = ContrastRatio(ParseHex(tokens["--exits-text"]), ParseHex(tokens["--exits-surface"]));
        Assert.True(ratio >= 4.5, $"Light text/surface contrast {ratio:F2} < 4.5");
    }

    [Fact]
    public void Dark_theme_text_on_surface_meets_wcag_aa_contrast()
    {
        var css = File.ReadAllText(CssPath());
        var darkBlock = Regex.Match(
            css,
            @"\[data-theme=""dark""\]\s*\{(?<body>.*?)\}(?=\s*/\*|\s*@media|\s*\[data-theme|\s*$)",
            RegexOptions.Singleline);
        Assert.True(darkBlock.Success, "Dark theme token block not found.");
        var tokens = ParseTokenBlock(darkBlock.Groups["body"].Value);
        var ratio = ContrastRatio(ParseHex(tokens["--exits-text"]), ParseHex(tokens["--exits-surface"]));
        Assert.True(ratio >= 4.5, $"Dark text/surface contrast {ratio:F2} < 4.5");
    }

    [Fact]
    public void Focus_ring_token_is_distinct_from_surface()
    {
        var tokens = ParseRootTokens(File.ReadAllText(CssPath()));
        Assert.NotEqual(tokens["--exits-focus"], tokens["--exits-surface"]);
        Assert.Contains(":focus-visible", File.ReadAllText(CssPath()), StringComparison.Ordinal);
    }

    [Fact]
    public void MoneyDisplay_exposes_accessible_label()
    {
        var dir = new DirectoryInfo(Path.GetDirectoryName(CssPath())!).Parent!;
        var money = File.ReadAllText(Path.Combine(dir.FullName, "Components", "Data", "MoneyDisplay.razor"));
        Assert.Contains("aria-label", money, StringComparison.Ordinal);
        Assert.Contains("AccessibleLabel", money, StringComparison.Ordinal);
        Assert.Contains("CurrencyCode", money, StringComparison.Ordinal);
    }

    [Fact]
    public void ThemePreference_enum_covers_system_light_dark()
    {
        Assert.Equal(new[] { ThemePreference.System, ThemePreference.Light, ThemePreference.Dark },
            Enum.GetValues<ThemePreference>());
    }

    private static Dictionary<string, string> ParseRootTokens(string css)
    {
        var match = Regex.Match(
            css,
            @":root,\s*\[data-theme=""light""\]\s*\{(?<body>.*?)\}(?=\s*/\*|\s*\[data-theme=""dark""\]|\s*@media|\s*$)",
            RegexOptions.Singleline);
        Assert.True(match.Success, "Light theme token block not found.");
        return ParseTokenBlock(match.Groups["body"].Value);
    }

    private static Dictionary<string, string> ParseTokenBlock(string body)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Match m in Regex.Matches(body, @"(?<name>--exits-[\w-]+)\s*:\s*(?<value>#[0-9A-Fa-f]{3,8})\s*;"))
        {
            map[m.Groups["name"].Value] = m.Groups["value"].Value;
        }

        return map;
    }

    private static (double R, double G, double B) ParseHex(string hex)
    {
        hex = hex.TrimStart('#');
        if (hex.Length == 3)
        {
            hex = string.Concat(hex.Select(c => $"{c}{c}"));
        }

        var value = int.Parse(hex[..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        return (
            ((value >> 16) & 0xFF) / 255.0,
            ((value >> 8) & 0xFF) / 255.0,
            (value & 0xFF) / 255.0);
    }

    private static double RelativeLuminance((double R, double G, double B) c)
    {
        static double Channel(double v) => v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        return 0.2126 * Channel(c.R) + 0.7152 * Channel(c.G) + 0.0722 * Channel(c.B);
    }

    private static double ContrastRatio((double R, double G, double B) a, (double R, double G, double B) b)
    {
        var l1 = RelativeLuminance(a);
        var l2 = RelativeLuminance(b);
        var lighter = Math.Max(l1, l2);
        var darker = Math.Min(l1, l2);
        return (lighter + 0.05) / (darker + 0.05);
    }
}
