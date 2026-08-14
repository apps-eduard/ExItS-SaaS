using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

/// <summary>
/// Inventory boundary: connected-supplier lifecycle must never mutate buyer stock.
/// Only buyer Goods Receipt / PurchaseStockService may add inventory.
/// </summary>
public sealed class ConnectedSupplierInventoryInvariantTests
{
    private static readonly PosOrganizationId Buyer = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosOrganizationId SupplierOrg = PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly DateTimeOffset Now = new(2026, 8, 14, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Supplier_accept_and_decline_do_not_reference_stock_types()
    {
        var domainPath = Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Domain",
            "ConnectedSuppliers",
            "ConnectedSuppliers.cs");
        var text = File.ReadAllText(domainPath);
        var orderClass = text.IndexOf("public sealed class ConnectedPurchaseOrder", StringComparison.Ordinal);
        Assert.True(orderClass > 0);
        var orderBody = text[orderClass..];
        Assert.Contains("public void Accept(", orderBody, StringComparison.Ordinal);
        Assert.Contains("public void Decline(", orderBody, StringComparison.Ordinal);
        Assert.DoesNotContain("Inventory", orderBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("StockMovement", orderBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OnHand", orderBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PurchaseStock", orderBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Accepting_connected_order_preserves_line_qty_without_stock_side_effects()
    {
        var relationship = ConnectedSupplierRelationship.Request(Buyer, SupplierOrg, Now);
        relationship.Approve(Now.AddMinutes(1));
        var line = ConnectedPurchaseOrderLine.Create(CatalogProductId.New(), "Bottled Water", "BW-1", 12m, 25m, "Piece");
        var order = ConnectedPurchaseOrder.CreateFromBuyerSubmission(
            relationship,
            PurchaseOrderId.New(),
            "PO-20260814-000001",
            DateOnly.FromDateTime(Now.UtcDateTime),
            "Desk delivery",
            [line],
            Now.AddMinutes(2));

        Assert.Equal(ConnectedPurchaseOrderStatus.New, order.Status);
        order.Accept(Now.AddMinutes(3));
        Assert.Equal(ConnectedPurchaseOrderStatus.Accepted, order.Status);
        Assert.Equal(12m, Assert.Single(order.Lines).Qty);
        Assert.Equal(300m, order.TotalAmount);

        var declined = ConnectedPurchaseOrder.CreateFromBuyerSubmission(
            relationship,
            PurchaseOrderId.New(),
            "PO-20260814-000002",
            DateOnly.FromDateTime(Now.UtcDateTime),
            null,
            [line],
            Now.AddMinutes(4));
        declined.Decline(Now.AddMinutes(5));
        Assert.Equal(ConnectedPurchaseOrderStatus.Declined, declined.Status);
        Assert.Equal(12m, Assert.Single(declined.Lines).Qty);
    }

    [Fact]
    public void Purchase_submit_path_still_delegates_inventory_only_to_receive()
    {
        var purchase = File.ReadAllText(Path.Combine(
            FindRepoRoot(),
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Application",
            "Purchasing",
            "PurchaseUseCases.cs"));

        Assert.Contains("ConnectedPurchaseOrder.CreateFromBuyerSubmission", purchase, StringComparison.Ordinal);
        Assert.Contains("ApplyReceiptAsync", purchase, StringComparison.Ordinal);
        var submitIdx = purchase.IndexOf("class SubmitPurchaseOrder", StringComparison.Ordinal);
        var receiveIdx = purchase.IndexOf("class ReceivePurchaseOrder", StringComparison.Ordinal);
        Assert.True(submitIdx > 0 && receiveIdx > submitIdx);
        var submitBody = purchase[submitIdx..receiveIdx];
        Assert.DoesNotContain("ApplyReceiptAsync", submitBody, StringComparison.Ordinal);
        Assert.Contains("ConnectedPurchaseOrder", submitBody, StringComparison.Ordinal);
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
