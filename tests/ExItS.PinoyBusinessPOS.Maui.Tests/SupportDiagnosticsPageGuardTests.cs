namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class SupportDiagnosticsPageGuardTests
{
    [Fact]
    public void Shared_diagnostics_view_is_read_only_and_uses_online_required_guard()
    {
        var view = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "SupportDiagnosticsView.razor"));

        Assert.Contains("ISupportDiagnosticsService", view, StringComparison.Ordinal);
        Assert.Contains("OnlineRequiredGuard", view, StringComparison.Ordinal);
        Assert.Contains("EnsureOnlineAsync", view, StringComparison.Ordinal);
        Assert.Contains("RetrySyncForCurrentSessionAsync", view, StringComparison.Ordinal);
        Assert.Contains("FormatReport", view, StringComparison.Ordinal);
        Assert.Contains("ContainsForbidden", view, StringComparison.Ordinal);
        Assert.DoesNotContain("ClearAsync", view, StringComparison.Ordinal);
        Assert.DoesNotContain("force synced", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/reconnect", view, StringComparison.Ordinal);
        Assert.DoesNotContain("LogoutAsync", view, StringComparison.Ordinal);
    }

    [Fact]
    public void Org_and_personal_settings_link_to_support_diagnostics()
    {
        var settings = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Pages", "Settings.razor"));
        var personal = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Pages", "Personal", "PersonalSettings.razor"));

        Assert.Contains("Support_SectionTitle", settings, StringComparison.Ordinal);
        Assert.Contains("/settings/support/diagnostics", settings, StringComparison.Ordinal);
        Assert.Contains("_canViewSupportDiagnostics", settings, StringComparison.Ordinal);
        Assert.Contains("EvaluateAccessForCurrentSessionAsync", settings, StringComparison.Ordinal);

        Assert.Contains("Support_SectionTitle", personal, StringComparison.Ordinal);
        Assert.Contains("/personal/settings/support/diagnostics", personal, StringComparison.Ordinal);
    }

    [Fact]
    public void Diagnostics_routes_are_offline_capable()
    {
        var policy = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Offline",
            "PosOfflineCapabilityPolicy.cs"));

        Assert.Contains("[\"/settings/support/diagnostics\"] = PosConnectivityRequirement.OfflineCapable", policy, StringComparison.Ordinal);
        Assert.Contains("[\"/personal/settings/support/diagnostics\"] = PosConnectivityRequirement.OfflineCapable", policy, StringComparison.Ordinal);
    }

    [Fact]
    public void Direct_route_pages_exist_for_both_scopes()
    {
        Assert.True(File.Exists(Path.Combine(
            MauiProject(), "Components", "Pages", "Support", "OrgSupportDiagnosticsPage.razor")));
        Assert.True(File.Exists(Path.Combine(
            MauiProject(), "Components", "Pages", "Personal", "PersonalSupportDiagnosticsPage.razor")));

        var orgPage = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Pages", "Support", "OrgSupportDiagnosticsPage.razor"));
        var personalPage = File.ReadAllText(Path.Combine(
            MauiProject(), "Components", "Pages", "Personal", "PersonalSupportDiagnosticsPage.razor"));

        Assert.Contains("@page \"/settings/support/diagnostics\"", orgPage, StringComparison.Ordinal);
        Assert.Contains("SupportDiagnosticsView", orgPage, StringComparison.Ordinal);
        Assert.Contains("@page \"/personal/settings/support/diagnostics\"", personalPage, StringComparison.Ordinal);
        Assert.Contains("@layout Layout.PersonalShell", personalPage, StringComparison.Ordinal);
        Assert.Contains("SupportDiagnosticsView", personalPage, StringComparison.Ordinal);
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
