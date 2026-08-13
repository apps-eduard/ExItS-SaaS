namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class OperationalSetupUiGuardTests
{
    [Fact]
    public void Operational_setup_page_has_first_run_panels_and_sticky_footer()
    {
        var page = File.ReadAllText(Path.Combine(
            MauiProject(),
            "Components",
            "Pages",
            "OperationalSetup",
            "OperationalSetupPage.razor"));
        var css = File.ReadAllText(Path.Combine(MauiProject(), "wwwroot", "app.css"));
        var en = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.fil-PH.resx"));

        Assert.Contains("pos-setup__badge", page, StringComparison.Ordinal);
        Assert.Contains("Setup_FirstRunHint", page, StringComparison.Ordinal);
        Assert.Contains("Setup_StoreSectionHint", page, StringComparison.Ordinal);
        Assert.Contains("Setup_ReceiptSectionHint", page, StringComparison.Ordinal);
        Assert.Contains("Setup_CashHandlingSection", page, StringComparison.Ordinal);
        Assert.Contains("Setup_CashCount", page, StringComparison.Ordinal);
        Assert.Contains("_cashCountMode", page, StringComparison.Ordinal);
        Assert.Contains("CashCountRequired", page, StringComparison.Ordinal);
        Assert.Contains("CashCountOptional", page, StringComparison.Ordinal);
        Assert.DoesNotContain("CashCountOff", page, StringComparison.Ordinal);
        Assert.Contains("Setup_CashCountSnapshotHint", page, StringComparison.Ordinal);
        Assert.Contains("Setup_Denominations", page, StringComparison.Ordinal);
        Assert.Contains("ManageOperationalSetup", page, StringComparison.Ordinal);
        Assert.Contains("pos-setup__footer", page, StringComparison.Ordinal);
        Assert.Contains("SeedDefaultsFromSession", page, StringComparison.Ordinal);
        Assert.Contains("OrganizationDisplayName", page, StringComparison.Ordinal);

        Assert.Contains(".pos-setup__panel", css, StringComparison.Ordinal);
        Assert.Contains(".pos-setup__footer", css, StringComparison.Ordinal);
        Assert.Contains("position: sticky", css, StringComparison.Ordinal);

        Assert.Contains("name=\"Setup_FirstRunHint\"", en, StringComparison.Ordinal);
        Assert.Contains("name=\"Setup_FirstRunHint\"", fil, StringComparison.Ordinal);
        Assert.Contains("name=\"Setup_StoreSectionHint\"", en, StringComparison.Ordinal);
        Assert.Contains("name=\"Setup_ReceiptSectionHint\"", fil, StringComparison.Ordinal);
        Assert.Contains("name=\"Setup_CashHandlingSection\"", en, StringComparison.Ordinal);
        Assert.Contains("name=\"Setup_CashCountHelpOptional\"", fil, StringComparison.Ordinal);
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
