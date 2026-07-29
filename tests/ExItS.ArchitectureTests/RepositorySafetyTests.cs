using System.Diagnostics;

namespace ExItS.ArchitectureTests;

public sealed class RepositorySafetyTests
{
    [Fact]
    public void Root_git_does_not_track_HealthCare_paths()
    {
        var root = FindRepositoryRoot();
        var tracked = RunGit(root, "ls-files", "--", "HealthCare/");
        Assert.True(string.IsNullOrWhiteSpace(tracked),
            "Root Git must not track nested HealthCare/ product files. Output: " + tracked);
    }

    [Fact]
    public void HealthCare_directory_is_ignored_by_root_gitignore()
    {
        var root = FindRepositoryRoot();
        var output = RunGit(root, "check-ignore", "-v", "HealthCare/");
        Assert.Contains("/HealthCare/", output, StringComparison.Ordinal);
    }

    [Fact]
    public void Platform_Integration_HealthCare_sources_are_tracked_not_ignored()
    {
        var root = FindRepositoryRoot();
        var relative =
            "src/Platform/ExItS.Platform.Application/Integration/HealthCare/HealthCareIntegrationAbstractions.cs"
                .Replace('/', Path.DirectorySeparatorChar);
        var full = Path.Combine(root, relative);
        Assert.True(File.Exists(full), "Expected Integration HealthCare abstraction file.");

        var ignored = RunGit(root, "check-ignore", "-v", relative.Replace('\\', '/'));
        Assert.True(string.IsNullOrWhiteSpace(ignored),
            "Platform Integration/HealthCare must not be ignored. Output: " + ignored);

        var tracked = RunGit(root, "ls-files", "--", relative.Replace('\\', '/'));
        Assert.False(string.IsNullOrWhiteSpace(tracked),
            "Platform Integration/HealthCare must be tracked by root Git.");
    }

    [Fact]
    public void Root_solution_does_not_list_HealthCare_projects()
    {
        var root = FindRepositoryRoot();
        var slnx = Path.Combine(root, "ExItS.slnx");
        Assert.True(File.Exists(slnx), "ExItS.slnx must exist.");
        var text = File.ReadAllText(slnx);
        Assert.DoesNotContain("HealthCare", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void No_product_or_shared_source_projects_exist_yet()
    {
        var root = FindRepositoryRoot();
        Assert.False(Directory.Exists(Path.Combine(root, "Shared")));
        Assert.False(Directory.Exists(Path.Combine(root, "Products")));
        Assert.False(Directory.Exists(Path.Combine(root, "src", "Products")));
        Assert.False(Directory.Exists(Path.Combine(root, "src", "Shared")));

        var csprojs = Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}HealthCare{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileNameWithoutExtension)
            .ToArray();

        Assert.DoesNotContain(csprojs, name => name is not null && name.Contains("PinoyBusinessPOS", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(csprojs, name => name is not null && name.Contains("HealthCare", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(csprojs, name => name is not null && name.Contains("Blazor", StringComparison.OrdinalIgnoreCase));
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx"))
                && Directory.Exists(Path.Combine(dir.FullName, ".git")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate ExItS repository root from test base directory.");
    }

    private static string RunGit(string workingDirectory, params string[] args)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var arg in args)
        {
            psi.ArgumentList.Add(arg);
        }

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start git.");
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0 && args[0] != "check-ignore")
        {
            throw new InvalidOperationException($"git {string.Join(' ', args)} failed ({process.ExitCode}): {stderr}");
        }

        return stdout.Trim();
    }
}
