using ExItS.Platform.Application.Catalog;

namespace ExItS.Platform.UnitTests.Organizations;

/// <summary>
/// WP12 regression guards for Platform multi-business hardening.
/// </summary>
public sealed class P23Wp12RegressionGuardTests
{
    [Fact]
    public void Platform_unit_of_work_exposes_organization_advisory_lock()
    {
        var method = typeof(IPlatformUnitOfWork).GetMethod(nameof(IPlatformUnitOfWork.ExecuteWithOrganizationLockAsync));
        Assert.NotNull(method);
    }

    [Fact]
    public void Activate_business_type_use_case_uses_organization_lock()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "src",
            "Platform",
            "ExItS.Platform.Application",
            "Organizations",
            "OrganizationBusinessTypeActivationUseCases.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("ExecuteWithOrganizationLockAsync", text, StringComparison.Ordinal);
        Assert.Contains("PersistenceConflictException", text, StringComparison.Ordinal);
        Assert.Contains("Idempotent: already inactive", text, StringComparison.Ordinal);
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
