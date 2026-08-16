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
        Assert.Contains("Setup_BusinessDetailsSection", page, StringComparison.Ordinal);
        Assert.Contains("Setup_BusinessDetailsHint", page, StringComparison.Ordinal);
        Assert.Contains("Setup_ReceiptDetailsSection", page, StringComparison.Ordinal);
        Assert.Contains("Setup_ReceiptDetailsHint", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Setup_CashHandlingSection", page, StringComparison.Ordinal);
        Assert.DoesNotContain("pos-setup__policy", page, StringComparison.Ordinal);
        Assert.Contains("_cashCountMode", page, StringComparison.Ordinal);
        Assert.Contains("CashCountRequired", page, StringComparison.Ordinal);
        Assert.DoesNotContain("CashCountOff", page, StringComparison.Ordinal);
        Assert.Contains("ManageOperationalSetup", page, StringComparison.Ordinal);
        Assert.Contains("pos-setup__footer", page, StringComparison.Ordinal);
        Assert.Contains("SeedDefaultsFromSession", page, StringComparison.Ordinal);
        Assert.Contains("OrganizationDisplayName", page, StringComparison.Ordinal);
        Assert.Contains("IsOrgPosFirstTimeSetupIncompleteAsync", page, StringComparison.Ordinal);
        Assert.Contains("Setup_BackToHome", page, StringComparison.Ordinal);
        Assert.Contains("Setup_SaveChanges", page, StringComparison.Ordinal);

        Assert.Contains(".pos-setup__panel", css, StringComparison.Ordinal);
        Assert.Contains(".pos-setup__footer", css, StringComparison.Ordinal);
        Assert.Contains(".pos-setup__policy", css, StringComparison.Ordinal);
        Assert.Contains(".pos-setup__denom-grid", css, StringComparison.Ordinal);
        Assert.Contains("position: sticky", css, StringComparison.Ordinal);

        Assert.Contains("name=\"Setup_FirstRunHint\"", en, StringComparison.Ordinal);
        Assert.Contains("name=\"Setup_FirstRunHint\"", fil, StringComparison.Ordinal);
        Assert.Contains("name=\"Setup_StoreSectionHint\"", en, StringComparison.Ordinal);
        Assert.Contains("name=\"Setup_ReceiptSectionHint\"", fil, StringComparison.Ordinal);
        Assert.Contains("name=\"Setup_CashHandlingSection\"", en, StringComparison.Ordinal);
        Assert.Contains("name=\"Setup_CashCountHelpOptional\"", fil, StringComparison.Ordinal);
        Assert.Contains("name=\"Setup_CashCountRecommended\"", en, StringComparison.Ordinal);
        Assert.Contains("name=\"Setup_AddDenominationShort\"", fil, StringComparison.Ordinal);
        Assert.Contains("name=\"Settings_StoreSectionTitle\"", en, StringComparison.Ordinal);
        Assert.Contains("name=\"Settings_CashHandlingSaved\"", fil, StringComparison.Ordinal);
    }

    [Fact]
    public void Cash_handling_lives_under_settings_for_manage_operational_setup()
    {
        var settings = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Settings.razor"));
        var cash = File.ReadAllText(Path.Combine(
            MauiProject(),
            "Components",
            "Pages",
            "CashHandlingSettings.razor"));
        var policy = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Offline",
            "PosOfflineCapabilityPolicy.cs"));

        Assert.Contains("GoCashHandling", settings, StringComparison.Ordinal);
        Assert.Contains("/settings/cash-handling", settings, StringComparison.Ordinal);
        Assert.Contains("Setup_CashHandlingSection", settings, StringComparison.Ordinal);
        Assert.Contains("ManageOperationalSetup", settings, StringComparison.Ordinal);

        Assert.Contains("@page \"/settings/cash-handling\"", cash, StringComparison.Ordinal);
        Assert.Contains("ManageOperationalSetup", cash, StringComparison.Ordinal);
        Assert.Contains("pos-setup__policy", cash, StringComparison.Ordinal);
        Assert.Contains("pos-setup__denom-grid", cash, StringComparison.Ordinal);
        Assert.Contains("CashCountRequired", cash, StringComparison.Ordinal);
        Assert.Contains("CashCountOptional", cash, StringComparison.Ordinal);
        Assert.DoesNotContain("CashCountOff", cash, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderBack Href=\"/settings\"", cash, StringComparison.Ordinal);

        Assert.Contains("[\"/settings/cash-handling\"] = PosConnectivityRequirement.OnlineRequired", policy, StringComparison.Ordinal);
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
