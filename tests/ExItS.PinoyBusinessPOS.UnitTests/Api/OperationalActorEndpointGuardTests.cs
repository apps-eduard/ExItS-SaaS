namespace ExItS.PinoyBusinessPOS.UnitTests.Api;

public sealed class OperationalActorEndpointGuardTests
{
    [Fact]
    public void Customer_order_fulfillment_endpoints_require_server_actor()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Api",
            "CustomerOrdering",
            "CustomerOrderEndpoints.cs"));

        Assert.Contains("MapFulfillment", source, StringComparison.Ordinal);
        Assert.Contains("TryGetActorId(request, out var actorId", source, StringComparison.Ordinal);
        Assert.Contains("MarkReadyAsync(org, id, actor, ct)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Stock_count_create_endpoint_requires_server_actor()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Api",
            "Inventory",
            "InventoryEndpoints.cs"));

        Assert.Contains("MapPost(\"/stock-counts\"", source, StringComparison.Ordinal);
        Assert.Contains("TryGetActorId(request, out var actorId", source, StringComparison.Ordinal);
        Assert.Contains("ExecuteAsync(organizationId, body, actorId, ct)", source, StringComparison.Ordinal);
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
