namespace ExItS.PinoyBusinessPOS.Web.Tests;

public sealed class OrgWebInventoryStockWordingTests
{
    [Fact]
    public void Stock_adjustment_uses_friendly_movement_labels_and_remarks()
    {
        var stock = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Web",
            "Components",
            "Pages",
            "Inventory",
            "Stock.razor"));

        Assert.Contains("<option value=\"In\">Stock in</option>", stock, StringComparison.Ordinal);
        Assert.Contains("<option value=\"Out\">Stock out</option>", stock, StringComparison.Ordinal);
        Assert.Contains("Label=\"Remarks\"", stock, StringComparison.Ordinal);
        Assert.Contains("StockMovementPresentation.ToFriendlyLabel(m.MovementType)", stock, StringComparison.Ordinal);
        Assert.Contains("<th>Remarks</th>", stock, StringComparison.Ordinal);
        Assert.Contains("Quantity and remarks are required.", stock, StringComparison.Ordinal);
        Assert.DoesNotContain("@m.MovementType", stock, StringComparison.Ordinal);
        Assert.DoesNotContain("Label=\"Reason\"", stock, StringComparison.Ordinal);
        Assert.DoesNotContain("In (receive)", stock, StringComparison.Ordinal);
        Assert.DoesNotContain("Out (write-off)", stock, StringComparison.Ordinal);
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
