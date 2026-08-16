using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class DirectPurchaseReceiptDomainTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly CatalogProductId ProductA = CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly CatalogProductId ProductB = CatalogProductId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
    private static readonly SupplierId Supplier = SupplierId.From(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));
    private static readonly Guid Actor = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Numbers_format_and_normalize()
    {
        var date = new DateOnly(2026, 8, 17);
        Assert.Equal("DPR-20260817-000001", DirectPurchaseReceiptNumbers.Format(date, 1));
        Assert.Equal("DPR-20260817-000001", DirectPurchaseReceiptNumbers.Normalize(" dpr-20260817-000001 "));
    }

    [Fact]
    public void Create_with_supplier_snapshots_source_name()
    {
        var receipt = DirectPurchaseReceipt.Create(
            Org,
            "DPR-20260817-000001",
            DateOnly.FromDateTime(Now.UtcDateTime),
            [Line(ProductA, "Coke", 2m, 12.5m)],
            Actor,
            Now,
            Supplier,
            "Acme Trading");

        Assert.Equal(Supplier, receipt.SupplierId);
        Assert.Equal("Acme Trading", receipt.SourceNameSnapshot);
        Assert.Equal(25m, receipt.TotalCost);
        Assert.Single(receipt.Lines);
    }

    [Fact]
    public void Create_with_ad_hoc_source_and_without_source()
    {
        var withSource = DirectPurchaseReceipt.Create(
            Org,
            "DPR-20260817-000002",
            DateOnly.FromDateTime(Now.UtcDateTime),
            [Line(ProductA, "Coke", 1m, 10m)],
            Actor,
            Now,
            sourceName: "Wet market stall");
        Assert.Null(withSource.SupplierId);
        Assert.Equal("Wet market stall", withSource.SourceNameSnapshot);

        var noSource = DirectPurchaseReceipt.Create(
            Org,
            "DPR-20260817-000003",
            DateOnly.FromDateTime(Now.UtcDateTime),
            [Line(ProductA, "Coke", 1m, 10m)],
            Actor,
            Now);
        Assert.Null(noSource.SupplierId);
        Assert.Null(noSource.SourceNameSnapshot);
    }

    [Fact]
    public void Multi_line_totals_and_rejects_empty_or_invalid_qty_cost()
    {
        var receipt = DirectPurchaseReceipt.Create(
            Org,
            "DPR-20260817-000004",
            DateOnly.FromDateTime(Now.UtcDateTime),
            [
                Line(ProductA, "Coke", 2m, 10m),
                Line(ProductB, "Sprite", 3m, 8m)
            ],
            Actor,
            Now);
        Assert.Equal(2, receipt.Lines.Count);
        Assert.Equal(44m, receipt.TotalCost);

        var empty = Assert.Throws<DomainException>(() =>
            DirectPurchaseReceipt.Create(
                Org,
                "DPR-20260817-000005",
                DateOnly.FromDateTime(Now.UtcDateTime),
                [],
                Actor,
                Now));
        Assert.Equal(DomainErrorCodes.DirectPurchaseRequiresLines, empty.ErrorCode);

        var zeroQty = Assert.Throws<DomainException>(() =>
            DirectPurchaseReceipt.Create(
                Org,
                "DPR-20260817-000006",
                DateOnly.FromDateTime(Now.UtcDateTime),
                [Line(ProductA, "Coke", 0m, 10m)],
                Actor,
                Now));
        Assert.Equal(DomainErrorCodes.InvalidDirectPurchaseQuantity, zeroQty.ErrorCode);

        var zeroCost = Assert.Throws<DomainException>(() =>
            DirectPurchaseReceipt.Create(
                Org,
                "DPR-20260817-000007",
                DateOnly.FromDateTime(Now.UtcDateTime),
                [Line(ProductA, "Coke", 1m, 0m)],
                Actor,
                Now));
        Assert.Equal(DomainErrorCodes.InvalidDirectPurchaseUnitCost, zeroCost.ErrorCode);
    }

    [Fact]
    public void DirectPurchaseReceipt_movement_uses_new_source_type()
    {
        var account = InventoryAccount.CreateUntracked(Org, ProductA, Now);
        account.Enable(0m, UnitOfMeasure.Piece, Actor, Now, hasOpeningStockAlready: false);
        var movement = StockMovement.DirectPurchaseReceipt(
            Org,
            ProductA,
            account.Id,
            5m,
            UnitOfMeasure.Piece,
            Guid.NewGuid(),
            Actor,
            Now);
        Assert.Equal(StockMovementType.DirectPurchaseReceipt, movement.MovementType);
        Assert.Equal(StockMovementSourceType.DirectPurchase, movement.SourceType);
        Assert.Equal(5m, movement.QuantityEffect);
        Assert.Equal(StockMovement.DirectPurchaseReceiptReason, movement.Reason);
    }

    [Fact]
    public void Product_and_source_snapshots_remain_independent_of_later_renames()
    {
        var receipt = DirectPurchaseReceipt.Create(
            Org,
            "DPR-20260817-000008",
            DateOnly.FromDateTime(Now.UtcDateTime),
            [new DirectPurchaseReceiptLineDraft(ProductA, "Canned Corned Beef", "CCB-1", UnitOfMeasure.Piece, 12m, 32.5m)],
            Actor,
            Now,
            Supplier,
            "Paul Supply");

        Assert.Equal("Canned Corned Beef", receipt.Lines[0].ProductNameSnapshot);
        Assert.Equal("CCB-1", receipt.Lines[0].SkuSnapshot);
        Assert.Equal(32.5m, receipt.Lines[0].UnitCost);
        Assert.Equal("Paul Supply", receipt.SourceNameSnapshot);

        // Simulate catalog/supplier renames by asserting receipt values are copies, not live lookups.
        Assert.NotEqual("Imported Soap", receipt.Lines[0].ProductNameSnapshot);
        Assert.NotEqual("Acme Renamed", receipt.SourceNameSnapshot);
    }

    private static DirectPurchaseReceiptLineDraft Line(
        CatalogProductId productId,
        string name,
        decimal qty,
        decimal cost) =>
        new(productId, name, null, UnitOfMeasure.Piece, qty, cost);
}
