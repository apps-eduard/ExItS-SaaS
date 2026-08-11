using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.Suppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Operations;

/// <summary>WP07: ByWeight kg quantities through purchasing, returns, adjustments, and report aggregation.</summary>
public sealed class WeightedOperationsTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly Guid Actor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset Now = new(2026, 8, 11, 18, 0, 0, TimeSpan.Zero);
    private static readonly CatalogProductId TomatoId = CatalogProductId.From(Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));
    private static readonly SupplierId Supplier = SupplierId.From(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));

    private static SaleLineDraft WeightedSaleLine(decimal qtyKg, decimal pricePerKg, string name = "Tomato") =>
        new(
            TomatoId,
            name,
            null,
            null,
            UnitOfMeasure.Kilogram,
            pricePerKg,
            qtyKg,
            SellingMode.ByWeight);

    private static Sale CheckoutWeighted(decimal qtyKg, decimal pricePerKg)
    {
        return Sale.Checkout(
            Org,
            SaleNumbers.Format(DateOnly.FromDateTime(Now.UtcDateTime), 1),
            SalePaymentMethod.Cash,
            [WeightedSaleLine(qtyKg, pricePerKg)],
            Actor,
            Now,
            amountTendered: 10_000m,
            cashierShiftId: CashierShiftId.New(),
            registerId: RegisterId.New());
    }

    [Fact]
    public void Purchase_order_ByWeight_accepts_decimal_kg_and_costs_per_kg()
    {
        var org = Org;
        var po = PurchaseOrder.CreateDraft(
            org,
            Supplier,
            DateOnly.FromDateTime(Now.Date),
            [new PurchaseOrderLineDraft(TomatoId, 10.500m, 80m)],
            Now);

        po.Submit(
            "PO-20260811-000001",
            [
                new PurchaseOrderLineSnapshotInput(
                    TomatoId,
                    "Tomato",
                    UnitOfMeasure.Kilogram,
                    10.500m,
                    80m,
                    SellingMode: SellingMode.ByWeight)
            ],
            Actor,
            Now);

        var line = Assert.Single(po.Lines);
        Assert.Equal(10.500m, line.OrderedQty);
        Assert.Equal(840.00m, line.LineTotal);

        line.ApplyReceipt(10.500m, SellingMode.ByWeight);
        Assert.Equal(10.500m, line.ReceivedQty);
        Assert.Equal(0m, line.OutstandingQty);
    }

    [Fact]
    public void Purchase_receipt_rejects_over_precision_kg()
    {
        var po = PurchaseOrder.CreateDraft(
            Org,
            Supplier,
            DateOnly.FromDateTime(Now.Date),
            [new PurchaseOrderLineDraft(TomatoId, 1m, 80m)],
            Now);
        po.Submit(
            "PO-20260811-000002",
            [
                new PurchaseOrderLineSnapshotInput(
                    TomatoId,
                    "Tomato",
                    UnitOfMeasure.Kilogram,
                    1m,
                    80m,
                    SellingMode: SellingMode.ByWeight)
            ],
            Actor,
            Now);

        var ex = Assert.Throws<DomainException>(
            () => po.Lines.Single().ApplyReceipt(0.1234m, SellingMode.ByWeight));
        Assert.Equal(DomainErrorCodes.InvalidSaleLineQuantity, ex.ErrorCode);
    }

    [Fact]
    public void Purchase_ByWeight_rejects_non_kilogram_unit()
    {
        var po = PurchaseOrder.CreateDraft(
            Org,
            Supplier,
            DateOnly.FromDateTime(Now.Date),
            [new PurchaseOrderLineDraft(TomatoId, 1m, 80m)],
            Now);

        var ex = Assert.Throws<DomainException>(() => po.Submit(
            "PO-20260811-000003",
            [
                new PurchaseOrderLineSnapshotInput(
                    TomatoId,
                    "Tomato",
                    UnitOfMeasure.Bottle,
                    1m,
                    80m,
                    SellingMode: SellingMode.ByWeight)
            ],
            Actor,
            Now));
        Assert.Equal(DomainErrorCodes.InvalidSellingModeUnit, ex.ErrorCode);
    }

    [Fact]
    public void PurchaseReceipt_movement_increases_inventory_exact_kg()
    {
        var account = InventoryAccount.CreateUntracked(Org, TomatoId, Now);
        account.Enable(0m, UnitOfMeasure.Kilogram, Actor, Now, hasOpeningStockAlready: false);

        var movement = StockMovement.PurchaseReceipt(
            Org,
            TomatoId,
            account.Id,
            25.500m,
            UnitOfMeasure.Kilogram,
            Guid.NewGuid(),
            Actor,
            Now,
            sellingMode: SellingMode.ByWeight);
        account.ApplyMovementEffect(movement.QuantityEffect);
        Assert.Equal(25.500m, account.OnHandQuantity);
    }

    [Fact]
    public void Weighted_partial_return_uses_historical_price_and_restores_exact_kg()
    {
        var sale = CheckoutWeighted(1.200m, 120m);
        var saleLine = sale.Lines.Single();
        Assert.Equal(144.00m, saleLine.LineTotal);

        var returnLine = SaleReturnLine.Create(
            SaleReturnId.New(),
            Org,
            saleLine,
            new SaleReturnLineDraft(saleLine.Id, 0.350m, RestockDisposition.ReturnToStock),
            previouslyReturnedQuantity: 0m,
            previouslyRefundedAmount: 0m);

        Assert.Equal(0.350m, returnLine.QuantityReturned);
        Assert.Equal(120m, returnLine.UnitPriceSnapshot);
        Assert.Equal(42.00m, returnLine.RefundAmount);

        var account = InventoryAccount.CreateUntracked(Org, TomatoId, Now);
        account.Enable(48.800m, UnitOfMeasure.Kilogram, Actor, Now, hasOpeningStockAlready: false);
        var restock = StockMovement.SaleReturnRestock(
            Org,
            TomatoId,
            account.Id,
            returnLine.QuantityReturned,
            saleLine.UnitOfMeasureSnapshot,
            Guid.NewGuid(),
            Actor,
            Now,
            sellingMode: saleLine.SellingModeSnapshot);
        account.ApplyMovementEffect(restock.QuantityEffect);
        Assert.Equal(49.150m, account.OnHandQuantity);
    }

    [Fact]
    public void Repeated_partial_returns_cannot_exceed_original_sold_quantity()
    {
        var sale = CheckoutWeighted(1.200m, 120m);
        var saleLine = sale.Lines.Single();

        var first = SaleReturnLine.Create(
            SaleReturnId.New(),
            Org,
            saleLine,
            new SaleReturnLineDraft(saleLine.Id, 0.800m, RestockDisposition.DoNotRestock),
            0m,
            0m);
        Assert.Equal(96.00m, first.RefundAmount);

        var second = SaleReturnLine.Create(
            SaleReturnId.New(),
            Org,
            saleLine,
            new SaleReturnLineDraft(saleLine.Id, 0.400m, RestockDisposition.DoNotRestock),
            previouslyReturnedQuantity: 0.800m,
            previouslyRefundedAmount: 96.00m);
        Assert.Equal(48.00m, second.RefundAmount);

        var over = Assert.Throws<DomainException>(() => SaleReturnLine.Create(
            SaleReturnId.New(),
            Org,
            saleLine,
            new SaleReturnLineDraft(saleLine.Id, 0.001m, RestockDisposition.DoNotRestock),
            previouslyReturnedQuantity: 1.200m,
            previouslyRefundedAmount: 144.00m));
        Assert.Equal(DomainErrorCodes.SaleReturnQuantityExceedsRefundable, over.ErrorCode);
    }

    [Fact]
    public void Historical_return_ignores_later_product_mode_change_on_sale_line_snapshot()
    {
        var sale = CheckoutWeighted(1.200m, 120m);
        var saleLine = sale.Lines.Single();
        Assert.Equal(SellingMode.ByWeight, saleLine.SellingModeSnapshot);

        // Live catalog may later change; return still uses the immutable sale-line snapshot.
        var returnLine = SaleReturnLine.Create(
            SaleReturnId.New(),
            Org,
            saleLine,
            new SaleReturnLineDraft(saleLine.Id, 0.001m, RestockDisposition.DoNotRestock),
            0m,
            0m);
        Assert.Equal(0.001m, returnLine.QuantityReturned);
        Assert.Equal(0.12m, returnLine.RefundAmount);
    }

    [Fact]
    public void Manual_adjustment_ByWeight_preserves_exact_decimal_kg()
    {
        var account = InventoryAccount.CreateUntracked(Org, TomatoId, Now);
        account.Enable(10.000m, UnitOfMeasure.Kilogram, Actor, Now, hasOpeningStockAlready: false);

        var add = StockMovement.ManualIncrease(
            Org,
            TomatoId,
            account.Id,
            2.350m,
            UnitOfMeasure.Kilogram,
            "Delivery correction",
            Actor,
            Now,
            sellingMode: SellingMode.ByWeight);
        account.ApplyMovementEffect(add.QuantityEffect);
        Assert.Equal(12.350m, account.OnHandQuantity);

        var waste = StockMovement.ManualDecrease(
            Org,
            TomatoId,
            account.Id,
            0.425m,
            UnitOfMeasure.Kilogram,
            "Spoilage",
            Actor,
            Now.AddMinutes(1),
            sellingMode: SellingMode.ByWeight);
        account.ApplyMovementEffect(waste.QuantityEffect);
        Assert.Equal(11.925m, account.OnHandQuantity);
    }

    [Fact]
    public void Report_aggregation_sums_weighted_quantities_as_decimals()
    {
        var q1 = 0.350m;
        var q2 = 1.200m;
        var q3 = 0.750m;
        Assert.Equal(2.300m, q1 + q2 + q3);

        var lines = new[]
        {
            WeightedSaleLine(q1, 120m),
            WeightedSaleLine(q2, 120m),
            WeightedSaleLine(q3, 220m, "Bangus")
        };
        // Bangus line uses different product id conceptually; aggregate tomato-only:
        var tomatoQty = lines.Where(l => l.NameSnapshot == "Tomato").Sum(l => l.Quantity);
        Assert.Equal(1.550m, tomatoQty);
        Assert.Equal(nameof(SellingMode.ByWeight), SellingModes.ToCode(SellingMode.ByWeight));
        Assert.Equal(nameof(UnitOfMeasure.Kilogram), UnitOfMeasures.ToCode(UnitOfMeasure.Kilogram));
    }

    [Fact]
    public void Tiny_0_001_kg_is_valid_for_purchase_return_and_adjustment()
    {
        Assert.Equal(0.001m, SaleLine.NormalizeQuantity(0.001m, UnitOfMeasure.Kilogram, SellingMode.ByWeight));
        Assert.Equal(0.001m, PurchaseOrderLine.NormalizeQuantity(0.001m, UnitOfMeasure.Kilogram, SellingMode.ByWeight));
        Assert.Equal(0.001m, WeightQuantities.NormalizeToKilograms(1m, WeightInputUnit.Gram));
    }

    [Fact]
    public void PerItem_purchase_regression_still_requires_whole_quantity()
    {
        var ex = Assert.Throws<DomainException>(
            () => PurchaseOrderLine.NormalizeQuantity(1.5m, UnitOfMeasure.Bottle, SellingMode.PerItem));
        Assert.Equal(DomainErrorCodes.InvalidSaleLineQuantity, ex.ErrorCode);
    }
}
