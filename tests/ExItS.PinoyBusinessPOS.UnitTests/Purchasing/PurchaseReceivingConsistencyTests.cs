using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Purchasing;

public sealed class PurchaseReceivingConsistencyTests
{
    private static readonly PosOrganizationId Org =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly CatalogProductId Product =
        CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly Guid Actor = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateTimeOffset Utc = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Direct_purchase_movement_stores_base_unit_cost()
    {
        var accountId = InventoryAccountId.New();
        var receiptId = Guid.NewGuid();
        var movement = StockMovement.DirectPurchaseReceipt(
            Org,
            Product,
            accountId,
            24m,
            UnitOfMeasure.Piece,
            receiptId,
            Actor,
            Utc,
            unitCost: 18m);

        Assert.Equal(18m, movement.UnitCost);
        Assert.Equal(24m, movement.QuantityEffect);
        Assert.Equal(StockMovementType.DirectPurchaseReceipt, movement.MovementType);
        Assert.Equal(432m, SaleMoney.RoundMoney(movement.UnitCost!.Value * movement.QuantityEffect));
    }

    [Fact]
    public void Purchase_receipt_converts_case_cost_to_base_unit_cost()
    {
        // 2 Cases @ ₱240/Case, multiplier 24 → 48 Pieces @ ₱10/Piece, stock value ₱480
        Assert.Equal(48m, ProductUnitConversion.ToBaseQuantity(2m, 24m));
        Assert.Equal(10m, ProductUnitConversion.ToBaseUnitCost(240m, 24m));

        var accountId = InventoryAccountId.New();
        var grnId = Guid.NewGuid();
        var baseQty = ProductUnitConversion.ToBaseQuantity(2m, 24m);
        var baseCost = ProductUnitConversion.ToBaseUnitCost(240m, 24m);
        var movement = StockMovement.PurchaseReceipt(
            Org,
            Product,
            accountId,
            baseQty,
            UnitOfMeasure.Piece,
            grnId,
            Actor,
            Utc,
            unitCost: baseCost);

        Assert.Equal(48m, movement.QuantityEffect);
        Assert.Equal(10m, movement.UnitCost);
        Assert.Equal(480m, SaleMoney.RoundMoney(movement.UnitCost!.Value * movement.QuantityEffect));
        Assert.NotEqual(240m, movement.UnitCost);
    }

    [Fact]
    public void Goods_receipt_line_exposes_base_unit_cost_from_purchase_unit_snapshot()
    {
        var supplier = SupplierId.From(Guid.NewGuid());
        var po = PurchaseOrder.CreateDraft(
            Org,
            supplier,
            DateOnly.FromDateTime(Utc.UtcDateTime),
            [
                new PurchaseOrderLineDraft(
                    Product,
                    OrderedQty: 2m,
                    UnitPurchaseCost: 240m,
                    PurchaseUnitNameSnapshot: "Case",
                    MultiplierToBaseSnapshot: 24m)
            ],
            Utc);
        po.Submit(
            "PO-20260828-000001",
            [
                new PurchaseOrderLineSnapshotInput(
                    Product,
                    "Bath Soap",
                    UnitOfMeasure.Piece,
                    OrderedQty: 2m,
                    UnitPurchaseCost: 240m,
                    PurchaseUnitNameSnapshot: "Case",
                    MultiplierToBaseSnapshot: 24m)
            ],
            Actor,
            Utc.AddMinutes(1));

        var receive = new PurchaseOrderReceiveLineDraft(
            Product,
            ReceiveQty: 2m,
            ExpiryDate: null,
            LotNumber: null);
        var grn = GoodsReceipt.Create(
            Org,
            po.Id,
            "GRN-20260828-000001",
            po,
            [receive],
            Actor,
            Utc.AddMinutes(2));

        var line = Assert.Single(grn.Lines);
        Assert.Equal(2m, line.QuantityReceived);
        Assert.Equal(240m, line.UnitPurchaseCostSnapshot);
        Assert.Equal(480m, line.LineTotalSnapshot);
        Assert.Equal(48m, line.BaseQuantity);
        Assert.Equal(10m, line.BaseUnitCost);
    }

    [Fact]
    public void Goods_receipt_line_stores_expiry_and_lot_for_good_qty()
    {
        var supplier = SupplierId.From(Guid.NewGuid());
        var po = PurchaseOrder.CreateDraft(
            Org,
            supplier,
            DateOnly.FromDateTime(Utc.UtcDateTime),
            [new PurchaseOrderLineDraft(Product, 10m, 18m)],
            Utc);
        po.Submit(
            "PO-20260828-000002",
            [
                new PurchaseOrderLineSnapshotInput(
                    Product,
                    "Milk",
                    UnitOfMeasure.Piece,
                    10m,
                    18m)
            ],
            Actor,
            Utc.AddMinutes(1));

        var receive = new PurchaseOrderReceiveLineDraft(
            Product,
            ReceiveQty: 4m,
            ExpiryDate: new DateOnly(2027, 12, 30),
            LotNumber: "LOT-A123");
        var grn = GoodsReceipt.Create(
            Org,
            po.Id,
            "GRN-20260828-000002",
            po,
            [receive],
            Actor,
            Utc.AddMinutes(2));

        var line = Assert.Single(grn.Lines);
        Assert.Equal(new DateOnly(2027, 12, 30), line.ExpiryDate);
        Assert.Equal("LOT-A123", line.LotNumber);
    }

    [Fact]
    public void Damaged_only_receipt_line_does_not_keep_expiry()
    {
        var supplier = SupplierId.From(Guid.NewGuid());
        var po = PurchaseOrder.CreateDraft(
            Org,
            supplier,
            DateOnly.FromDateTime(Utc.UtcDateTime),
            [new PurchaseOrderLineDraft(Product, 10m, 18m)],
            Utc);
        po.Submit(
            "PO-20260828-000003",
            [
                new PurchaseOrderLineSnapshotInput(
                    Product,
                    "Milk",
                    UnitOfMeasure.Piece,
                    10m,
                    18m)
            ],
            Actor,
            Utc.AddMinutes(1));

        var receive = new PurchaseOrderReceiveLineDraft(
            Product,
            ReceiveQty: 0m,
            DamagedQty: 2m,
            ExpiryDate: new DateOnly(2027, 1, 1),
            LotNumber: "SHOULD-CLEAR");
        var grn = GoodsReceipt.Create(
            Org,
            po.Id,
            "GRN-20260828-000003",
            po,
            [receive],
            Actor,
            Utc.AddMinutes(2));

        var line = Assert.Single(grn.Lines);
        Assert.Equal(0m, line.QuantityReceived);
        Assert.Null(line.ExpiryDate);
        Assert.Null(line.LotNumber);
    }

    [Fact]
    public void Manual_increase_does_not_carry_unit_cost()
    {
        var accountId = InventoryAccountId.New();
        var movement = StockMovement.ManualIncrease(
            Org,
            Product,
            accountId,
            5m,
            UnitOfMeasure.Piece,
            "Count correction",
            Actor,
            Utc);

        Assert.Null(movement.UnitCost);
        Assert.Equal(StockMovementType.ManualIncrease, movement.MovementType);
    }
}
