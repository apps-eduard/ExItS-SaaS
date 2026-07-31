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

    private static Sale CompletedCashSale(decimal quantity, decimal unitPrice)
    {
        var saleId = SaleId.New();
        var line = SaleLine.Create(
            saleId,
            Org,
            1,
            new SaleLineDraft(
                CatalogProductId.New(),
                "Widget",
                "W-1",
                null,
                UnitOfMeasure.Piece,
                unitPrice,
                quantity));

        return Sale.Rehydrate(
            saleId,
            Org,
            "SALE-20260731-000001",
            SaleStatus.Completed,
            SalePaymentMethod.Cash,
            line.LineTotal,
            line.LineTotal,
            line.LineTotal,
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
