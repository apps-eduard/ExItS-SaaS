using System.Xml.Linq;

namespace ExItS.Platform.Admin.UnitTests;

public sealed class LocalizationResourceTests
{
    private static readonly string[] NavCriticalKeys =
    [
        "Brand_Name",
        "Nav_Dashboard",
        "Nav_Products",
        "Nav_Organizations",
        "Nav_Subscriptions",
        "Nav_Payments",
        "Nav_Users",
        "Nav_Entitlements",
        "Nav_Audit",
        "Common_Save",
        "Common_Cancel",
        "Common_Confirm",
        "Common_Create",
        "Common_Search",
        "Common_Filter",
        "State_Loading",
        "State_Empty",
        "State_Error",
        "Theme_Label",
        "Language_Label",
        "Unauthorized_Title",
        "Unauthorized_Message",
        "Banner_DevSecurityCompact",
        "Nav_SkipToContent",
        "Users_Title",
        "Organizations_Title",
        "Subscriptions_Title",
        "Payments_Title",
        "Common_ActionFailed",
        "Status_Active",
        "Status_Voided",
        "Dashboard_Title",
        "Dashboard_Section_Primary",
        "Report_FiltersAria",
        "Form_ActionsAria",
        "OrgProductAccess_RevokeConfirmMessage",
        "Common_EmDash"
    ];

    [Fact]
    public void English_resource_file_exists_and_has_no_blank_nav_critical_values()
    {
        var path = ResourcePath("AdminResources.resx");
        Assert.True(File.Exists(path), $"Missing {path}");

        var values = LoadResourceValues(path);
        foreach (var key in NavCriticalKeys)
        {
            Assert.True(values.ContainsKey(key), $"English resource is missing key '{key}'.");
            Assert.False(string.IsNullOrWhiteSpace(values[key]), $"English resource has a blank value for key '{key}'.");
        }
    }

    [Fact]
    public void Filipino_resource_file_exists_and_has_no_blank_nav_critical_values()
    {
        var path = ResourcePath("AdminResources.fil-PH.resx");
        Assert.True(File.Exists(path), $"Missing {path}");

        var values = LoadResourceValues(path);
        foreach (var key in NavCriticalKeys)
        {
            Assert.True(values.ContainsKey(key), $"Filipino (fil-PH) resource is missing key '{key}'.");
            Assert.False(string.IsNullOrWhiteSpace(values[key]), $"Filipino (fil-PH) resource has a blank value for key '{key}'.");
        }
    }

    [Fact]
    public void Every_english_key_has_a_corresponding_filipino_key_english_fallback_is_complete()
    {
        var english = LoadResourceValues(ResourcePath("AdminResources.resx"));
        var filipino = LoadResourceValues(ResourcePath("AdminResources.fil-PH.resx"));

        var missing = english.Keys.Where(key => !filipino.ContainsKey(key)).ToList();
        Assert.True(missing.Count == 0, $"Filipino (fil-PH) resource is missing keys present in English: {string.Join(", ", missing)}");
    }

    [Fact]
    public void Resource_files_do_not_contain_empty_data_values()
    {
        foreach (var file in new[] { "AdminResources.resx", "AdminResources.fil-PH.resx" })
        {
            var values = LoadResourceValues(ResourcePath(file));
            foreach (var (key, value) in values)
            {
                Assert.False(string.IsNullOrWhiteSpace(value), $"{file} contains an empty value for key '{key}'.");
            }
        }
    }

    private static Dictionary<string, string> LoadResourceValues(string path)
    {
        var document = XDocument.Load(path);
        return document.Root!
            .Elements("data")
            .ToDictionary(
                el => el.Attribute("name")!.Value,
                el => el.Element("value")?.Value ?? string.Empty,
                StringComparer.Ordinal);
    }

    private static string ResourcePath(string fileName)
    {
        var root = FindRepositoryRoot();
        return Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Localization", fileName);
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

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
