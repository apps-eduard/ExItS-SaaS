using System.Diagnostics;

namespace ExItS.ArchitectureTests;

/// <summary>
/// Portfolio independence: ExItS must not nest or track a HealthCare product source tree.
/// </summary>
public sealed class RepositorySafetyTests
{
    [Fact]
    public void Root_git_does_not_track_HealthCare_product_paths()
    {
        var root = FindRepositoryRoot();
        var tracked = RunGit(root, "ls-files", "--", "HealthCare/");
        Assert.True(string.IsNullOrWhiteSpace(tracked),
            "Root Git must not track a HealthCare/ product tree. Output: " + tracked);
    }

    [Fact]
    public void Root_HealthCare_product_directory_does_not_exist()
    {
        var root = FindRepositoryRoot();
        Assert.False(
            Directory.Exists(Path.Combine(root, "HealthCare")),
            "A nested HealthCare/ product directory must not exist in the ExItS workspace.");
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
    public void PinoyBusinessPOS_and_DesignSystem_projects_exist_without_HealthCare_csproj()
    {
        var root = FindRepositoryRoot();
        Assert.True(Directory.Exists(Path.Combine(root, "src", "Products", "PinoyBusinessPOS")));
        Assert.True(Directory.Exists(Path.Combine(root, "src", "Shared", "ExItS.DesignSystem")));
        Assert.False(Directory.Exists(Path.Combine(root, "Shared")));
        Assert.False(Directory.Exists(Path.Combine(root, "Products")));

        var csprojs = Directory.GetFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                        && !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileNameWithoutExtension)
            .ToArray();

        Assert.Contains(csprojs, name => name is not null && name.Equals("ExItS.PinoyBusinessPOS.Maui", StringComparison.Ordinal));
        Assert.Contains(csprojs, name => name is not null && name.Equals("ExItS.PinoyBusinessPOS.Web", StringComparison.Ordinal));
        Assert.Contains(csprojs, name => name is not null && name.Equals("ExItS.DesignSystem", StringComparison.Ordinal));
        Assert.DoesNotContain(csprojs, name => name is not null && name.Contains("HealthCare", StringComparison.OrdinalIgnoreCase));
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
