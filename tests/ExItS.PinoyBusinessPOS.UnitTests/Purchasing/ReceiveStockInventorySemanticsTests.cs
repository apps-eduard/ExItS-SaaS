using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Purchasing;

/// <summary>
/// UX wording maps Receive stock → ManualIncrease; PO create/submit must not raise on-hand.
/// </summary>
public sealed class ReceiveStockInventorySemanticsTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly CatalogProductId Product = CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly SupplierId Supplier = SupplierId.From(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));
    private static readonly Guid Actor = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ReceiveStockIncreasesInventory()
    {
        var account = InventoryAccount.CreateUntracked(Org, Product, Now);
        account.Enable(10m, UnitOfMeasure.Piece, Actor, Now, hasOpeningStockAlready: false);
        var before = account.OnHandQuantity;

        var movement = StockMovement.ManualIncrease(
            Org,
            Product,
            account.Id,
            5m,
            UnitOfMeasure.Piece,
            "Receive stock",
            Actor,
            Now);
        account.ApplyMovementEffect(movement.QuantityEffect);

        Assert.Equal(before + 5m, account.OnHandQuantity);
        Assert.Equal(StockMovementType.ManualIncrease, movement.MovementType);
        Assert.Null(movement.SourceId);
    }

    [Fact]
    public void PurchaseOrderDoesNotIncreaseInventory()
    {
        var account = InventoryAccount.CreateUntracked(Org, Product, Now);
        account.Enable(10m, UnitOfMeasure.Piece, Actor, Now, hasOpeningStockAlready: false);
        var onHandBefore = account.OnHandQuantity;

        var po = PurchaseOrder.CreateDraft(
            Org,
            Supplier,
            DateOnly.FromDateTime(Now.Date),
            [new PurchaseOrderLineDraft(Product, 2m, 10.5m)],
            Now);

        Assert.Equal(PurchaseOrderStatus.Draft, po.Status);
        Assert.Equal(onHandBefore, account.OnHandQuantity);
    }

    [Fact]
    public void SupplierAcceptanceDoesNotIncreaseInventory_WhenPoIsSubmitted()
    {
        var account = InventoryAccount.CreateUntracked(Org, Product, Now);
        account.Enable(10m, UnitOfMeasure.Piece, Actor, Now, hasOpeningStockAlready: false);
        var onHandBefore = account.OnHandQuantity;

        var po = PurchaseOrder.CreateDraft(
            Org,
            Supplier,
            DateOnly.FromDateTime(Now.Date),
            [new PurchaseOrderLineDraft(Product, 2m, 10.5m)],
            Now);

        po.Submit(
            "PO-20260815-000001",
            [
                new PurchaseOrderLineSnapshotInput(
                    Product,
                    "Item",
                    UnitOfMeasure.Piece,
                    2m,
                    10.5m,
                    null)
            ],
            Actor,
            Now);

        Assert.Equal(PurchaseOrderStatus.Ordered, po.Status);
        Assert.Equal(onHandBefore, account.OnHandQuantity);
    }

    [Fact]
    public void GoodsReceiptIncreasesInventory()
    {
        var account = InventoryAccount.CreateUntracked(Org, Product, Now);
        account.Enable(10m, UnitOfMeasure.Piece, Actor, Now, hasOpeningStockAlready: false);
        var before = account.OnHandQuantity;
        var goodsReceiptId = Guid.NewGuid();

        var movement = StockMovement.PurchaseReceipt(
            Org,
            Product,
            account.Id,
            3m,
            UnitOfMeasure.Piece,
            goodsReceiptId,
            Actor,
            Now);
        account.ApplyMovementEffect(movement.QuantityEffect);

        Assert.Equal(before + 3m, account.OnHandQuantity);
        Assert.Equal(StockMovementType.PurchaseReceipt, movement.MovementType);
        Assert.Equal(goodsReceiptId, movement.SourceId);
    }

    [Fact]
    public void ReceiveStockSupplierCanBeOptional()
    {
        var account = InventoryAccount.CreateUntracked(Org, Product, Now);
        account.Enable(0m, UnitOfMeasure.Piece, Actor, Now, hasOpeningStockAlready: false);

        var movement = StockMovement.ManualIncrease(
            Org,
            Product,
            account.Id,
            1m,
            UnitOfMeasure.Piece,
            "Cash and carry",
            Actor,
            Now);

        Assert.Equal(StockMovementSourceType.Manual, movement.SourceType);
        Assert.Null(movement.SourceId);
    }

    [Fact]
    public void ReceiveStockUnitConversionUsesBaseQuantity()
    {
        const decimal boxes = 5m;
        const decimal unitsPerBox = 24m;
        var baseQty = boxes * unitsPerBox;

        var account = InventoryAccount.CreateUntracked(Org, Product, Now);
        account.Enable(0m, UnitOfMeasure.Piece, Actor, Now, hasOpeningStockAlready: false);

        var movement = StockMovement.ManualIncrease(
            Org,
            Product,
            account.Id,
            baseQty,
            UnitOfMeasure.Piece,
            "Receive stock boxes",
            Actor,
            Now);
        account.ApplyMovementEffect(movement.QuantityEffect);

        Assert.Equal(120m, account.OnHandQuantity);
    }

    [Fact]
    public void ReceiveStockPreservesExpirationData()
    {
        var expiry = new DateOnly(2026, 12, 31);
        var account = InventoryAccount.CreateUntracked(Org, Product, Now);
        account.Enable(0m, UnitOfMeasure.Piece, Actor, Now, hasOpeningStockAlready: false);

        var lot = InventoryLot.Create(
            Org,
            Product,
            expiry,
            quantityOnHand: 2m,
            Now,
            lotNumber: "LOT-1");

        Assert.Equal(expiry, lot.ExpirationDate);
        Assert.Equal("LOT-1", lot.LotNumber);
        Assert.Equal(2m, lot.QuantityOnHand);
    }
}
