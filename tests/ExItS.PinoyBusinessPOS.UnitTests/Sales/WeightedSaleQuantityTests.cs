using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Sales;

public sealed class WeightedSaleQuantityTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.NewGuid());
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);
    private static readonly CashierShiftId Shift = CashierShiftId.New();
    private static readonly RegisterId Register = RegisterId.New();

    private static SaleLineDraft Weighted(
        string name,
        decimal unitPrice,
        decimal qtyKg) =>
        new(
            CatalogProductId.New(),
            name,
            null,
            null,
            UnitOfMeasure.Kilogram,
            unitPrice,
            qtyKg,
            SellingMode.ByWeight);

    private static SaleLineDraft PerItem(
        string name,
        decimal unitPrice,
        decimal quantity,
        UnitOfMeasure unit = UnitOfMeasure.Bottle) =>
        new(
            CatalogProductId.New(),
            name,
            "SKU-1",
            null,
            unit,
            unitPrice,
            quantity,
            SellingMode.PerItem);

    private static Sale Checkout(params SaleLineDraft[] lines) =>
        Sale.Checkout(
            Org,
            SaleNumbers.Format(new DateOnly(2026, 8, 11), 1),
            SalePaymentMethod.Cash,
            lines,
            Actor,
            Now,
            amountTendered: 10_000m,
            cashierShiftId: Shift,
            registerId: Register);

    [Theory]
    [InlineData(1.200, 120, 144.00)]
    [InlineData(0.350, 120, 42.00)]
    [InlineData(0.750, 220, 165.00)]
    public void ByWeight_line_total_is_price_per_kg_times_quantity(decimal qty, decimal price, decimal expected)
    {
        var sale = Checkout(Weighted("Tomato", price, qty));
        var line = Assert.Single(sale.Lines);
        Assert.Equal(SellingMode.ByWeight, line.SellingModeSnapshot);
        Assert.Equal(UnitOfMeasure.Kilogram, line.UnitOfMeasureSnapshot);
        Assert.Equal(qty, line.Quantity);
        Assert.Equal(price, line.UnitPrice);
        Assert.Equal(expected, line.LineTotal);
        Assert.Equal(expected, sale.Total);
    }

    [Fact]
    public void Mixed_PerItem_and_ByWeight_cart_totals_correctly()
    {
        var sale = Checkout(
            PerItem("Coke", 25m, 2m),
            Weighted("Tomato", 120m, 1.200m));

        Assert.Equal(2, sale.Lines.Count);
        Assert.Equal(50.00m, sale.Lines[0].LineTotal);
        Assert.Equal(144.00m, sale.Lines[1].LineTotal);
        Assert.Equal(194.00m, sale.Total);
    }

    [Fact]
    public void ByWeight_accepts_fractional_kilograms()
    {
        Assert.Equal(0.350m, SaleLine.NormalizeQuantity(0.350m, UnitOfMeasure.Kilogram, SellingMode.ByWeight));
    }

    [Fact]
    public void PerItem_whole_unit_rejects_fractional_quantity()
    {
        var ex = Assert.Throws<DomainException>(
            () => SaleLine.NormalizeQuantity(1.5m, UnitOfMeasure.Bottle, SellingMode.PerItem));
        Assert.Equal(DomainErrorCodes.InvalidSaleLineQuantity, ex.ErrorCode);
    }

    [Fact]
    public void ByWeight_with_non_kilogram_unit_rejected_at_create()
    {
        var draft = new SaleLineDraft(
            CatalogProductId.New(),
            "Bad",
            null,
            null,
            UnitOfMeasure.Bottle,
            10m,
            1m,
            SellingMode.ByWeight);

        var ex = Assert.Throws<DomainException>(() => Checkout(draft));
        Assert.Equal(DomainErrorCodes.InvalidSellingModeUnit, ex.ErrorCode);
    }

    [Fact]
    public void Inventory_sale_deduction_preserves_exact_decimal_kilograms()
    {
        var productId = CatalogProductId.New();
        var account = InventoryAccount.CreateUntracked(Org, productId, Now);
        account.Enable(50.000m, UnitOfMeasure.Kilogram, Actor, Now, hasOpeningStockAlready: false);

        var first = StockMovement.SaleDeduction(
            Org,
            productId,
            account.Id,
            1.200m,
            UnitOfMeasure.Kilogram,
            Guid.NewGuid(),
            Actor,
            Now,
            sellingMode: SellingMode.ByWeight);
        account.ApplyMovementEffect(first.QuantityEffect);
        Assert.Equal(48.800m, account.OnHandQuantity);

        var second = StockMovement.SaleDeduction(
            Org,
            productId,
            account.Id,
            0.350m,
            UnitOfMeasure.Kilogram,
            Guid.NewGuid(),
            Actor,
            Now.AddMinutes(1),
            sellingMode: SellingMode.ByWeight);
        account.ApplyMovementEffect(second.QuantityEffect);
        Assert.Equal(48.450m, account.OnHandQuantity);
    }

    [Fact]
    public void Insufficient_stock_comparison_works_with_decimals()
    {
        var productId = CatalogProductId.New();
        var account = InventoryAccount.CreateUntracked(Org, productId, Now);
        account.Enable(0.500m, UnitOfMeasure.Kilogram, Actor, Now, hasOpeningStockAlready: false);

        Assert.True(account.OnHandQuantity < 0.600m);
        Assert.False(account.OnHandQuantity < 0.500m);
    }

    [Fact]
    public void Gram_input_normalizes_then_prices_as_kilograms()
    {
        var kg = WeightQuantities.NormalizeToKilograms(350m, WeightInputUnit.Gram);
        Assert.Equal(0.350m, kg);
        Assert.Equal(42.00m, SaleMoney.RoundMoney(120m * kg));
    }
}
