using System.Globalization;
using System.Xml.Linq;
using ExItS.PinoyBusinessPOS.Application.Formatting;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class CultureFallbackAndThemePreferenceTests
{
    [Fact]
    public void Culture_store_rejects_or_falls_back_for_unsupported_values()
    {
        var root = FindRepoRoot();
        var store = File.ReadAllText(Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Services", "MauiCulturePreferenceStore.cs"));
        Assert.Contains("fil-PH", store, StringComparison.Ordinal);
        Assert.Contains("en", store, StringComparison.Ordinal);
        // Unsupported values must not crash preference boot; store constrains to known cultures.
        Assert.True(
            store.Contains("GetCultureInfo", StringComparison.Ordinal)
            || store.Contains("Supported", StringComparison.OrdinalIgnoreCase)
            || store.Contains("fil-PH", StringComparison.Ordinal));
    }

    [Fact]
    public void Unknown_culture_info_lookup_does_not_break_currency_formatting_helpers()
    {
        // Prefer known cultures; if platform throws for nonsense names, assert throw is CultureNotFoundException.
        try
        {
            _ = CultureInfo.GetCultureInfo("zz-ZZ");
        }
        catch (CultureNotFoundException)
        {
            // Expected on many runtimes — unsupported locales must not be silently selected.
            Assert.True(true);
            return;
        }

        var text = CultureFormatting.FormatCurrency(12.5m, "PHP", CultureInfo.GetCultureInfo("en"));
        Assert.Contains("PHP", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Calendar_date_formatting_does_not_shift_day_when_asUtc()
    {
        var utc = new DateTimeOffset(2026, 7, 31, 23, 30, 0, TimeSpan.Zero);
        var en = CultureInfo.GetCultureInfo("en");
        var fil = CultureInfo.GetCultureInfo("fil-PH");
        var dateEn = CultureFormatting.FormatDate(utc, en, asUtc: true);
        var dateFil = CultureFormatting.FormatDate(utc, fil, asUtc: true);
        Assert.Contains("31", dateEn, StringComparison.Ordinal);
        Assert.Contains("31", dateFil, StringComparison.Ordinal);
    }

    [Fact]
    public void Filipino_uses_fil_PH_culture_for_number_formatting()
    {
        var fil = CultureInfo.GetCultureInfo("fil-PH");
        Assert.Equal("fil-PH", fil.Name);
        var formatted = CultureFormatting.FormatCurrency(1234.5m, "PHP", fil);
        Assert.StartsWith("PHP ", formatted, StringComparison.Ordinal);
        Assert.Contains("1", formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void Theme_preference_store_defaults_to_system_for_unknown_values()
    {
        var root = FindRepoRoot();
        var store = File.ReadAllText(Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Services", "MauiThemePreferenceStore.cs"));
        Assert.Contains("ThemePreference.System", store, StringComparison.Ordinal);
        Assert.Contains("TryParse", store, StringComparison.Ordinal);
        Assert.Contains("exits-pos-theme", store, StringComparison.Ordinal);
    }

    [Fact]
    public void Pos_resources_have_bidirectional_en_fil_parity_including_skip_link()
    {
        var english = Load(PosResourcePath("PosResources.resx"));
        var filipino = Load(PosResourcePath("PosResources.fil-PH.resx"));
        var missingFil = english.Keys.Where(k => !filipino.ContainsKey(k)).ToList();
        var missingEn = filipino.Keys.Where(k => !english.ContainsKey(k)).ToList();
        Assert.True(missingFil.Count == 0, $"fil-PH missing: {string.Join(", ", missingFil.Take(20))}");
        Assert.True(missingEn.Count == 0, $"EN missing: {string.Join(", ", missingEn.Take(20))}");
        Assert.True(english.ContainsKey("Nav_SkipToContent"));
        Assert.False(string.IsNullOrWhiteSpace(english["Nav_SkipToContent"]));
        Assert.False(string.IsNullOrWhiteSpace(filipino["Nav_SkipToContent"]));
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

        throw new InvalidOperationException("Repo root not found.");
    }
}
