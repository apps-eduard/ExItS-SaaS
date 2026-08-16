namespace ExItS.Platform.Admin.UnitTests;

public sealed class OrganizationTaxConfigurationUiGuardTests
{
    [Fact]
    public void Organizations_page_exposes_tax_configuration_controls()
    {
        var text = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "Platform",
            "ExItS.Platform.Admin",
            "Components",
            "Pages",
            "Organizations.razor"));

        Assert.Contains("Organizations_ComplianceTaxConfiguration", text, StringComparison.Ordinal);
        Assert.Contains("Organizations_ComplianceEnableTaxConfiguration", text, StringComparison.Ordinal);
        Assert.Contains("TaxConfigurationEnabled", text, StringComparison.Ordinal);
        Assert.Contains("SetOrganizationTaxConfigurationCapabilityAsync", text, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Could not locate ExItS.slnx.");
    }
}
