using System.Globalization;
using System.Xml.Linq;
using ExItS.PinoyBusinessPOS.Application.Formatting;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class LocalizationAndFormattingTests
{
    private static readonly string[] PosCritical =
    [
        "Nav_Home", "Nav_Products", "Nav_Sales", "Nav_Customers", "Nav_More", "Nav_Settings",
        "Settings_ThemeLabel", "Settings_DensityLabel", "Settings_LanguageLabel",
        "Settings_Language_English", "Settings_Language_Filipino",
        "Deferred_Title", "NotFound_Title", "Preference_SaveFailed",
        "Common_Retry", "Common_Connected", "Api_TestConnection",
        "DevShowcase_Title", "DevShowcase_UnavailableTitle",
        "SignIn_Title", "Auth_Logout", "Access_DeniedTitle", "Welcome_Title",
        "Nav_SkipToContent"
    ];

    [Fact]
    public void Pos_english_and_filipino_resources_match_and_cover_critical_keys()
    {
        var english = Load(PosResourcePath("PosResources.resx"));
        var filipino = Load(PosResourcePath("PosResources.fil-PH.resx"));

        var missingFil = english.Keys.Where(k => !filipino.ContainsKey(k)).ToList();
        Assert.True(missingFil.Count == 0, $"fil-PH missing: {string.Join(", ", missingFil)}");

        foreach (var key in PosCritical)
        {
            Assert.True(english.ContainsKey(key), $"EN missing {key}");
            Assert.True(filipino.ContainsKey(key), $"fil-PH missing {key}");
            Assert.False(string.IsNullOrWhiteSpace(english[key]));
            Assert.False(string.IsNullOrWhiteSpace(filipino[key]));
        }

        Assert.Equal("Tagalog", english["Settings_Language_Filipino"]);
        Assert.Equal("Tagalog", filipino["Settings_Language_Filipino"]);
    }

    [Fact]
    public void Maui_pages_do_not_hard_code_user_facing_english_sentences()
    {
        var pages = Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Components", "Pages");
        foreach (var file in Directory.EnumerateFiles(pages, "*.razor", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Sorry, the content you are looking for", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Back to home\"", text, StringComparison.Ordinal);
            Assert.DoesNotContain("?? \"OK\"", text, StringComparison.Ordinal);
        }

        var notFound = File.ReadAllText(Path.Combine(pages, "NotFound.razor"));
        Assert.Contains("NotFound_Title", notFound, StringComparison.Ordinal);
        Assert.Contains("IStringLocalizer<PosResources>", notFound, StringComparison.Ordinal);
    }

    [Fact]
    public void Culture_preference_and_boot_js_support_fil_PH()
    {
        var boot = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "wwwroot", "theme-boot.js"));
        Assert.Contains("fil-PH", boot, StringComparison.Ordinal);
        Assert.Contains("applyCulture", boot, StringComparison.Ordinal);

        var cultureStore = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Services", "MauiCulturePreferenceStore.cs"));
        Assert.Contains("fil-PH", cultureStore, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_and_settings_reference_localized_nav_and_density_keys()
    {
        var root = FindRepoRoot();
        var shell = File.ReadAllText(Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Components", "Layout", "PosShell.razor"));
        Assert.Contains("Nav_Primary", shell, StringComparison.Ordinal);
        Assert.Contains("Nav_Home", shell, StringComparison.Ordinal);

        var settings = File.ReadAllText(Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Components", "Pages", "Settings.razor"));
        Assert.Contains("Settings_Language_Filipino", settings, StringComparison.Ordinal);
        Assert.Contains("ApiLocalizer", settings, StringComparison.Ordinal);
        Assert.Contains("PreferenceSaveFailed", settings, StringComparison.Ordinal);
    }

    [Fact]
    public void App_css_supports_long_filipino_nav_label_wrapping()
    {
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "wwwroot", "app.css"));
        Assert.Contains("pos-nav-item__label", css, StringComparison.Ordinal);
        Assert.Contains("overflow-wrap", css, StringComparison.Ordinal);
        Assert.Contains("-webkit-line-clamp", css, StringComparison.Ordinal);
    }

    [Fact]
    public void CultureFormatting_formats_without_mutating_values()
    {
        var en = CultureInfo.GetCultureInfo("en");
        var fil = CultureInfo.GetCultureInfo("fil-PH");
        var utc = new DateTimeOffset(2026, 7, 30, 12, 30, 0, TimeSpan.Zero);

        var dateEn = CultureFormatting.FormatDate(utc, en, asUtc: true);
        var dateFil = CultureFormatting.FormatDate(utc, fil, asUtc: true);
        Assert.False(string.IsNullOrWhiteSpace(dateEn));
        Assert.False(string.IsNullOrWhiteSpace(dateFil));

        var stamped = CultureFormatting.FormatDateTime(utc, en, asUtc: true, includeZoneLabel: true);
        Assert.Contains(CultureFormatting.UtcLabel, stamped, StringComparison.Ordinal);

        const decimal amount = 1234.5m;
        var currency = CultureFormatting.FormatCurrency(amount, "PHP", en);
        Assert.StartsWith("PHP ", currency, StringComparison.Ordinal);
        Assert.Equal(1234.5m, amount);

        var percent = CultureFormatting.FormatPercent(0.15m, en, decimals: 0);
        Assert.Contains("15", percent, StringComparison.Ordinal);

        var number = CultureFormatting.FormatNumber(1000m, en, "N0");
        Assert.False(string.IsNullOrWhiteSpace(number));
    }

    [Fact]
    public void No_machine_translation_package_references()
    {
        var maui = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "ExItS.PinoyBusinessPOS.Maui.csproj"));
        var design = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Shared", "ExItS.DesignSystem",
            "ExItS.DesignSystem.csproj"));
        foreach (var text in new[] { maui, design })
        {
            Assert.DoesNotContain("Azure.AI.Translation", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Google.Cloud.Translation", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DeepL", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static Dictionary<string, string> Load(string path)
    {
        Assert.True(File.Exists(path), path);
        return XDocument.Load(path).Root!
            .Elements("data")
            .ToDictionary(
                el => el.Attribute("name")!.Value,
                el => el.Element("value")?.Value ?? string.Empty,
                StringComparer.Ordinal);
    }

    private static string PosResourcePath(string fileName) =>
        Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Localization", fileName);

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
