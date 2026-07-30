using System.Xml.Linq;

namespace ExItS.DesignSystem.Tests;

public sealed class LocalizationResourceCompletenessTests
{
    private static readonly string[] DesignSystemCritical =
    [
        "Empty_DefaultTitle", "Error_Retry", "Loading_Label", "Search_Placeholder",
        "Action_Close", "Action_Dismiss", "Action_ClearSearch", "Action_Save",
        "Action_Cancel", "Action_Confirm", "Empty_NoRecords", "Status_Success"
    ];

    private static readonly string[] ValidationCritical =
    [
        "Required", "InvalidSelection", "InvalidNumber", "InvalidFormat"
    ];

    private static readonly string[] ErrorCritical =
    [
        "Unexpected_Title", "Unauthorized_Title", "Forbidden_Title",
        "Timeout_Title", "Offline_Title", "Unavailable_Title", "PreferenceSaveFailed"
    ];

    [Theory]
    [InlineData("DesignSystemResources")]
    [InlineData("ValidationResources")]
    [InlineData("ErrorResources")]
    public void English_and_filipino_resource_sets_have_matching_non_empty_keys(string baseName)
    {
        var english = Load(ResourcePath($"{baseName}.resx"));
        var filipino = Load(ResourcePath($"{baseName}.fil-PH.resx"));

        Assert.NotEmpty(english);
        var missingFil = english.Keys.Where(k => !filipino.ContainsKey(k)).ToList();
        var missingEn = filipino.Keys.Where(k => !english.ContainsKey(k)).ToList();
        Assert.True(missingFil.Count == 0, $"fil-PH missing: {string.Join(", ", missingFil)}");
        Assert.True(missingEn.Count == 0, $"English missing: {string.Join(", ", missingEn)}");

        foreach (var (key, value) in english.Concat(filipino))
        {
            Assert.False(string.IsNullOrWhiteSpace(value), $"Blank value for '{key}' in {baseName}");
        }
    }

    [Fact]
    public void DesignSystem_critical_keys_exist_in_both_cultures()
    {
        AssertCritical("DesignSystemResources", DesignSystemCritical);
    }

    [Fact]
    public void Validation_critical_keys_exist_in_both_cultures()
    {
        AssertCritical("ValidationResources", ValidationCritical);
    }

    [Fact]
    public void Error_critical_keys_exist_in_both_cultures()
    {
        AssertCritical("ErrorResources", ErrorCritical);
    }

    [Fact]
    public void Shared_components_do_not_embed_hard_coded_english_action_defaults()
    {
        var root = FindRepoRoot();
        var files = new[]
        {
            Path.Combine(root, "src", "Shared", "ExItS.DesignSystem", "Components", "Feedback", "SearchBox.razor"),
            Path.Combine(root, "src", "Shared", "ExItS.DesignSystem", "Components", "Overlay", "Dialog.razor"),
            Path.Combine(root, "src", "Shared", "ExItS.DesignSystem", "Components", "Overlay", "Drawer.razor"),
            Path.Combine(root, "src", "Shared", "ExItS.DesignSystem", "Components", "Overlay", "ToastHost.razor"),
        };

        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("= \"Close\"", text, StringComparison.Ordinal);
            Assert.DoesNotContain("= \"Dismiss\"", text, StringComparison.Ordinal);
            Assert.DoesNotContain("= \"Clear search\"", text, StringComparison.Ordinal);
            Assert.Contains("IStringLocalizer<DesignSystemResources>", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void DesignSystem_resources_contain_no_pos_product_terms()
    {
        foreach (var file in new[] { "DesignSystemResources.resx", "ValidationResources.resx", "ErrorResources.resx" })
        {
            var text = File.ReadAllText(ResourcePath(file));
            Assert.DoesNotContain("Utang", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("PinoyBusinessPOS", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("GCash", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static void AssertCritical(string baseName, IEnumerable<string> keys)
    {
        var english = Load(ResourcePath($"{baseName}.resx"));
        var filipino = Load(ResourcePath($"{baseName}.fil-PH.resx"));
        foreach (var key in keys)
        {
            Assert.True(english.ContainsKey(key), $"EN missing {key}");
            Assert.True(filipino.ContainsKey(key), $"fil-PH missing {key}");
            Assert.False(string.IsNullOrWhiteSpace(english[key]));
            Assert.False(string.IsNullOrWhiteSpace(filipino[key]));
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

    private static string ResourcePath(string fileName) =>
        Path.Combine(FindRepoRoot(), "src", "Shared", "ExItS.DesignSystem", "Localization", fileName);

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
