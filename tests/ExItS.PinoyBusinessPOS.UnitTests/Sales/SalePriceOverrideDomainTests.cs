using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Sales;

/// <summary>
/// RMAP-B01 sale price override. Overrides change UnitPrice only; catalog SellingPrice is never
/// rewritten. Manager deviation is inclusive ≤100%; Owner unlimited; free (≤0) is denied.
/// </summary>
public sealed class SalePriceOverrideDomainTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.NewGuid());
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 14, 0, 0, TimeSpan.Zero);
    private static readonly CashierShiftId Shift = CashierShiftId.New();
    private static readonly RegisterId Register = RegisterId.New();

    private const string Reason = "VIP negotiation";

    private static SaleLineDraft Draft(
        decimal unitPrice,
        decimal quantity,
        CatalogProductId? productId = null,
        UnitOfMeasure unit = UnitOfMeasure.Piece,
        SellingMode sellingMode = SellingMode.PerItem,
        string name = "Sardinas",
        ProductUnitId? sellingUnitId = null,
        decimal? enteredQuantity = null,
        decimal? multiplier = null) =>
        new(
            productId ?? CatalogProductId.New(),
            name,
            "SKU-1",
            "4801234567890",
            unit,
            unitPrice,
            quantity,
            sellingMode,
            sellingUnitId,
            sellingUnitId is null ? null : "Sack",
            enteredQuantity,
            multiplier);

    private static SalePriceOverrideIntent Override(
        decimal requested,
        int? lineNumber = 1,
        CatalogProductId? productId = null,
        decimal? expectedBaseline = null,
        string? reason = Reason) =>
        new(requested, reason!, productId, lineNumber, expectedBaseline);

    private static Sale Checkout(
        IReadOnlyList<SaleLineDraft> lines,
        IReadOnlyList<SalePriceOverrideIntent>? overrides = null,
        IReadOnlyList<CommercialDiscountIntent>? discounts = null,
        bool allowUnlimited = false,
        SalePaymentMethod method = SalePaymentMethod.Cash,
        decimal? tendered = 100_000m) =>
        Sale.Checkout(
            Org,
            SaleNumbers.Format(new DateOnly(2026, 8, 21), 1),
            method,
            lines,
            Actor,
            Now,
            tendered,
            cashierShiftId: Shift,
            registerId: Register,
            commercialDiscounts: discounts,
            priceOverrides: overrides,
            allowUnlimitedSalePriceOverride: allowUnlimited);

    [Fact]
    public void No_override_leaves_unit_price_and_totals_unchanged()
    {
        var sale = Checkout([Draft(100m, 2m)]);

        Assert.Equal(100m, sale.Lines[0].UnitPrice);
        Assert.Equal(200m, sale.GrossSubtotal);
        Assert.Equal(200m, sale.Subtotal);
        Assert.Empty(sale.PriceOverrides);
    }

    [Theory]
    [InlineData(90)]
    [InlineData(150)]
    [InlineData(200)]
    public void Manager_deviation_at_or_below_100_percent_passes(decimal requested)
    {
        var sale = Checkout([Draft(100m, 1m)], [Override(requested)], allowUnlimited: false);

        Assert.Equal(requested, sale.Lines[0].UnitPrice);
        Assert.Equal(requested, sale.Subtotal);
        var audit = Assert.Single(sale.PriceOverrides);
        Assert.Equal(100m, audit.BaselineUnitPrice);
        Assert.Equal(requested, audit.AppliedUnitPrice);
        Assert.Equal(Reason, audit.Reason);
    }

    [Fact]
    public void Manager_deviation_above_100_percent_is_denied()
    {
        var error = Assert.Throws<DomainException>(() =>
            Checkout([Draft(100m, 1m)], [Override(200.01m)], allowUnlimited: false));

        Assert.Equal(DomainErrorCodes.SalePriceOverrideExceedsManagerLimit, error.ErrorCode);
    }

    [Fact]
    public void Owner_unlimited_allows_250_against_100_baseline()
    {
        var sale = Checkout([Draft(100m, 1m)], [Override(250m)], allowUnlimited: true);

        Assert.Equal(250m, sale.Lines[0].UnitPrice);
        Assert.Equal(250m, sale.Subtotal);
        Assert.Single(sale.PriceOverrides);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Requested_zero_or_negative_is_denied_for_manager_and_owner(decimal requested)
    {
        var manager = Assert.Throws<DomainException>(() =>
            Checkout([Draft(100m, 1m)], [Override(requested)], allowUnlimited: false));
        Assert.Equal(DomainErrorCodes.SalePriceOverrideInvalidAmount, manager.ErrorCode);

        var owner = Assert.Throws<DomainException>(() =>
            Checkout([Draft(100m, 1m)], [Override(requested)], allowUnlimited: true));
        Assert.Equal(DomainErrorCodes.SalePriceOverrideInvalidAmount, owner.ErrorCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_reason_is_denied(string? reason)
    {
        var error = Assert.Throws<DomainException>(() =>
            Checkout([Draft(100m, 1m)], [Override(150m, reason: reason!)], allowUnlimited: false));

        Assert.Equal(DomainErrorCodes.SalePriceOverrideReasonRequired, error.ErrorCode);
    }

    [Fact]
    public void Stale_expected_baseline_conflicts_without_clamping()
    {
        var error = Assert.Throws<DomainException>(() =>
            Checkout(
                [Draft(100m, 1m)],
                [Override(150m, expectedBaseline: 80m)],
                allowUnlimited: false));

        Assert.Equal(DomainErrorCodes.SalePriceOverrideStaleBaseline, error.ErrorCode);
    }

    [Fact]
    public void Override_then_commercial_percent_discount_uses_overridden_gross()
    {
        var sale = Checkout(
            [Draft(100m, 2m)],
            [Override(80m)],
            [new CommercialDiscountIntent(
                SaleDiscountScope.Sale,
                SaleDiscountMethod.Percentage,
                10m,
                Reason)],
            allowUnlimited: false);

        Assert.Equal(80m, sale.Lines[0].UnitPrice);
        Assert.Equal(160m, sale.GrossSubtotal);
        Assert.Equal(16m, sale.DiscountTotal);
        Assert.Equal(144m, sale.Subtotal);
        Assert.Single(sale.PriceOverrides);
        Assert.Single(sale.CommercialDiscounts);
    }

    [Fact]
    public void Override_then_fixed_line_discount_uses_overridden_gross()
    {
        var sale = Checkout(
            [Draft(100m, 1m)],
            [Override(200m)],
            [new CommercialDiscountIntent(
                SaleDiscountScope.Line,
                SaleDiscountMethod.FixedAmount,
                50m,
                Reason,
                LineNumber: 1)],
            allowUnlimited: false);

        Assert.Equal(200m, sale.Lines[0].UnitPrice);
        Assert.Equal(200m, sale.GrossSubtotal);
        Assert.Equal(50m, sale.LineDiscountTotal);
        Assert.Equal(150m, sale.Lines[0].LineTotal);
    }

    [Fact]
    public void ByWeight_override_changes_unit_price_only_quantity_unchanged()
    {
        var sale = Checkout(
            [Draft(55m, 1.250m, unit: UnitOfMeasure.Kilogram, sellingMode: SellingMode.ByWeight)],
            [Override(60m)],
            allowUnlimited: false);

        Assert.Equal(60m, sale.Lines[0].UnitPrice);
        Assert.Equal(1.250m, sale.Lines[0].Quantity);
        Assert.Equal(75.00m, sale.Lines[0].GrossLineTotal);
    }

    [Fact]
    public void Multi_uom_entered_quantity_gross_uses_overridden_unit_price()
    {
        var sale = Checkout(
            [Draft(
                2600m,
                quantity: 50m,
                sellingUnitId: ProductUnitId.New(),
                enteredQuantity: 1m,
                multiplier: 50m)],
            [Override(2400m)],
            allowUnlimited: false);

        Assert.Equal(2400m, sale.Lines[0].UnitPrice);
        Assert.Equal(2400m, sale.Lines[0].GrossLineTotal);
        Assert.Equal(50m, sale.Lines[0].Quantity);
    }

    [Fact]
    public void Quote_matches_checkout_money_for_override_and_discount()
    {
        var lines = new[] { Draft(100m, 1m) };
        var overrides = new[] { Override(150m) };
        var discounts = new[]
        {
            new CommercialDiscountIntent(
                SaleDiscountScope.Sale,
                SaleDiscountMethod.Percentage,
                10m,
                Reason)
        };

        var quote = Sale.QuoteCheckoutMoney(Org, lines, discounts, overrides, allowUnlimitedSalePriceOverride: false);
        var sale = Checkout(lines, overrides, discounts, allowUnlimited: false);

        Assert.Equal(quote.Discounts.GrossSubtotal, sale.GrossSubtotal);
        Assert.Equal(quote.Discounts.NetSubtotal, sale.Subtotal);
        Assert.Equal(quote.PricedDrafts[0].UnitPrice, sale.Lines[0].UnitPrice);
        Assert.Equal(quote.PriceOverrides.Adjustments[0].BaselineUnitPrice, sale.PriceOverrides[0].BaselineUnitPrice);
    }

    [Fact]
    public void Money_validation_rejects_more_than_two_decimal_places()
    {
        var error = Assert.Throws<DomainException>(() =>
            Checkout([Draft(100m, 1m)], [Override(150.123m)], allowUnlimited: true));

        Assert.Equal(DomainErrorCodes.SalePriceOverrideInvalidAmount, error.ErrorCode);
    }

    [Fact]
    public void ExceedsManagerLimit_helper_is_inclusive_at_exact_100_percent()
    {
        Assert.False(SalePriceOverrideRules.ExceedsManagerLimit(100m, 200m));
        Assert.True(SalePriceOverrideRules.ExceedsManagerLimit(100m, 200.01m));
        Assert.False(SalePriceOverrideRules.ExceedsManagerLimit(100m, 90m));
        Assert.True(SalePriceOverrideRules.ExceedsManagerLimit(0m, 1m));
    }
}
