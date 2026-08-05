namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class OrgSummaryUiGuardTests
{
    [Fact]
    public void Org_summary_is_compact_dashboard_without_duplicate_context_chrome()
    {
        var page = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Organization", "OrgSummary.razor"));
        var en = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.fil-PH.resx"));
        var topBar = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "ShellOrganizationIdentity.razor"));
        var accountMenu = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "ShellAccountMenu.razor"));
        var switcher = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "AccountContextSwitcher.razor"));

        Assert.Contains("@page \"/org\"", page, StringComparison.Ordinal);
        Assert.Contains("pos-org__title", page, StringComparison.Ordinal);
        Assert.Contains("Org_SummaryTitle", page, StringComparison.Ordinal);
        Assert.Contains("Org_AccessSection", page, StringComparison.Ordinal);
        Assert.Contains("Org_OrganizationLabel", page, StringComparison.Ordinal);
        Assert.Contains("Org_Subscription", page, StringComparison.Ordinal);
        Assert.Contains("Org_Entitlement", page, StringComparison.Ordinal);
        Assert.Contains("Org_PosAccess", page, StringComparison.Ordinal);
        Assert.Contains("pos-org__status-list", page, StringComparison.Ordinal);
        Assert.Contains("pos-org__chip", page, StringComparison.Ordinal);
        Assert.Contains("Org_EnterPos", page, StringComparison.Ordinal);
        Assert.Contains("ButtonVariant.Primary", page, StringComparison.Ordinal);
        Assert.Contains("EnterPosAsync", page, StringComparison.Ordinal);
        Assert.Contains("_enteringPos", page, StringComparison.Ordinal);
        Assert.Contains("EnablePosAsync", page, StringComparison.Ordinal);
        Assert.Contains("pos-org__nav-row", page, StringComparison.Ordinal);
        Assert.Contains("Org_ProfileLink", page, StringComparison.Ordinal);
        Assert.Contains("Org_StaffLink", page, StringComparison.Ordinal);
        Assert.Contains("Org_SubscriptionLink", page, StringComparison.Ordinal);
        Assert.Contains("Personal_MyQrLink", page, StringComparison.Ordinal);
        Assert.Contains("/org/profile", page, StringComparison.Ordinal);
        Assert.Contains("/org/staff", page, StringComparison.Ordinal);
        Assert.Contains("/org/subscription", page, StringComparison.Ordinal);
        Assert.Contains("/personal/my-qr", page, StringComparison.Ordinal);
        Assert.Contains("Org_WebAdminReminder", page, StringComparison.Ordinal);
        Assert.Contains("pos-org__footnote", page, StringComparison.Ordinal);
        Assert.Contains("_isOrganizationOwner", page, StringComparison.Ordinal);
        Assert.Contains("ListEligibleOrganizationsAsync", page, StringComparison.Ordinal);
        Assert.Contains("ResolveStartRouteAsync", page, StringComparison.Ordinal);

        Assert.DoesNotContain("Personal_BackHome", page, StringComparison.Ordinal);
        Assert.DoesNotContain("GoPersonal", page, StringComparison.Ordinal);
        Assert.DoesNotContain("AccountContextSwitcher", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Context_SwitcherTitle", page, StringComparison.Ordinal);
        Assert.DoesNotContain("PageHeader", page, StringComparison.Ordinal);
        Assert.DoesNotContain("ButtonVariant.Secondary\" OnClick=\"GoProfile\"", page, StringComparison.Ordinal);
        Assert.DoesNotContain("InlineMessage Tone=\"InlineMessageTone.Info\">@L[\"Org_WebAdminReminder\"]", page, StringComparison.Ordinal);

        // Top-bar / shared switcher remain the context-switching surfaces.
        Assert.Contains("OrganizationDisplayName", topBar, StringComparison.Ordinal);
        Assert.Contains("ShellAccountMenu", File.ReadAllText(Path.Combine(MauiProject(), "Components", "Layout", "PosShell.razor")), StringComparison.Ordinal);
        Assert.Contains("IconName=\"more\"", accountMenu, StringComparison.Ordinal);
        Assert.Contains("SwitchToPersonalAsync", switcher, StringComparison.Ordinal);
        Assert.Contains("SelectOrganizationAsync", switcher, StringComparison.Ordinal);
        Assert.Contains("ListEligibleOrganizationsAsync", switcher, StringComparison.Ordinal);

        foreach (var key in new[] { "Org_AccessSection", "Org_OrganizationLabel" })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    private static string MauiProject()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "Products", "PinoyBusinessPOS",
                "ExItS.PinoyBusinessPOS.Maui");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate PinoyBusinessPOS.Maui project.");
    }
}
