namespace ExItS.ArchitectureTests;

/// <summary>
/// MB2-01A guards: product governance scope naming, sparse availability keys, no Platform FK coupling.
/// </summary>
public sealed class PosProductGovernanceArchitectureTests
{
    [Fact]
    public void CatalogProductScope_codes_are_OrganizationStandard_and_BranchLocal_not_Global()
    {
        var text = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Domain"),
            "Catalog",
            "CatalogProductScope.cs"));

        Assert.Contains("OrganizationStandard", text, StringComparison.Ordinal);
        Assert.Contains("BranchLocal", text, StringComparison.Ordinal);
        Assert.Contains("Platform Global Catalog", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\n    Global =", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\n    Local =", text, StringComparison.Ordinal);
        Assert.Contains("nameof(CatalogProductScope.OrganizationStandard)", text, StringComparison.Ordinal);
        Assert.Contains("nameof(CatalogProductScope.BranchLocal)", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_domain_has_no_second_product_table_type_for_BranchLocal()
    {
        var catalogDomain = Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"), "Catalog");
        var typeNames = Directory.EnumerateFiles(catalogDomain, "*.cs", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("CatalogProduct", typeNames);
        Assert.Contains("BranchProductAvailability", typeNames);
        Assert.Contains("CatalogProductScope", typeNames);
        Assert.DoesNotContain("BranchLocalProduct", typeNames);
        Assert.DoesNotContain("LocalCatalogProduct", typeNames);
        Assert.DoesNotContain("GlobalCatalogProduct", typeNames);
    }

    [Fact]
    public void BranchProductAvailability_record_has_org_branch_product_keys()
    {
        var recordPath = Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Infrastructure"),
            "Persistence",
            "Catalog",
            "BranchProductAvailabilityRecord.cs");
        var text = File.ReadAllText(recordPath);
        Assert.Contains("OrganizationId", text, StringComparison.Ordinal);
        Assert.Contains("BranchId", text, StringComparison.Ordinal);
        Assert.Contains("ProductId", text, StringComparison.Ordinal);
        Assert.Contains("IsOffered", text, StringComparison.Ordinal);
    }

    [Fact]
    public void PosDbContext_registers_availability_without_platform_branch_fk()
    {
        var context = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Infrastructure"),
            "Persistence",
            "PosDbContext.cs"));

        Assert.Contains("\"branch_product_availabilities\"", context, StringComparison.Ordinal);
        Assert.Contains("fk_branch_product_availabilities_products", context, StringComparison.Ordinal);
        Assert.Contains("ck_products_scope", context, StringComparison.Ordinal);
        Assert.DoesNotContain("OrganizationBranch", context, StringComparison.Ordinal);
        Assert.DoesNotContain("fk_branch_product_availabilities_branches", context, StringComparison.Ordinal);
        Assert.DoesNotContain("fk_products_origin_branch", context, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_domain_does_not_reference_Platform_Infrastructure()
    {
        var domain = Path.Combine(PosProject("ExItS.PinoyBusinessPOS.Domain"), "Catalog");
        foreach (var file in Directory.EnumerateFiles(domain, "*.cs", SearchOption.AllDirectories))
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotContain("ExItS.Platform.Infrastructure", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Microsoft.EntityFrameworkCore", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Npgsql", text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CatalogProduct_has_Scope_and_OriginBranchId()
    {
        var text = File.ReadAllText(Path.Combine(
            PosProject("ExItS.PinoyBusinessPOS.Domain"),
            "Catalog",
            "CatalogProduct.cs"));
        Assert.Contains("CatalogProductScope Scope", text, StringComparison.Ordinal);
        Assert.Contains("PosBranchId? OriginBranchId", text, StringComparison.Ordinal);
        Assert.Contains("CatalogProductScope.OrganizationStandard", text, StringComparison.Ordinal);
    }

    private static string PosProject(string projectName) => Path.Combine(
        FindRepositoryRoot(), "src", "Products", "PinoyBusinessPOS", projectName);

    private static string FindRepositoryRoot()
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
