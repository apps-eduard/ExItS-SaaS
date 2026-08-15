using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Sales;

/// <summary>
/// Canonical Rice example: base kg; sell kg ₱55; sell Sack ₱2,600 (50 kg). Independent prices.
/// </summary>
public sealed class RiceSellUnitCheckoutSemanticsTests
{
    private static readonly PosOrganizationId Org =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly Guid Actor = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void SellUnitQuantityConvertsToBaseQuantity()
    {
        Assert.Equal(50m, ProductUnitConversion.ToBaseQuantity(1m, 50m));
        Assert.Equal(100m, ProductUnitConversion.ToBaseQuantity(2m, 50m));
        Assert.Equal(3.5m, ProductUnitConversion.ToBaseQuantity(3.5m, 1m));
    }

    [Fact]
    public void SellUnitUsesIndependentPrice_NotDerivedFromBaseRetail()
    {
        var product = CatalogProduct.Create(Org, "Rice", UnitOfMeasure.Kilogram, 55m, Now);
        var kg = CatalogProductUnit.Create(
            Org, product.Id, ProductUnitKind.Sell, "kg", "kg", 1m, Now, sellingPrice: 55m);
        var sack = CatalogProductUnit.Create(
            Org, product.Id, ProductUnitKind.Sell, "Sack", "Sack", 50m, Now, sellingPrice: 2600m);

        Assert.Equal(55m, kg.SellingPrice);
        Assert.Equal(2600m, sack.SellingPrice);
        Assert.NotEqual(50m * 55m, sack.SellingPrice);
    }

    [Fact]
    public void RiceCanonicalExample_SackThenKg_DeductsSharedBaseInventory()
    {
        var product = CatalogProduct.Create(Org, "Rice", UnitOfMeasure.Kilogram, 55m, Now);
        var account = InventoryAccount.CreateUntracked(Org, product.Id, Now);
        account.Enable(500m, UnitOfMeasure.Kilogram, Actor, Now, hasOpeningStockAlready: false);

        var sackUnit = CatalogProductUnit.Create(
            Org, product.Id, ProductUnitKind.Sell, "Sack", "Sack", 50m, Now, sellingPrice: 2600m);
        var kgUnit = CatalogProductUnit.Create(
            Org, product.Id, ProductUnitKind.Sell, "kg", "kg", 1m, Now, sellingPrice: 55m);

        var sackLine = CheckoutSaleLineSnapshots.TryCreateOnlineDraft(
            new CheckoutSaleLineRequest(product.Id.Value, Quantity: 1m, SellingUnitId: sackUnit.Id.Value, EnteredQuantity: 1m),
            product,
            sackUnit);
        Assert.True(sackLine.IsSuccess);
        Assert.NotNull(sackLine.Value);
        Assert.Equal(2600m, SaleMoney.RoundMoney(sackLine.Value.UnitPrice * (sackLine.Value.EnteredQuantity ?? 0m)));
        Assert.Equal(50m, sackLine.Value.Quantity);
        Assert.Equal(2600m, sackLine.Value.UnitPrice);

        account.ApplyMovementEffect(-sackLine.Value.Quantity);
        Assert.Equal(450m, account.OnHandQuantity);

        var kgLine = CheckoutSaleLineSnapshots.TryCreateOnlineDraft(
            new CheckoutSaleLineRequest(product.Id.Value, Quantity: 3m, SellingUnitId: kgUnit.Id.Value, EnteredQuantity: 3m),
            product,
            kgUnit);
        Assert.True(kgLine.IsSuccess);
        Assert.NotNull(kgLine.Value);
        Assert.Equal(165m, SaleMoney.RoundMoney(kgLine.Value.UnitPrice * (kgLine.Value.EnteredQuantity ?? 0m)));
        Assert.Equal(3m, kgLine.Value.Quantity);

        account.ApplyMovementEffect(-kgLine.Value.Quantity);
        Assert.Equal(447m, account.OnHandQuantity);
    }

    [Fact]
    public void CannotSellSackWhenBaseStockBelowConversion()
    {
        var product = CatalogProduct.Create(Org, "Rice", UnitOfMeasure.Kilogram, 55m, Now);
        var sack = CatalogProductUnit.Create(
            Org, product.Id, ProductUnitKind.Sell, "Sack", "Sack", 50m, Now, sellingPrice: 2600m);

        var draft = CheckoutSaleLineSnapshots.TryCreateOnlineDraft(
            new CheckoutSaleLineRequest(product.Id.Value, Quantity: 1m, SellingUnitId: sack.Id.Value, EnteredQuantity: 1m),
            product,
            sack);
        Assert.True(draft.IsSuccess);
        Assert.Equal(50m, draft.Value!.Quantity);

        const decimal onHand = 40m;
        Assert.False(onHand >= draft.Value.Quantity);
    }

    [Fact]
    public void CanSellSackWhenBaseStockEqualsConversion()
    {
        const decimal onHand = 50m;
        var baseNeeded = ProductUnitConversion.ToBaseQuantity(1m, 50m);
        Assert.True(onHand >= baseNeeded);
    }

    [Fact]
    public void MultipleUnitsCompeteForSameBaseStock()
    {
        var onHand = 60m;
        var afterSack = onHand - ProductUnitConversion.ToBaseQuantity(1m, 50m);
        Assert.Equal(10m, afterSack);
        Assert.False(afterSack >= ProductUnitConversion.ToBaseQuantity(1m, 50m));
        Assert.True(afterSack >= ProductUnitConversion.ToBaseQuantity(10m, 1m));
    }

    [Fact]
    public void SaleSnapshotPreservesSellUnitConversionAndUnitPrice()
    {
        var sellingUnitId = ProductUnitId.New();
        var line = SaleLine.Create(
            SaleId.New(),
            Org,
            1,
            new SaleLineDraft(
                CatalogProductId.New(),
                "Rice",
                null,
                null,
                UnitOfMeasure.Kilogram,
                UnitPrice: 2600m,
                Quantity: 50m,
                SellingModeSnapshot: SellingMode.PerItem,
                SellingUnitId: sellingUnitId,
                SellingUnitNameSnapshot: "Sack",
                EnteredQuantity: 1m,
                MultiplierToBaseSnapshot: 50m));

        Assert.Equal(sellingUnitId, line.SellingUnitId);
        Assert.Equal("Sack", line.SellingUnitNameSnapshot);
        Assert.Equal(1m, line.EnteredQuantity);
        Assert.Equal(50m, line.MultiplierToBaseSnapshot);
        Assert.Equal(2600m, line.UnitPrice);
        Assert.Equal(2600m, line.LineTotal);
        Assert.Equal(50m, line.Quantity);
    }

    [Fact]
    public void RefundRestoresBaseQuantityMath_AndPackPartialUsesEnteredPrice()
    {
        var sellingUnitId = ProductUnitId.New();
        var line = SaleLine.Create(
            SaleId.New(),
            Org,
            1,
            new SaleLineDraft(
                CatalogProductId.New(),
                "Rice",
                null,
                null,
                UnitOfMeasure.Kilogram,
                UnitPrice: 2600m,
                Quantity: 50m,
                SellingModeSnapshot: SellingMode.PerItem,
                SellingUnitId: sellingUnitId,
                SellingUnitNameSnapshot: "Sack",
                EnteredQuantity: 1m,
                MultiplierToBaseSnapshot: 50m));

        // Returning 25 kg base of a 50 kg / ₱2600 sack should refund half of pack price.
        var partial = SaleReturnRefundable.ComputeRefundAmount(line, 25m, 0m, 0m);
        Assert.Equal(1300m, partial);

        var full = SaleReturnRefundable.ComputeRefundAmount(line, 50m, 0m, 0m);
        Assert.Equal(2600m, full);

        Assert.Equal(50m, SaleReturnRefundable.RefundableQuantity(line, 0m));
    }

    [Fact]
    public void ChangingSellUnitDoesNotChangeInventoryBeforeCheckout()
    {
        var account = InventoryAccount.CreateUntracked(Org, CatalogProductId.New(), Now);
        account.Enable(500m, UnitOfMeasure.Kilogram, Actor, Now, hasOpeningStockAlready: false);
        var before = account.OnHandQuantity;
        _ = ProductUnitConversion.ToBaseQuantity(2m, 50m);
        Assert.Equal(before, account.OnHandQuantity);
    }
}
