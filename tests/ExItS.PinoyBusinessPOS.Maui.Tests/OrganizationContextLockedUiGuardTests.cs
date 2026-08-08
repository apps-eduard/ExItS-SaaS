namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class OrganizationContextLockedUiGuardTests
{
    [Fact]
    public void StoreHeader_and_Settings_hide_org_switch_when_context_locked()
    {
        var header = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "StoreHeader.razor"));
        var settings = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Settings.razor"));

        Assert.Contains("OrganizationContextLocked", header, StringComparison.Ordinal);
        Assert.Contains(
            "ShowSwitchOrganization && CurrentUser.Session?.OrganizationContextLocked != true",
            header,
            StringComparison.Ordinal);
        Assert.Contains(
            "ShowPersonalHome && CurrentUser.Session?.OrganizationContextLocked != true",
            header,
            StringComparison.Ordinal);

        Assert.Contains("OrganizationContextLocked != true", settings, StringComparison.Ordinal);
        Assert.Contains("Settings_SwitchOrganization", settings, StringComparison.Ordinal);
        Assert.Contains("GoSwitchOrg", settings, StringComparison.Ordinal);
    }

    [Fact]
    public void Offline_operating_grant_remains_user_and_organization_scoped()
    {
        var model = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Offline",
            "OfflineOperatingGrantModels.cs"));

        Assert.Contains("Guid UserId", model, StringComparison.Ordinal);
        Assert.Contains("Guid OrganizationId", model, StringComparison.Ordinal);
        Assert.Contains("public sealed record OfflineOperatingGrant(", model, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Could not locate repository root.");
    }
}
