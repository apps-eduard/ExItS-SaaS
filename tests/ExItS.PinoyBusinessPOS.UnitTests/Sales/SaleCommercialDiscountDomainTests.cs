using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Sales;

/// <summary>
/// RMAP-B03 commercial sale discount. Covers the money contract: gross stays gross, net is what
/// totals use, allocations reconcile exactly, and quantity/unit price are never rewritten.
/// </summary>
public sealed class SaleCommercialDiscountDomainTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.NewGuid());
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 1, 0, 0, TimeSpan.Zero);
    private static readonly CashierShiftId Shift = CashierShiftId.New();
    private static readonly RegisterId Register = RegisterId.New();

    private const string Reason = "Regular customer courtesy";

    private static SaleLineDraft Draft(
        decimal unitPrice,
        decimal quantity,
        CatalogProductId? productId = null,
        UnitOfMeasure unit = UnitOfMeasure.Piece,
        SellingMode sellingMode = SellingMode.PerItem,
        string name = "Sardinas") =>
        new(
            productId ?? CatalogProductId.New(),
            name,
            "SKU-1",
            "4801234567890",
            unit,
            unitPrice,
            quantity,
            sellingMode);

    private static Sale Checkout(
        IReadOnlyList<SaleLineDraft> lines,
        IReadOnlyList<CommercialDiscountIntent>? discounts = null,
        decimal? tendered = 100_000m) =>
        Sale.Checkout(
            Org,
            SaleNumbers.Format(new DateOnly(2026, 8, 21), 1),
            SalePaymentMethod.Cash,
            lines,
            Actor,
            Now,
            tendered,
            cashierShiftId: Shift,
            registerId: Register,
            commercialDiscounts: discounts);

    private static CommercialDiscountIntent LinePercent(
        decimal percent,
        int? lineNumber = null,
        CatalogProductId? productId = null) =>
        new(SaleDiscountScope.Line, SaleDiscountMethod.Percentage, percent, Reason, productId, lineNumber);

    private static CommercialDiscountIntent LineFixed(
        decimal amount,
        int? lineNumber = null,
        CatalogProductId? productId = null) =>
        new(SaleDiscountScope.Line, SaleDiscountMethod.FixedAmount, amount, Reason, productId, lineNumber);

    private static CommercialDiscountIntent SalePercent(decimal percent) =>
        new(SaleDiscountScope.Sale, SaleDiscountMethod.Percentage, percent, Reason);

    private static CommercialDiscountIntent SaleFixed(decimal amount) =>
        new(SaleDiscountScope.Sale, SaleDiscountMethod.FixedAmount, amount, Reason);

    [Fact]
    public void Checkout_without_discounts_keeps_gross_equal_to_net_and_records_no_adjustments()
    {
        var sale = Checkout([Draft(25.50m, 3m), Draft(10m, 2m)]);

        Assert.Equal(96.50m, sale.GrossSubtotal);
        Assert.Equal(96.50m, sale.Subtotal);
        Assert.Equal(0m, sale.LineDiscountTotal);
        Assert.Equal(0m, sale.SaleDiscountTotal);
        Assert.Equal(0m, sale.DiscountTotal);
        Assert.Empty(sale.CommercialDiscounts);

        foreach (var line in sale.Lines)
        {
            Assert.Equal(line.GrossLineTotal, line.LineTotal);
            Assert.Equal(0m, line.TotalLineDiscount);
        }
    }

    [Fact]
    public void Line_percentage_discount_reduces_only_its_own_line()
    {
        var sale = Checkout([Draft(100m, 2m), Draft(50m, 1m)], [LinePercent(10m, lineNumber: 1)]);

        var first = sale.Lines[0];
        Assert.Equal(200m, first.GrossLineTotal);
        Assert.Equal(20m, first.LineDiscountAmount);
        Assert.Equal(0m, first.SaleDiscountAllocatedAmount);
        Assert.Equal(180m, first.LineTotal);

        Assert.Equal(50m, sale.Lines[1].LineTotal);
        Assert.Equal(0m, sale.Lines[1].TotalLineDiscount);

        Assert.Equal(250m, sale.GrossSubtotal);
        Assert.Equal(20m, sale.LineDiscountTotal);
        Assert.Equal(0m, sale.SaleDiscountTotal);
        Assert.Equal(20m, sale.DiscountTotal);
        Assert.Equal(230m, sale.Subtotal);
        Assert.Equal(230m, sale.Total);
    }

    [Fact]
    public void Line_fixed_discount_may_target_a_line_by_product()
    {
        var target = CatalogProductId.New();
        var sale = Checkout(
            [Draft(100m, 1m), Draft(80m, 1m, productId: target)],
            [LineFixed(12.34m, productId: target)]);

        Assert.Equal(0m, sale.Lines[0].TotalLineDiscount);
        Assert.Equal(12.34m, sale.Lines[1].LineDiscountAmount);
        Assert.Equal(67.66m, sale.Lines[1].LineTotal);
        Assert.Equal(167.66m, sale.Subtotal);

        var adjustment = Assert.Single(sale.CommercialDiscounts);
        Assert.Equal(SaleDiscountScope.Line, adjustment.Scope);
        Assert.Equal(SaleDiscountMethod.FixedAmount, adjustment.Method);
        Assert.Equal(12.34m, adjustment.RequestedValue);
        Assert.Equal(12.34m, adjustment.CalculatedAmount);
        Assert.Equal(sale.Lines[1].Id.Value, adjustment.SaleLineId!.Value);
        Assert.Equal(SaleDiscountSource.Manual, adjustment.Source);
        Assert.Equal(Actor, adjustment.AppliedBy);
    }

    [Fact]
    public void Line_scoped_discount_on_a_duplicated_product_must_name_its_line()
    {
        var duplicated = CatalogProductId.New();
        var error = Assert.Throws<DomainException>(() => Checkout(
            [Draft(100m, 1m, productId: duplicated), Draft(100m, 1m, productId: duplicated)],
            [LineFixed(5m, productId: duplicated)]));

        Assert.Equal(DomainErrorCodes.SaleDiscountLineAmbiguous, error.ErrorCode);
    }

    [Fact]
    public void Sale_percentage_discount_allocates_across_every_line()
    {
        var sale = Checkout([Draft(100m, 1m), Draft(300m, 1m)], [SalePercent(10m)]);

        Assert.Equal(400m, sale.GrossSubtotal);
        Assert.Equal(0m, sale.LineDiscountTotal);
        Assert.Equal(40m, sale.SaleDiscountTotal);
        Assert.Equal(360m, sale.Subtotal);
        Assert.Equal(10m, sale.Lines[0].SaleDiscountAllocatedAmount);
        Assert.Equal(30m, sale.Lines[1].SaleDiscountAllocatedAmount);
    }

    [Fact]
    public void Sale_fixed_discount_allocates_leftover_centavos_to_the_largest_remainder()
    {
        // 10.00 over bases 33.33 / 33.33 / 33.34: truncated shares are 3.33 / 3.33 / 3.33,
        // leaving one centavo that the largest discarded fraction (line 3) takes.
        var sale = Checkout(
            [Draft(33.33m, 1m), Draft(33.33m, 1m), Draft(33.34m, 1m)],
            [SaleFixed(10m)]);

        Assert.Equal(100m, sale.GrossSubtotal);
        Assert.Equal(10m, sale.SaleDiscountTotal);
        Assert.Equal(90m, sale.Subtotal);
        Assert.Equal([3.33m, 3.33m, 3.34m], sale.Lines.Select(l => l.SaleDiscountAllocatedAmount).ToArray());
        Assert.Equal(10m, sale.Lines.Sum(l => l.SaleDiscountAllocatedAmount));
    }

    [Fact]
    public void Sale_fixed_discount_breaks_an_exact_tie_by_lowest_line_number()
    {
        // Equal bases split 0.01 evenly at 0.005 each; the tie goes to line 1.
        var sale = Checkout([Draft(10m, 1m), Draft(10m, 1m)], [SaleFixed(0.01m)]);

        Assert.Equal([0.01m, 0m], sale.Lines.Select(l => l.SaleDiscountAllocatedAmount).ToArray());
        Assert.Equal(19.99m, sale.Subtotal);
    }

    [Fact]
    public void Line_and_sale_discounts_combine_and_reconcile_exactly()
    {
        var sale = Checkout(
            [Draft(100m, 1m), Draft(200m, 1m), Draft(700m, 1m)],
            [LinePercent(50m, lineNumber: 3), SalePercent(10m)]);

        // Line 3 drops to 350, so the sale-level base is 100 + 200 + 350 = 650.
        Assert.Equal(1000m, sale.GrossSubtotal);
        Assert.Equal(350m, sale.LineDiscountTotal);
        Assert.Equal(65m, sale.SaleDiscountTotal);
        Assert.Equal(415m, sale.DiscountTotal);
        Assert.Equal(585m, sale.Subtotal);

        Assert.Equal(sale.GrossSubtotal - sale.DiscountTotal, sale.Subtotal);
        Assert.Equal(sale.Subtotal, sale.Lines.Sum(l => l.LineTotal));
        Assert.Equal(sale.LineDiscountTotal, sale.Lines.Sum(l => l.LineDiscountAmount));
        Assert.Equal(sale.SaleDiscountTotal, sale.Lines.Sum(l => l.SaleDiscountAllocatedAmount));
        Assert.Equal(2, sale.CommercialDiscounts.Count);
        Assert.Equal(
            sale.SaleDiscountTotal,
            sale.CommercialDiscounts.Single(a => a.Scope == SaleDiscountScope.Sale).CalculatedAmount);
    }

    [Fact]
    public void Full_discount_drives_a_cash_total_to_zero()
    {
        var sale = Checkout([Draft(100m, 1m)], [SalePercent(100m)], tendered: 0m);

        Assert.Equal(100m, sale.GrossSubtotal);
        Assert.Equal(100m, sale.DiscountTotal);
        Assert.Equal(0m, sale.Subtotal);
        Assert.Equal(0m, sale.Total);
        Assert.Equal(0m, sale.AmountTendered);
        Assert.Equal(0m, sale.ChangeAmount);
    }

    [Fact]
    public void Utang_still_requires_a_positive_total_after_discount()
    {
        var error = Assert.Throws<DomainException>(() => Sale.Checkout(
            Org,
            SaleNumbers.Format(new DateOnly(2026, 8, 21), 1),
            SalePaymentMethod.Utang,
            [Draft(100m, 1m)],
            Actor,
            Now,
            customerId: POSCustomerId.New(),
            linkedCreditEntryId: CreditEntryId.New(),
            cashierShiftId: Shift,
            registerId: Register,
            commercialDiscounts: [SalePercent(100m)]));

        Assert.Equal(DomainErrorCodes.SaleUtangTotalMustBePositive, error.ErrorCode);
    }

    [Fact]
    public void Discount_cannot_exceed_the_eligible_base()
    {
        var lineTooLarge = Assert.Throws<DomainException>(() => Checkout(
            [Draft(100m, 1m), Draft(50m, 1m)],
            [LineFixed(100.01m, lineNumber: 1)]));
        Assert.Equal(DomainErrorCodes.SaleDiscountExceedsEligible, lineTooLarge.ErrorCode);

        var saleTooLarge = Assert.Throws<DomainException>(() => Checkout(
            [Draft(100m, 1m), Draft(50m, 1m)],
            [SaleFixed(150.01m)]));
        Assert.Equal(DomainErrorCodes.SaleDiscountExceedsEligible, saleTooLarge.ErrorCode);

        var stacked = Assert.Throws<DomainException>(() => Checkout(
            [Draft(100m, 1m)],
            [LinePercent(100m, lineNumber: 1), SaleFixed(1m)]));
        Assert.Equal(DomainErrorCodes.SaleDiscountExceedsEligible, stacked.ErrorCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_reason_is_rejected(string? reason)
    {
        var error = Assert.Throws<DomainException>(() => Checkout(
            [Draft(100m, 1m)],
            [new CommercialDiscountIntent(
                SaleDiscountScope.Sale,
                SaleDiscountMethod.Percentage,
                10m,
                reason!)]));

        Assert.Equal(DomainErrorCodes.SaleDiscountReasonRequired, error.ErrorCode);
    }

    [Fact]
    public void Percentage_outside_zero_to_one_hundred_is_rejected()
    {
        Assert.Equal(
            DomainErrorCodes.SaleDiscountInvalidPercent,
            Assert.Throws<DomainException>(() => Checkout([Draft(100m, 1m)], [SalePercent(0m)])).ErrorCode);
        Assert.Equal(
            DomainErrorCodes.SaleDiscountInvalidPercent,
            Assert.Throws<DomainException>(() => Checkout([Draft(100m, 1m)], [SalePercent(100.01m)])).ErrorCode);
    }

    [Fact]
    public void Fixed_amount_must_be_positive_with_at_most_two_decimals()
    {
        Assert.Equal(
            DomainErrorCodes.SaleDiscountInvalidAmount,
            Assert.Throws<DomainException>(() => Checkout([Draft(100m, 1m)], [SaleFixed(0m)])).ErrorCode);
        Assert.Equal(
            DomainErrorCodes.SaleDiscountInvalidAmount,
            Assert.Throws<DomainException>(() => Checkout([Draft(100m, 1m)], [SaleFixed(1.234m)])).ErrorCode);
    }

    [Fact]
    public void Weighted_line_discount_reduces_money_and_leaves_quantity_untouched()
    {
        var sale = Checkout(
            [Draft(60m, 2.345m, unit: UnitOfMeasure.Kilogram, sellingMode: SellingMode.ByWeight, name: "Bigas")],
            [LinePercent(10m, lineNumber: 1)]);

        var line = sale.Lines[0];
        Assert.Equal(2.345m, line.Quantity);
        Assert.Equal(60m, line.UnitPrice);
        Assert.Equal(140.70m, line.GrossLineTotal);
        Assert.Equal(14.07m, line.LineDiscountAmount);
        Assert.Equal(126.63m, line.LineTotal);
        Assert.Equal(126.63m, sale.Subtotal);
    }

    [Fact]
    public void Selling_unit_conversion_discounts_the_entered_quantity_price_not_the_base_quantity()
    {
        var draft = new SaleLineDraft(
            CatalogProductId.New(),
            "Bigas 25kg sako",
            "SKU-SACK",
            null,
            UnitOfMeasure.Kilogram,
            UnitPrice: 1_500m,
            Quantity: 0m,
            SellingMode.PerItem,
            SellingUnitId: ProductUnitId.New(),
            SellingUnitNameSnapshot: "Sako",
            EnteredQuantity: 2m,
            MultiplierToBaseSnapshot: 25m);

        var sale = Checkout([draft], [LinePercent(5m, lineNumber: 1)]);
        var line = sale.Lines[0];

        Assert.Equal(50m, line.Quantity);
        Assert.Equal(2m, line.EnteredQuantity);
        Assert.Equal(3_000m, line.GrossLineTotal);
        Assert.Equal(150m, line.LineDiscountAmount);
        Assert.Equal(2_850m, line.LineTotal);
    }

    [Fact]
    public void Quote_matches_what_checkout_records()
    {
        var lines = new[] { Draft(100m, 1m), Draft(200m, 1m), Draft(700m, 1m) };
        var intents = new[] { LinePercent(50m, lineNumber: 3), SalePercent(10m) };

        var quote = Sale.QuoteCommercialDiscounts(Org, lines, intents);
        var sale = Checkout(lines, intents);

        Assert.Equal(sale.GrossSubtotal, quote.GrossSubtotal);
        Assert.Equal(sale.LineDiscountTotal, quote.LineDiscountTotal);
        Assert.Equal(sale.SaleDiscountTotal, quote.SaleDiscountTotal);
        Assert.Equal(sale.DiscountTotal, quote.DiscountTotal);
        Assert.Equal(sale.Subtotal, quote.NetSubtotal);
        Assert.Equal(
            sale.Lines.Select(l => l.LineTotal).ToArray(),
            quote.Lines.OrderBy(l => l.LineNumber).Select(l => l.NetLineTotal).ToArray());
    }

    [Fact]
    public void Legacy_rehydrated_rows_default_every_discount_field_to_zero()
    {
        var saleId = SaleId.New();
        var line = SaleLine.Rehydrate(
            SaleLineId.New(),
            saleId,
            Org,
            CatalogProductId.New(),
            1,
            "Sardinas",
            "SKU-1",
            null,
            UnitOfMeasure.Piece,
            unitPrice: 25m,
            quantity: 2m,
            lineTotal: 50m);

        Assert.Equal(50m, line.GrossLineTotal);
        Assert.Equal(0m, line.LineDiscountAmount);
        Assert.Equal(0m, line.SaleDiscountAllocatedAmount);
        Assert.Equal(0m, line.TotalLineDiscount);
        Assert.Equal(50m, line.LineTotal);

        var sale = Sale.Rehydrate(
            saleId,
            Org,
            "SALE-20260730-000001",
            SaleStatus.Completed,
            SalePaymentMethod.Cash,
            subtotal: 50m,
            total: 50m,
            taxAmount: 0m,
            amountTendered: 50m,
            changeAmount: 0m,
            gcashReference: null,
            recordedAtUtc: Now,
            recordedBy: Actor,
            voidedAtUtc: null,
            voidedBy: null,
            voidReason: null,
            updatedAtUtc: Now,
            lines: [line]);

        Assert.Equal(50m, sale.GrossSubtotal);
        Assert.Equal(0m, sale.LineDiscountTotal);
        Assert.Equal(0m, sale.SaleDiscountTotal);
        Assert.Equal(0m, sale.DiscountTotal);
        Assert.Empty(sale.CommercialDiscounts);
    }

    [Fact]
    public void Too_many_intents_are_rejected()
    {
        var intents = Enumerable
            .Range(0, SaleCommercialDiscountRules.MaxIntentCount + 1)
            .Select(_ => SalePercent(1m))
            .ToArray();

        var error = Assert.Throws<DomainException>(() => Checkout([Draft(1_000m, 1m)], intents));
        Assert.Equal(DomainErrorCodes.SaleDiscountTooMany, error.ErrorCode);
    }
}
