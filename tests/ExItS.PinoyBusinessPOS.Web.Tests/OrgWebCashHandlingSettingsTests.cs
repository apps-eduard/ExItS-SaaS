namespace ExItS.PinoyBusinessPOS.Web.Tests;

public sealed class OrgWebCashHandlingSettingsTests
{
    [Fact]
    public void Settings_page_manages_required_optional_policy_and_denominations()
    {
        var settings = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Components",
            "Pages",
            "Settings",
            "Settings.razor"));

        Assert.Contains("Cash handling", settings, StringComparison.Ordinal);
        Assert.Contains("Cash Count Policy", settings, StringComparison.Ordinal);
        Assert.Contains("value=\"Required\"", settings, StringComparison.Ordinal);
        Assert.Contains("value=\"Optional\"", settings, StringComparison.Ordinal);
        Assert.DoesNotContain("value=\"Off\"", settings, StringComparison.Ordinal);
        Assert.Contains("ManageOperationalSetup", settings, StringComparison.Ordinal);
        Assert.Contains("ListCashDenominationsAsync", settings, StringComparison.Ordinal);
        Assert.Contains("ReplaceCashDenominationsAsync", settings, StringComparison.Ordinal);
        Assert.Contains("Add denomination", settings, StringComparison.Ordinal);
        Assert.Contains("AntDesign", File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "ExItS.PinoyBusinessPOS.Web.csproj")), StringComparison.OrdinalIgnoreCase);
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

        throw new InvalidOperationException("Repository root was not found.");
    }
}
