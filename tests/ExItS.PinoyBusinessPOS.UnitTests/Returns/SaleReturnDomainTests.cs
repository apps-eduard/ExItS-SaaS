using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Returns;

public sealed class SaleReturnDomainTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly Guid Actor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset Now = new(2026, 7, 31, 5, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Refundable_quantity_and_amount_follow_remaining_line_totals()
    {
        var sale = CompletedCashSale(quantity: 3m, unitPrice: 10m);
        var line = sale.Lines.Single();

        Assert.Equal(3m, SaleReturnRefundable.RefundableQuantity(line, 0m));
        Assert.Equal(30m, SaleReturnRefundable.RefundableAmount(line, 0m));
        Assert.Equal(10m, SaleReturnRefundable.ComputeRefundAmount(line, 1m, 0m, 0m));
        Assert.Equal(20m, SaleReturnRefundable.ComputeRefundAmount(line, 2m, 1m, 10m));
    }

    [Fact]
    public void Net_refund_follows_cumulative_line_total_not_unit_price()
    {
        // qty 10, net LineTotal 80 (e.g. discounted) — never 10×UnitPrice
        var sale = CompletedCashSale(quantity: 10m, unitPrice: 10m, lineTotalOverride: 80m);
        var line = sale.Lines.Single();

        var first = SaleReturnRefundable.ComputeRefundAmount(line, 3m, 0m, 0m);
        Assert.Equal(24m, first); // Round(80 * 3/10)

        var second = SaleReturnRefundable.ComputeRefundAmount(line, 3m, 3m, 24m);
        Assert.Equal(24m, second); // Round(80 * 6/10) - 24

        var final = SaleReturnRefundable.ComputeRefundAmount(line, 4m, 6m, 48m);
        Assert.Equal(32m, final); // 80 - 48
        Assert.Equal(80m, first + second + final);
    }

    [Fact]
    public void Net_refund_centavo_slices_absorb_remainder_on_final()
    {
        // qty 3, net 10.00 — proportional rounds, final slice equals LineTotal
        var sale = CompletedCashSale(quantity: 3m, unitPrice: 4m, lineTotalOverride: 10m);
        var line = sale.Lines.Single();

        var a = SaleReturnRefundable.ComputeRefundAmount(line, 1m, 0m, 0m);
        Assert.Equal(3.33m, a); // Round(10 * 1/3)

        var b = SaleReturnRefundable.ComputeRefundAmount(line, 1m, 1m, 3.33m);
        Assert.Equal(3.34m, b); // Round(10 * 2/3) - 3.33 = 6.67 - 3.33

        var c = SaleReturnRefundable.ComputeRefundAmount(line, 1m, 2m, 6.67m);
        Assert.Equal(3.33m, c); // 10.00 - 6.67
        Assert.Equal(10.00m, a + b + c);
    }

    [Fact]
    public void Over_return_quantity_is_rejected()
    {
        var sale = CompletedCashSale(quantity: 2m, unitPrice: 5m);
        var line = sale.Lines.Single();

        var act = () => SaleReturnLine.Create(
            SaleReturnId.New(),
            Org,
            line,
            new SaleReturnLineDraft(line.Id, 3m, RestockDisposition.DoNotRestock),
            previouslyReturnedQuantity: 0m,
            previouslyRefundedAmount: 0m);

        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Void_and_return_are_mutually_exclusive_at_domain_level()
    {
        var sale = CompletedCashSale(quantity: 1m, unitPrice: 10m);
        sale.Void("mistake", Actor, Now);

        var act = () => SaleReturn.CreateCompleted(
            Org,
            ReturnNumbers.Format(DateOnly.FromDateTime(Now.UtcDateTime), 1),
            sale,
            [new SaleReturnLineDraft(sale.Lines.Single().Id, 1m, RestockDisposition.DoNotRestock)],
            new Dictionary<Guid, (decimal, decimal)>(),
            "customer changed mind",
            Actor,
            Now,
            CashierShiftId.New());

        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Cash_return_requires_shift()
    {
        var sale = CompletedCashSale(quantity: 1m, unitPrice: 10m);

        var act = () => SaleReturn.CreateCompleted(
            Org,
            ReturnNumbers.Format(DateOnly.FromDateTime(Now.UtcDateTime), 1),
            sale,
            [new SaleReturnLineDraft(sale.Lines.Single().Id, 1m, RestockDisposition.DoNotRestock)],
            new Dictionary<Guid, (decimal, decimal)>(),
            "wrong item",
            Actor,
            Now);

        Assert.Throws<DomainException>(act);
    }

    private static Sale CompletedCashSale(decimal quantity, decimal unitPrice, decimal? lineTotalOverride = null)
    {
        var saleId = SaleId.New();
        var gross = SaleMoney.RoundMoney(unitPrice * quantity);
        var net = lineTotalOverride ?? gross;
        var line = SaleLine.Rehydrate(
            SaleLineId.New(),
            saleId,
            Org,
            CatalogProductId.New(),
            1,
            "Widget",
            "W-1",
            null,
            UnitOfMeasure.Piece,
            unitPrice,
            quantity,
            net,
            grossLineTotal: gross,
            lineDiscountAmount: SaleMoney.RoundMoney(gross - net));

        return Sale.Rehydrate(
            saleId,
            Org,
            "SALE-20260731-000001",
            SaleStatus.Completed,
            SalePaymentMethod.Cash,
            net,
            net,
            0m,
            net,
            0m,
            null,
            Now,
            Actor,
            null,
            null,
            null,
            Now,
            [line],
            cashierShiftId: CashierShiftId.New());
    }
}
