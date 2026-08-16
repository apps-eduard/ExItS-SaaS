namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class TaxConfigurationOperationalSetupUiGuardTests
{
    [Fact]
    public void Operational_setup_page_gates_tax_settings_behind_capability_flag()
    {
        var page = File.ReadAllText(Path.Combine(
            MauiProject(),
            "Components",
            "Pages",
            "OperationalSetup",
            "OperationalSetupPage.razor"));

        Assert.Contains("Setup_BusinessDetailsSection", page, StringComparison.Ordinal);
        Assert.Contains("Setup_TaxSettingsSection", page, StringComparison.Ordinal);
        Assert.Contains("_taxConfigurationEnabled", page, StringComparison.Ordinal);
        Assert.Contains("Setup_TaxIncludedInPrice", page, StringComparison.Ordinal);
        Assert.Contains("@if (_taxConfigurationEnabled)", page, StringComparison.Ordinal);
    }

    private static string MauiProject()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "src",
                "Products",
                "PinoyBusinessPOS",
                "ExItS.PinoyBusinessPOS.Maui");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Maui project root was not found.");
    }
}
