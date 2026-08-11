using ExItS.PinoyBusinessPOS.Application.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Offline;

/// <summary>WP12 regression guards for POS weighted/prices/offline hardening.</summary>
public sealed class P23Wp12PosHardeningGuardTests
{
    [Fact]
    public void Todays_prices_requires_expected_updated_at_token()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Catalog",
            "CatalogProductUseCases.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("IsStaleOrMissing", text, StringComparison.Ordinal);
        Assert.Contains("ExpectedUpdatedAtUtc is required for Today's Prices", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Checkout_sale_rejects_trusted_snapshots_without_client_sale_id()
    {
        var path = Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Sales",
            "SaleUseCases.cs");
        var text = File.ReadAllText(path);
        Assert.Contains("Trusted sale line snapshots require a client SaleId", text, StringComparison.Ordinal);
        Assert.True(CheckoutSaleLineSnapshots.RequestUsesTrustedSnapshots(
        [
            new CheckoutSaleLineRequest(Guid.NewGuid(), 1m, UnitPriceSnapshot: 10m)
        ]));
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
