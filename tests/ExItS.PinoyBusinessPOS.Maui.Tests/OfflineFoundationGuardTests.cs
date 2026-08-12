namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class OfflineFoundationGuardTests
{
    [Fact]
    public void PosShell_hosts_persistent_sync_status_indicator()
    {
        var shell = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Components", "Layout", "PosShell.razor"));
        var sync = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Components", "Shared", "ShellSyncStatus.razor"));
        var header = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Components", "Shared", "StoreHeader.razor"));

        Assert.Contains("StoreHeader", shell, StringComparison.Ordinal);
        Assert.Contains("ShellSyncStatus", header, StringComparison.Ordinal);
        Assert.Contains("IPosSyncStatusService", sync, StringComparison.Ordinal);
        Assert.Contains("NotifyApiReachability", File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Application",
            "Abstractions", "SyncStatusAbstractions.cs")), StringComparison.Ordinal);
        Assert.Contains("PosApiReachabilityHandler", File.ReadAllText(Path.Combine(FindRepoRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.ApiClient",
            "DependencyInjection.cs")), StringComparison.Ordinal);
        Assert.Contains("pos-sync-status", sync, StringComparison.Ordinal);
        Assert.Contains("pos-sync-status--chip", sync, StringComparison.Ordinal);
        Assert.Contains("pos-topbar__status", header, StringComparison.Ordinal);
        Assert.Contains("pos-sync-popover", sync, StringComparison.Ordinal);
        Assert.Contains("ShellAccountMenu", header, StringComparison.Ordinal);
        Assert.Contains("SyncStatus_Online", sync, StringComparison.Ordinal);
        Assert.Contains("SyncStatus_Offline", sync, StringComparison.Ordinal);
        Assert.Contains("SyncStatus_Pending", sync, StringComparison.Ordinal);
        Assert.Contains("SyncStatus_Syncing", sync, StringComparison.Ordinal);
        Assert.Contains("SyncStatus_Failed", sync, StringComparison.Ordinal);
        Assert.Contains("LastSyncedAtUtc", sync, StringComparison.Ordinal);
        Assert.Contains("OfflineQueue.GetCountsAsync", sync, StringComparison.Ordinal);
        Assert.Contains("aria-label", sync, StringComparison.Ordinal);
        Assert.Contains("SyncStatus_ConnectionLabel", sync, StringComparison.Ordinal);
        Assert.Contains("SyncStatus_SyncLabel", sync, StringComparison.Ordinal);
        Assert.Contains("SyncStatus_PendingItems", sync, StringComparison.Ordinal);
        Assert.Contains("SyncStatus_WaitingForConnection", sync, StringComparison.Ordinal);
        Assert.Contains("SyncStatus_SyncNow", sync, StringComparison.Ordinal);
        Assert.Contains("ConnectionTitle", sync, StringComparison.Ordinal);
        Assert.Contains("SyncSectionTitle", sync, StringComparison.Ordinal);
        Assert.Contains("pos-sync-popover__section", sync, StringComparison.Ordinal);
        Assert.DoesNotContain("ExtraItems", sync, StringComparison.Ordinal);
        Assert.DoesNotContain("pos-sync-status--menu", sync, StringComparison.Ordinal);
        Assert.DoesNotContain("SyncQueue", sync, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("emoji", sync, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PosShell_sync_chip_distinguishes_quiet_and_attention_states()
    {
        var sync = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Components", "Shared", "ShellSyncStatus.razor"));
        var css = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "wwwroot", "app.css"));

        Assert.Contains("pos-sync-status--quiet", sync, StringComparison.Ordinal);
        Assert.Contains("pos-sync-status--attention", sync, StringComparison.Ordinal);
        Assert.Contains("pos-sync-status__badge", sync, StringComparison.Ordinal);
        Assert.Contains("PosSyncStatusKind.LastSynced => \"synced\"", sync, StringComparison.Ordinal);
        Assert.Contains(".pos-sync-status--quiet", css, StringComparison.Ordinal);
        Assert.Contains(".pos-sync-status--synced", css, StringComparison.Ordinal);
        Assert.Contains(".pos-sync-popover__section", css, StringComparison.Ordinal);
        Assert.Contains(".pos-sync-popover__value--synced", css, StringComparison.Ordinal);
        Assert.Contains(".pos-sync-status__badge", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Offline_foundation_diagnostics_are_dev_gated()
    {
        var page = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Components", "Pages", "Dev", "OfflineFoundationDiagnosticsPage.razor"));

        Assert.Contains("@page \"/dev/offline-foundation\"", page, StringComparison.Ordinal);
        Assert.Contains("Diagnostics.IsAvailable", page, StringComparison.Ordinal);
        Assert.Contains("DevOffline_Unavailable", page, StringComparison.Ordinal);
        Assert.DoesNotContain("Authorization", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SyncQueue", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("AppDataDirectory", page, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bearer ", page, StringComparison.OrdinalIgnoreCase);

        var shell = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Components", "Layout", "PosShell.razor"));
        Assert.DoesNotContain("/dev/offline-foundation", shell, StringComparison.Ordinal);
    }

    [Fact]
    public void Sync_status_resources_exist_in_english_and_tagalog()
    {
        var en = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Localization", "PosResources.resx"));
        var fil = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Localization", "PosResources.fil-PH.resx"));

        foreach (var key in new[]
                 {
                     "SyncStatus_Online", "SyncStatus_Offline", "SyncStatus_Reconnect",
                     "SyncStatus_ReconnectMessage", "SyncStatus_SyncNow", "SyncStatus_ConnectionLabel",
                     "SyncStatus_PendingCountLabel", "SyncStatus_FailedCountLabel",
                     "SyncStatus_PendingItems", "SyncStatus_WaitingForConnection",
                     "SyncStatus_SyncingProgress", "SyncStatus_ConnectionOnlineDetail",
                     "DevOffline_Title", "Settings_DevOfflineLink"
                 })
        {
            Assert.Contains($"name=\"{key}\"", en, StringComparison.Ordinal);
            Assert.Contains($"name=\"{key}\"", fil, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void DesignSystem_and_razor_pages_do_not_open_sqlite_directly()
    {
        var root = FindRepoRoot();
        var designCsproj = File.ReadAllText(Path.Combine(root, "src", "Shared", "ExItS.DesignSystem",
            "ExItS.DesignSystem.csproj"));
        Assert.DoesNotContain("Sqlite", designCsproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LocalStore", designCsproj, StringComparison.OrdinalIgnoreCase);

        var pagesDir = Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Components", "Pages");
        foreach (var file in Directory.EnumerateFiles(pagesDir, "*.razor", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("Microsoft.Data.Sqlite", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SqliteConnection", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ILocalDatabaseFactory", text, StringComparison.Ordinal);
            Assert.DoesNotContain("SyncQueue", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("operation_queue", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void LocalStore_uses_microsoft_data_sqlite_without_sqlcipher_or_queue()
    {
        var root = FindRepoRoot();
        var csproj = File.ReadAllText(Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.LocalStore", "ExItS.PinoyBusinessPOS.LocalStore.csproj"));
        Assert.Contains("Microsoft.Data.Sqlite", csproj, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SQLCipher", csproj, StringComparison.OrdinalIgnoreCase);

        foreach (var file in Directory.EnumerateFiles(
                     Path.Combine(root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.LocalStore"),
                     "*.cs",
                     SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("class SyncQueue", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("IOfflineMutationQueue", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SQLCipher", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CREATE TABLE customers", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("CREATE TABLE credit_entries", text, StringComparison.OrdinalIgnoreCase);
        }

        var migrator = File.ReadAllText(Path.Combine(root, "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.LocalStore", "LocalDatabaseMigrator.cs"));
        Assert.Contains("local_customer_projection", migrator, StringComparison.Ordinal);
        Assert.Contains("local_repayment_projection", migrator, StringComparison.Ordinal);
        Assert.Contains("v4 payment projections", migrator, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CREATE TABLE repayments", migrator, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("CREATE TABLE IF NOT EXISTS repayments", migrator, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Reconnect_page_exists_for_offline_access_denial()
    {
        var page = File.ReadAllText(Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Maui", "Components", "Pages", "Reconnect.razor"));
        Assert.Contains("@page \"/reconnect\"", page, StringComparison.Ordinal);
        Assert.Contains("SyncStatus_Reconnect", page, StringComparison.Ordinal);
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
