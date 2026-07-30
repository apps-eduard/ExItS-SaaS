namespace ExItS.ArchitectureTests;

/// <summary>P9-WP03: backup/restore architecture and repository safety guards.</summary>
public sealed class BackupRestoreArchitectureTests
{
    private static string RepoRoot()
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

        throw new InvalidOperationException("Could not locate ExItS.slnx from test base directory.");
    }

    [Fact]
    public void Phase_marker_is_backup_and_restore()
    {
        var root = RepoRoot();
        var pos = File.ReadAllText(Path.Combine(root, "src/Products/PinoyBusinessPOS/ExItS.PinoyBusinessPOS.Api/Program.cs"));
        var platform = File.ReadAllText(Path.Combine(root, "src/Platform/ExItS.Platform.Api/Program.cs"));
        Assert.Contains("P9-WP03-backup-and-restore", pos, StringComparison.Ordinal);
        Assert.Contains("P9-WP03-backup-and-restore", platform, StringComparison.Ordinal);
    }

    [Fact]
    public void Backup_library_and_ops_scripts_exist()
    {
        var root = RepoRoot();
        Assert.True(File.Exists(Path.Combine(root, "src/Shared/ExItS.BackupRestore/PostgreSqlBackupService.cs")));
        Assert.True(File.Exists(Path.Combine(root, "ops/backup/Backup-ExItsDatabase.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "ops/backup/Restore-ExItsDatabase.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "ops/backup/Verify-ExItsBackup.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "ops/backup/Invoke-ExItsRetentionCleanup.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "tools/ExItS.BackupRestore.Cli/Program.cs")));
    }

    [Fact]
    public void Repository_does_not_commit_dump_artifacts()
    {
        var root = RepoRoot();
        var dumps = Directory.EnumerateFiles(root, "*.dump", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.Empty(dumps);

        var enc = Directory.EnumerateFiles(root, "*.dump.enc", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .ToList();
        Assert.Empty(enc);
    }

    [Fact]
    public void Ops_config_example_has_no_real_password_payload()
    {
        var root = RepoRoot();
        var example = File.ReadAllText(Path.Combine(root, "ops/backup/config.example.env"));
        Assert.DoesNotContain("Password=prod", example, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bearer ", example, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EXITS_PLATFORM_DATABASE", example, StringComparison.Ordinal);
        Assert.Contains("EXITS_POS_DATABASE", example, StringComparison.Ordinal);
    }

    [Fact]
    public void Destructive_confirmation_token_is_explicit()
    {
        var root = RepoRoot();
        var constants = File.ReadAllText(Path.Combine(root, "src/Shared/ExItS.BackupRestore/BackupManifest.cs"));
        Assert.Contains("DESTROY_AND_RESTORE", constants, StringComparison.Ordinal);
        Assert.Contains("P9-WP03-backup-and-restore", constants, StringComparison.Ordinal);
    }

    [Fact]
    public void Solution_includes_backup_projects_and_excludes_healthcare_root()
    {
        var root = RepoRoot();
        var slnx = File.ReadAllText(Path.Combine(root, "ExItS.slnx"));
        Assert.Contains("ExItS.BackupRestore", slnx, StringComparison.Ordinal);
        Assert.Contains("ExItS.BackupRestore.Tests", slnx, StringComparison.Ordinal);
        Assert.DoesNotContain("HealthCare/", slnx, StringComparison.Ordinal);
        Assert.DoesNotMatch(@"HealthCare[/\\].*\.csproj", slnx);
    }
}
