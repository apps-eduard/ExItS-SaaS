using ExItS.Platform.Api.Authentication;

namespace ExItS.Platform.UnitTests.Authentication;

/// <summary>PUBSTORE account-scope exemption for public store discovery.</summary>
public sealed class PublicStoreAccountScopeExemptionTests
{
    [Theory]
    [InlineData("/api/v1/public/stores/ORG123456")]
    [InlineData("/api/v1/public/stores/ORG123456/branches")]
    [InlineData("/api/v1/public/stores/orgabcdef")]
    [InlineData("/API/V1/PUBLIC/STORES/ORG999999/branches")]
    public void PUBSTORE_public_store_discovery_paths_are_exempt(string path)
    {
        Assert.True(AccountScopeGuardMiddleware.IsPublicStoreDiscoveryPath(path));
    }

    [Theory]
    [InlineData("/api/v1/public/")]
    [InlineData("/api/v1/public/other")]
    [InlineData("/api/v1/public/stores")]
    [InlineData("/api/v1/platform/organizations")]
    [InlineData("/api/v1/personal/me")]
    [InlineData("/api/v1/organizations/resolve-public-id")]
    public void PUBSTORE_unrelated_routes_are_not_exempted_by_helper(string path)
    {
        Assert.False(AccountScopeGuardMiddleware.IsPublicStoreDiscoveryPath(path));
    }

    [Fact]
    public void PUBSTORE_09_public_store_endpoints_keep_allow_anonymous_and_rate_limiting()
    {
        var root = FindRepositoryRoot();
        var endpoints = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Platform",
            "ExItS.Platform.Api",
            "Organizations",
            "PublicStoreEndpoints.cs"));

        Assert.Contains("/api/v1/public/stores/{publicOrganizationId}", endpoints, StringComparison.Ordinal);
        Assert.Contains("/api/v1/public/stores/{publicOrganizationId}/branches", endpoints, StringComparison.Ordinal);
        Assert.Contains(".AllowAnonymous()", endpoints, StringComparison.Ordinal);
        Assert.Contains("PublicIdResolveRateLimitPolicy", endpoints, StringComparison.Ordinal);
        Assert.Contains("PublicIdResolveRateLimitFilter", endpoints, StringComparison.Ordinal);

        var guard = File.ReadAllText(Path.Combine(
            root,
            "src",
            "Platform",
            "ExItS.Platform.Api",
            "Authentication",
            "AccountScopeGuardMiddleware.cs"));
        Assert.Contains("IsPublicStoreDiscoveryPath", guard, StringComparison.Ordinal);
        Assert.Contains("/api/v1/public/stores/", guard, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "path.StartsWith(\"/api/v1/public/\"",
            guard,
            StringComparison.Ordinal);
    }

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
