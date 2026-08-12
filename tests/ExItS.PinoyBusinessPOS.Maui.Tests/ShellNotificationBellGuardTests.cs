using ExItS.PinoyBusinessPOS.Application.Platform;

namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class ShellNotificationBellGuardTests
{
    [Fact]
    public void StoreHeader_renders_notification_bell_between_sync_and_menu()
    {
        var header = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "StoreHeader.razor"));
        var syncIndex = header.IndexOf("ShellSyncStatus", StringComparison.Ordinal);
        var bellIndex = header.IndexOf("ShellNotificationBell", StringComparison.Ordinal);
        var menuIndex = header.IndexOf("ShellAccountMenu", StringComparison.Ordinal);

        Assert.True(syncIndex >= 0);
        Assert.True(bellIndex > syncIndex);
        Assert.True(menuIndex > bellIndex);
        Assert.Contains("EffectiveShowNotifications", header, StringComparison.Ordinal);
        Assert.Contains("UsePersonalNotifications", header, StringComparison.Ordinal);
        Assert.Contains("/personal/notifications", File.ReadAllText(Path.Combine(MauiProject(),
            "Components", "Shared", "ShellNotificationBell.razor")), StringComparison.Ordinal);
        Assert.Contains("/org/customer-link-notifications", File.ReadAllText(Path.Combine(MauiProject(),
            "Components", "Shared", "ShellNotificationBell.razor")), StringComparison.Ordinal);
    }

    [Fact]
    public void Personal_and_Organization_shells_keep_authenticated_notifications_enabled()
    {
        var personal = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Layout", "PersonalShell.razor"));
        var pos = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Layout", "PosShell.razor"));
        var auth = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Layout", "AuthShell.razor"));
        var bell = File.ReadAllText(Path.Combine(MauiProject(), "Components", "Shared", "ShellNotificationBell.razor"));

        Assert.Contains("StoreHeaderIdentity.Personal", personal, StringComparison.Ordinal);
        Assert.Contains("StoreHeaderIdentity.Organization", pos, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowNotifications=\"false\"", personal, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowNotifications=\"false\"", pos, StringComparison.Ordinal);
        Assert.Contains("ShowNotifications=\"false\"", auth, StringComparison.Ordinal);
        Assert.Contains("GetPersonalNotificationsAsync", bell, StringComparison.Ordinal);
        Assert.Contains("GetOrganizationNotificationsAsync", bell, StringComparison.Ordinal);
        Assert.Contains("!n.IsRead", bell, StringComparison.Ordinal);
        Assert.Contains("IconGlyphs.Get(\"bell\")", bell, StringComparison.Ordinal);
        Assert.Contains("ShellNotificationUnread.FormatBadge", bell, StringComparison.Ordinal);
        Assert.Contains("Shell_NotificationsAria", bell, StringComparison.Ordinal);
    }

    [Fact]
    public void DesignSystem_provides_cloud_and_bell_glyphs()
    {
        var glyphs = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Shared", "ExItS.DesignSystem",
            "Components", "Internal", "IconGlyphs.cs"));
        Assert.Contains("[\"cloud\"]", glyphs, StringComparison.Ordinal);
        Assert.Contains("[\"cloud-off\"]", glyphs, StringComparison.Ordinal);
        Assert.Contains("[\"bell\"]", glyphs, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(1, "1")]
    [InlineData(7, "7")]
    [InlineData(25, "25")]
    [InlineData(99, "99")]
    [InlineData(100, "99+")]
    [InlineData(250, "99+")]
    public void Unread_badge_formats_compact_display(int unread, string? expected)
    {
        Assert.Equal(expected, ShellNotificationUnread.FormatBadge(unread));
    }

    [Fact]
    public void Localization_includes_notification_aria_keys()
    {
        var en = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(MauiProject(), "Localization", "PosResources.fil-PH.resx"));
        Assert.Contains("name=\"Shell_NotificationsAria\"", en, StringComparison.Ordinal);
        Assert.Contains("name=\"Shell_NotificationsAriaUnread\"", en, StringComparison.Ordinal);
        Assert.Contains("name=\"Shell_NotificationsAria\"", fil, StringComparison.Ordinal);
        Assert.Contains("name=\"Shell_NotificationsAriaUnread\"", fil, StringComparison.Ordinal);
    }

    private static string MauiProject() =>
        Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui");

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
