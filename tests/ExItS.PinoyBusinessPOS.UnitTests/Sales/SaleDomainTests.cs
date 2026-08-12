using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Sales;

public sealed class SaleDomainTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.NewGuid());
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 30, 4, 15, 0, TimeSpan.Zero);

    private static SaleLineDraft Draft(
        decimal unitPrice,
        decimal quantity,
        UnitOfMeasure unit = UnitOfMeasure.Piece,
        string name = "Sardinas") =>
        new(CatalogProductId.New(), name, "SKU-1", "4801234567890", unit, unitPrice, quantity);

    private static readonly CashierShiftId Shift = CashierShiftId.New();
    private static readonly RegisterId Register = RegisterId.New();

    private static Sale Checkout(
        IReadOnlyList<SaleLineDraft> lines,
        SalePaymentMethod method = SalePaymentMethod.Cash,
        decimal? tendered = 10_000m,
        string? reference = null,
        CashierShiftId? shiftId = null,
        RegisterId? registerId = null) =>
        Sale.Checkout(
            Org,
            SaleNumbers.Format(new DateOnly(2026, 7, 30), 1),
            method,
            lines,
            Actor,
            Now,
            method == SalePaymentMethod.Cash ? tendered : null,
            reference,
            cashierShiftId: shiftId ?? Shift,
            registerId: registerId ?? Register);

    [Fact]
    public void Checkout_records_completed_sale_with_numbered_lines_and_totals()
    {
        var sale = Checkout([Draft(25.50m, 3m), Draft(10m, 2m, name: "Kape")]);

        Assert.Equal(SaleStatus.Completed, sale.Status);
        Assert.Equal("SALE-20260730-000001", sale.SaleNumber);
        Assert.Equal(Actor, sale.RecordedBy);
        Assert.Equal(Now, sale.RecordedAtUtc);
        Assert.Null(sale.VoidedAtUtc);
        Assert.Null(sale.VoidedBy);
        Assert.Null(sale.VoidReason);
        Assert.Equal(new[] { 1, 2 }, sale.Lines.Select(l => l.LineNumber).ToArray());
        Assert.Equal(96.50m, sale.Subtotal);
        Assert.Equal(sale.Subtotal, sale.Total);
    }

    [Fact]
    public void Money_rounds_half_away_from_zero_like_credit_and_repayment()
    {
        // Banker's rounding would give 2.00, 0.12, and 2.34 instead.
        Assert.Equal(2.01m, SaleMoney.RoundMoney(1.005m * 2m));
        Assert.Equal(0.25m, SaleMoney.RoundMoney(0.125m * 2m));
        Assert.Equal(2.35m, SaleMoney.RoundMoney(2.345m));
        Assert.Equal(-2.35m, SaleMoney.RoundMoney(-2.345m));
    }

    [Fact]
    public void Line_total_uses_away_from_zero_rounding_on_the_product()
    {
        // 0.05 x 2.5 kg = 0.125, which banker's rounding would take down to 0.12.
        var sale = Sale.Checkout(
            Org,
            SaleNumbers.Format(new DateOnly(2026, 7, 30), 1),
            SalePaymentMethod.Cash,
            [Draft(0.05m, 2.5m, UnitOfMeasure.Kilogram)],
            Actor,
            Now,
            10m,
            cashierShiftId: Shift, registerId: Register);

        Assert.Equal(0.13m, sale.Lines[0].LineTotal);
        Assert.Equal(0.13m, sale.Total);
        Assert.Equal(9.87m, sale.ChangeAmount);
    }

    [Fact]
    public void Sale_total_rounds_after_summing_rounded_line_totals()
    {
        var sale = Checkout([Draft(0.33m, 3m), Draft(0.34m, 3m)]);

        Assert.Equal(0.99m, sale.Lines[0].LineTotal);
        Assert.Equal(1.02m, sale.Lines[1].LineTotal);
        Assert.Equal(2.01m, sale.Total);
    }

    [Theory]
    [InlineData(UnitOfMeasure.Piece)]
    [InlineData(UnitOfMeasure.Pack)]
    [InlineData(UnitOfMeasure.Box)]
    [InlineData(UnitOfMeasure.Bottle)]
    [InlineData(UnitOfMeasure.Can)]
    [InlineData(UnitOfMeasure.Sachet)]
    public void Countable_units_reject_fractional_quantities(UnitOfMeasure unit)
    {
        Assert.True(SaleMoney.IsWholeUnit(unit));
        Assert.Equal(0, SaleMoney.MaxQuantityDecimals(unit));

        var error = Assert.Throws<DomainException>(() => Checkout([Draft(10m, 1.5m, unit)]));
        Assert.Equal(DomainErrorCodes.InvalidSaleLineQuantity, error.ErrorCode);

        Assert.Equal(2m, SaleLine.NormalizeQuantity(2m, unit));
    }

    [Theory]
    [InlineData(UnitOfMeasure.Kilogram)]
    [InlineData(UnitOfMeasure.Gram)]
    [InlineData(UnitOfMeasure.Liter)]
    [InlineData(UnitOfMeasure.Milliliter)]
    [InlineData(UnitOfMeasure.Meter)]
    public void Measured_units_allow_three_decimals_but_reject_four(UnitOfMeasure unit)
    {
        Assert.False(SaleMoney.IsWholeUnit(unit));
        Assert.Equal(3, SaleMoney.MaxQuantityDecimals(unit));
        Assert.Equal(0.125m, SaleLine.NormalizeQuantity(0.125m, unit));

        var error = Assert.Throws<DomainException>(() => Checkout([Draft(10m, 0.1255m, unit)]));
        Assert.Equal(DomainErrorCodes.InvalidSaleLineQuantity, error.ErrorCode);
    }

    [Fact]
    public void Quantity_must_be_greater_than_zero()
    {
        foreach (var quantity in new[] { 0m, -1m, -0.001m })
        {
            var error = Assert.Throws<DomainException>(
                () => SaleLine.NormalizeQuantity(quantity, UnitOfMeasure.Kilogram));
            Assert.Equal(DomainErrorCodes.InvalidSaleLineQuantity, error.ErrorCode);
        }
    }

    [Fact]
    public void Sale_requires_at_least_one_line()
    {
        var error = Assert.Throws<DomainException>(() => Checkout([]));
        Assert.Equal(DomainErrorCodes.SaleRequiresAtLeastOneLine, error.ErrorCode);
    }

    [Fact]
    public void Cash_sale_derives_change_from_tender()
    {
        var sale = Checkout([Draft(25.50m, 3m)], tendered: 100m);

        Assert.Equal(76.50m, sale.Total);
        Assert.Equal(100m, sale.AmountTendered);
        Assert.Equal(23.50m, sale.ChangeAmount);
        Assert.Null(sale.GCashReference);
    }

    [Fact]
    public void Cash_sale_with_exact_tender_has_zero_change()
    {
        var sale = Checkout([Draft(50m, 1m)], tendered: 50m);

        Assert.Equal(50m, sale.AmountTendered);
        Assert.Equal(0m, sale.ChangeAmount);
    }

    [Fact]
    public void Cash_sale_rejects_tender_below_total()
    {
        var error = Assert.Throws<DomainException>(() => Checkout([Draft(50m, 1m)], tendered: 49.99m));
        Assert.Equal(DomainErrorCodes.SaleAmountTenderedBelowTotal, error.ErrorCode);
    }

    [Fact]
    public void Cash_sale_requires_a_tender()
    {
        var error = Assert.Throws<DomainException>(() => Checkout([Draft(50m, 1m)], tendered: null));
        Assert.Equal(DomainErrorCodes.InvalidSaleAmountTendered, error.ErrorCode);
    }

    [Fact]
    public void Manual_gcash_sale_trims_optional_reference_and_carries_no_tender()
    {
        var sale = Checkout(
            [Draft(50m, 1m)],
            SalePaymentMethod.ManualGCash,
            reference: "  REF-12345  ");

        Assert.Equal(SalePaymentMethod.ManualGCash, sale.PaymentMethod);
        Assert.Equal("REF-12345", sale.GCashReference);
        Assert.Null(sale.AmountTendered);
        Assert.Null(sale.ChangeAmount);
    }

    [Fact]
    public void Manual_gcash_sale_allows_no_reference()
    {
        var sale = Checkout([Draft(50m, 1m)], SalePaymentMethod.ManualGCash, reference: "   ");
        Assert.Null(sale.GCashReference);
    }

    [Fact]
    public void Manual_gcash_reference_has_a_maximum_length()
    {
        var error = Assert.Throws<DomainException>(() => Checkout(
            [Draft(50m, 1m)],
            SalePaymentMethod.ManualGCash,
            reference: new string('R', Sale.GCashReferenceMaxLength + 1)));
        Assert.Equal(DomainErrorCodes.InvalidSaleGCashReference, error.ErrorCode);
    }

    [Fact]
    public void Gcash_reference_is_rejected_on_a_cash_sale()
    {
        var error = Assert.Throws<DomainException>(
            () => Sale.NormalizeGCashReference(SalePaymentMethod.Cash, "REF-1"));
        Assert.Equal(DomainErrorCodes.InvalidSaleGCashReference, error.ErrorCode);
    }

    [Fact]
    public void Manual_gcash_sale_rejects_a_tendered_amount()
    {
        var error = Assert.Throws<DomainException>(() => Sale.Checkout(
            Org,
            SaleNumbers.Format(new DateOnly(2026, 7, 30), 1),
            SalePaymentMethod.ManualGCash,
            [Draft(50m, 1m)],
            Actor,
            Now,
            amountTendered: 50m,
            cashierShiftId: Shift, registerId: Register));
        Assert.Equal(DomainErrorCodes.InvalidSaleAmountTendered, error.ErrorCode);
    }

    [Fact]
    public void Void_moves_completed_to_voided_with_reason_and_actor()
    {
        var sale = Checkout([Draft(50m, 1m)]);
        var voidedBy = Guid.NewGuid();
        var voidedAt = Now.AddMinutes(5);

        sale.Void("  Wrong item scanned  ", voidedBy, voidedAt);

        Assert.Equal(SaleStatus.Voided, sale.Status);
        Assert.Equal("Wrong item scanned", sale.VoidReason);
        Assert.Equal(voidedBy, sale.VoidedBy);
        Assert.Equal(voidedAt, sale.VoidedAtUtc);
        Assert.Equal(voidedAt, sale.UpdatedAtUtc);

        // Totals and lines are never rewritten by a void.
        Assert.Equal(50m, sale.Total);
        Assert.Single(sale.Lines);
    }

    [Fact]
    public void Void_is_rejected_on_an_already_voided_sale()
    {
        var sale = Checkout([Draft(50m, 1m)]);
        sale.Void("First", Actor, Now);

        var error = Assert.Throws<DomainException>(() => sale.Void("Second", Actor, Now));
        Assert.Equal(DomainErrorCodes.InvalidSaleStatusTransition, error.ErrorCode);
    }

    [Fact]
    public void Void_requires_a_reason_and_an_actor()
    {
        var sale = Checkout([Draft(50m, 1m)]);

        var missingReason = Assert.Throws<DomainException>(() => sale.Void("   ", Actor, Now));
        Assert.Equal(DomainErrorCodes.InvalidSaleVoidReason, missingReason.ErrorCode);

        var missingActor = Assert.Throws<DomainException>(() => sale.Void("Reason", Guid.Empty, Now));
        Assert.Equal(DomainErrorCodes.InvalidSaleActor, missingActor.ErrorCode);

        var tooLong = Assert.Throws<DomainException>(
            () => sale.Void(new string('x', Sale.VoidReasonMaxLength + 1), Actor, Now));
        Assert.Equal(DomainErrorCodes.InvalidSaleVoidReason, tooLong.ErrorCode);

        Assert.Equal(SaleStatus.Completed, sale.Status);
    }

    [Fact]
    public void Checkout_requires_a_utc_timestamp_and_an_actor()
    {
        var localTime = new DateTimeOffset(2026, 7, 30, 12, 0, 0, TimeSpan.FromHours(8));

        var notUtc = Assert.Throws<DomainException>(() => Sale.Checkout(
            Org,
            SaleNumbers.Format(new DateOnly(2026, 7, 30), 1),
            SalePaymentMethod.Cash,
            [Draft(50m, 1m)],
            Actor,
            localTime,
            50m,
            cashierShiftId: Shift, registerId: Register));
        Assert.Equal(DomainErrorCodes.InvalidUtcTimestamp, notUtc.ErrorCode);

        var noActor = Assert.Throws<DomainException>(() => Sale.Checkout(
            Org,
            SaleNumbers.Format(new DateOnly(2026, 7, 30), 1),
            SalePaymentMethod.Cash,
            [Draft(50m, 1m)],
            Guid.Empty,
            Now,
            50m,
            cashierShiftId: Shift, registerId: Register));
        Assert.Equal(DomainErrorCodes.InvalidSaleActor, noActor.ErrorCode);
    }

    [Fact]
    public void Line_snapshots_are_trimmed_and_preserved_independently_of_the_catalog()
    {
        var productId = CatalogProductId.New();
        var sale = Sale.Checkout(
            Org,
            SaleNumbers.Format(new DateOnly(2026, 7, 30), 7),
            SalePaymentMethod.Cash,
            [new SaleLineDraft(productId, "  Bigas  ", "  SKU-9  ", "  4800000000017  ", UnitOfMeasure.Kilogram, 62m, 1.5m)],
            Actor,
            Now,
            100m,
            cashierShiftId: Shift, registerId: Register);

        var line = Assert.Single(sale.Lines);
        Assert.Equal(productId, line.ProductId);
        Assert.Equal("Bigas", line.NameSnapshot);
        Assert.Equal("SKU-9", line.SkuSnapshot);
        Assert.Equal("4800000000017", line.BarcodeSnapshot);
        Assert.Equal(UnitOfMeasure.Kilogram, line.UnitOfMeasureSnapshot);
        Assert.Equal(62m, line.UnitPrice);
        Assert.Equal(1.5m, line.Quantity);
        Assert.Equal(93m, line.LineTotal);
    }

    [Fact]
    public void Line_unit_price_must_be_non_negative_with_at_most_two_decimals()
    {
        var negative = Assert.Throws<DomainException>(() => SaleLine.NormalizeUnitPrice(-0.01m));
        Assert.Equal(DomainErrorCodes.InvalidSaleLineUnitPrice, negative.ErrorCode);

        var tooPrecise = Assert.Throws<DomainException>(() => SaleLine.NormalizeUnitPrice(1.001m));
        Assert.Equal(DomainErrorCodes.InvalidSaleLineUnitPrice, tooPrecise.ErrorCode);

        Assert.Equal(0m, SaleLine.NormalizeUnitPrice(0m));
    }

    [Fact]
    public void Payment_method_codes_are_stable_and_parseable()
    {
        Assert.Equal(new[] { "Cash", "ManualGCash", "Utang", "Card", "GCash" }, SalePaymentMethods.Codes.ToArray());
        Assert.Equal("Cash", SalePaymentMethods.ToCode(SalePaymentMethod.Cash));
        Assert.Equal("ManualGCash", SalePaymentMethods.ToCode(SalePaymentMethod.ManualGCash));
        Assert.Equal("Utang", SalePaymentMethods.ToCode(SalePaymentMethod.Utang));
        Assert.Equal("Card", SalePaymentMethods.ToCode(SalePaymentMethod.Card));
        Assert.Equal("GCash", SalePaymentMethods.ToCode(SalePaymentMethod.GCash));
        Assert.Equal(SalePaymentMethod.ManualGCash, SalePaymentMethods.Parse("manualgcash"));
        Assert.Equal(SalePaymentMethod.Utang, SalePaymentMethods.Parse("utang"));
        Assert.Equal(SalePaymentMethod.Card, SalePaymentMethods.Parse("card"));
        Assert.Equal(SalePaymentMethod.GCash, SalePaymentMethods.Parse("gcash"));

        var error = Assert.Throws<DomainException>(() => SalePaymentMethods.Parse("SplitTender"));
        Assert.Equal(DomainErrorCodes.InvalidSalePaymentMethod, error.ErrorCode);
    }

    [Fact]
    public void Sale_numbers_are_formatted_and_normalized_per_business_date()
    {
        Assert.Equal("SALE-20260730-000001", SaleNumbers.Format(new DateOnly(2026, 7, 30), 1));
        Assert.Equal("SALE-20260101-012345", SaleNumbers.Format(new DateOnly(2026, 1, 1), 12_345));
        Assert.Equal("SALE-20260730-000001", SaleNumbers.Normalize(" sale-20260730-000001 "));

        Assert.Equal(new DateOnly(2026, 7, 30), SaleNumbers.BusinessDateOf(Now));

        foreach (var invalid in new[] { "", "SALE-2026-1", "INVOICE-20260730-000001", "SALE-20260730-1" })
        {
            var error = Assert.Throws<DomainException>(() => SaleNumbers.Normalize(invalid));
            Assert.Equal(DomainErrorCodes.InvalidSaleNumber, error.ErrorCode);
        }

        var outOfRange = Assert.Throws<DomainException>(() => SaleNumbers.Format(new DateOnly(2026, 7, 30), 0));
        Assert.Equal(DomainErrorCodes.InvalidSaleNumber, outOfRange.ErrorCode);
    }

    [Fact]
    public void Cash_checkout_may_attach_customer_without_credit_link()
    {
        var customerId = POSCustomerId.New();
        var sale = Sale.Checkout(
            Org,
            SaleNumbers.Format(new DateOnly(2026, 7, 30), 1),
            SalePaymentMethod.Cash,
            [Draft(50m, 1m)],
            Actor,
            Now,
            amountTendered: 100m,
            customerId: customerId,
            linkedCreditEntryId: null,
            cashierShiftId: Shift,
            registerId: Register);

        Assert.Equal(SalePaymentMethod.Cash, sale.PaymentMethod);
        Assert.Equal(customerId, sale.CustomerId);
        Assert.Null(sale.LinkedCreditEntryId);
        Assert.Equal(50m, sale.ChangeAmount);
    }

    [Fact]
    public void Cash_checkout_still_rejects_linked_credit_entry()
    {
        var error = Assert.Throws<DomainException>(() => Sale.Checkout(
            Org,
            SaleNumbers.Format(new DateOnly(2026, 7, 30), 1),
            SalePaymentMethod.Cash,
            [Draft(50m, 1m)],
            Actor,
            Now,
            amountTendered: 50m,
            customerId: POSCustomerId.New(),
            linkedCreditEntryId: CreditEntryId.New(),
            cashierShiftId: Shift,
            registerId: Register));
        Assert.Equal(DomainErrorCodes.SaleCashMustNotLinkCredit, error.ErrorCode);
    }

    [Fact]
    public void Utang_checkout_records_customer_and_linked_credit_without_tender()
    {
        var customerId = POSCustomerId.New();
        var creditEntryId = CreditEntryId.New();

        var sale = Sale.Checkout(
            Org,
            SaleNumbers.Format(new DateOnly(2026, 7, 30), 1),
            SalePaymentMethod.Utang,
            [Draft(50m, 2m)],
            Actor,
            Now,
            amountTendered: null,
            customerId: customerId,
            linkedCreditEntryId: creditEntryId,
            cashierShiftId: Shift, registerId: Register);

        Assert.Equal(SalePaymentMethod.Utang, sale.PaymentMethod);
        Assert.Equal(100m, sale.Total);
        Assert.Equal(customerId, sale.CustomerId);
        Assert.Equal(creditEntryId, sale.LinkedCreditEntryId);
        Assert.Null(sale.AmountTendered);
        Assert.Null(sale.ChangeAmount);
        Assert.Null(sale.GCashReference);
    }

    [Fact]
    public void Utang_checkout_rejects_zero_total()
    {
        var error = Assert.Throws<DomainException>(() => Sale.Checkout(
            Org,
            SaleNumbers.Format(new DateOnly(2026, 7, 30), 1),
            SalePaymentMethod.Utang,
            [Draft(0m, 1m)],
            Actor,
            Now,
            customerId: POSCustomerId.New(),
            linkedCreditEntryId: CreditEntryId.New(),
            cashierShiftId: Shift, registerId: Register));
        Assert.Equal(DomainErrorCodes.SaleUtangTotalMustBePositive, error.ErrorCode);
    }

    [Fact]
    public void Utang_checkout_requires_customer_and_linked_credit_ids()
    {
        var missingCustomer = Assert.Throws<DomainException>(() => Sale.Checkout(
            Org,
            SaleNumbers.Format(new DateOnly(2026, 7, 30), 1),
            SalePaymentMethod.Utang,
            [Draft(50m, 1m)],
            Actor,
            Now,
            linkedCreditEntryId: CreditEntryId.New(),
            cashierShiftId: Shift, registerId: Register));
        Assert.Equal(DomainErrorCodes.SaleUtangCustomerRequired, missingCustomer.ErrorCode);

        var missingCredit = Assert.Throws<DomainException>(() => Sale.Checkout(
            Org,
            SaleNumbers.Format(new DateOnly(2026, 7, 30), 1),
            SalePaymentMethod.Utang,
            [Draft(50m, 1m)],
            Actor,
            Now,
            customerId: POSCustomerId.New(),
            cashierShiftId: Shift, registerId: Register));
        Assert.Equal(DomainErrorCodes.SaleUtangLinkageInvalid, missingCredit.ErrorCode);
    }

    [Fact]
    public void Utang_checkout_rejects_a_tendered_amount()
    {
        var error = Assert.Throws<DomainException>(() => Sale.Checkout(
            Org,
            SaleNumbers.Format(new DateOnly(2026, 7, 30), 1),
            SalePaymentMethod.Utang,
            [Draft(50m, 1m)],
            Actor,
            Now,
            amountTendered: 50m,
            customerId: POSCustomerId.New(),
            linkedCreditEntryId: CreditEntryId.New(),
            cashierShiftId: Shift, registerId: Register));
        Assert.Equal(DomainErrorCodes.InvalidSaleAmountTendered, error.ErrorCode);
    }

    [Fact]
    public void Sale_exposes_tax_amount_but_no_inventory_or_discount_state()
    {
        var names = typeof(Sale).GetProperties().Select(p => p.Name)
            .Concat(typeof(SaleLine).GetProperties().Select(p => p.Name))
            .ToList();

        Assert.Contains("CustomerId", names);
        Assert.Contains("LinkedCreditEntryId", names);
        Assert.Contains("TaxAmount", names);

        foreach (var forbidden in new[]
                 {
                     "Stock", "QuantityOnHand", "Inventory", "Vat", "Discount",
                     "Refund", "Tip", "Fee"
                 })
        {
            Assert.DoesNotContain(names, n => n.Contains(forbidden, StringComparison.OrdinalIgnoreCase));
        }
    }
}
