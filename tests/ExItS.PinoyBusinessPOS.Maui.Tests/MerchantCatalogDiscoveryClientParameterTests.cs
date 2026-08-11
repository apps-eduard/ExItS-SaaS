namespace ExItS.PinoyBusinessPOS.Maui.Tests;

public sealed class MerchantCatalogDiscoveryClientParameterTests
{
    [Fact]
    public void Discovery_and_platform_clients_send_businessTypeCode_not_legacy_businessType()
    {
        var repoRoot = FindRepoRoot();
        var discovery = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.ApiClient",
            "MerchantCatalogDiscoveryClient.cs"));
        var platform = File.ReadAllText(Path.Combine(
            repoRoot,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Api",
            "Catalog",
            "PlatformMerchantCatalogClient.cs"));

        Assert.Contains("businessTypeCode", discovery, StringComparison.Ordinal);
        Assert.Contains("businessTypeId=", discovery, StringComparison.Ordinal);
        Assert.Contains("string? businessTypeCode", discovery, StringComparison.Ordinal);
        Assert.DoesNotContain("AppendOptional(path, \"businessType\",", discovery, StringComparison.Ordinal);
        Assert.DoesNotContain("primaryBusinessTypeId=", discovery, StringComparison.Ordinal);
        Assert.DoesNotContain("string? businessType =", discovery, StringComparison.Ordinal);

        Assert.Contains("businessTypeCode=", platform, StringComparison.Ordinal);
        Assert.Contains("string? businessTypeCode", platform, StringComparison.Ordinal);
        Assert.DoesNotContain("businessType=", platform.Replace("businessTypeCode=", string.Empty, StringComparison.Ordinal), StringComparison.Ordinal);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx"))
                || File.Exists(Path.Combine(dir.FullName, "ExItS.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test base directory.");
    }
}
