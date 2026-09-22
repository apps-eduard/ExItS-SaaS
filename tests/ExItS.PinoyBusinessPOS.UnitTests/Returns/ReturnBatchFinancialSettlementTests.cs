using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Returns;

public sealed class ReturnBatchFinancialSettlementTests
{
    [Fact]
    public void Unpaid_sale_reduces_obligation_without_refund()
    {
        var settlement = ReturnBatchFinancialSettlement.Evaluate(
            originalSaleTotal: 1000m,
            settledPayments: 0m,
            acceptedReturnValue: 300m,
            priorAcceptedReturnValues: 0m,
            alreadyRefundedAcrossBatches: 0m);

        Assert.Equal(700m, settlement.RemainingDue);
        Assert.Equal(0m, settlement.RefundDue);
        Assert.Equal(300m, settlement.ObligationReducedAmount);
    }

    [Fact]
    public void Fully_paid_sale_has_refund_due()
    {
        var settlement = ReturnBatchFinancialSettlement.Evaluate(1000m, 1000m, 300m, 0m, 0m);
        Assert.Equal(300m, settlement.RefundDue);
        Assert.Equal(0m, settlement.RemainingDue);
    }

    [Fact]
    public void Partially_paid_sale_refunds_only_excess_over_obligation()
    {
        var settlement = ReturnBatchFinancialSettlement.Evaluate(1000m, 800m, 300m, 0m, 0m);
        Assert.Equal(100m, settlement.RefundDue);
        Assert.Equal(0m, settlement.RemainingDue);
    }

    [Fact]
    public void Settlement_resolver_treats_awaiting_payment_as_unsettled()
    {
        var sale = BuildSale(SalePaymentMethod.Card, SaleStatus.AwaitingPayment, 1000m);
        Assert.Equal(0m, SaleReturnSettlementResolver.ResolveSettledPayments(sale));
    }

    [Fact]
    public void Settlement_resolver_treats_pending_check_as_unsettled()
    {
        var sale = BuildSale(SalePaymentMethod.Check, SaleStatus.Completed, 1000m, CheckSettlementStatus.Pending);
        Assert.Equal(0m, SaleReturnSettlementResolver.ResolveSettledPayments(sale));
    }

    [Fact]
    public void Settlement_resolver_treats_cleared_check_as_settled()
    {
        var sale = BuildSale(SalePaymentMethod.Check, SaleStatus.Completed, 1000m, CheckSettlementStatus.Cleared);
        Assert.Equal(1000m, SaleReturnSettlementResolver.ResolveSettledPayments(sale));
    }

    [Fact]
    public void Finalize_status_for_pending_check_is_obligation_reduced()
    {
        var settlement = ReturnBatchFinancialSettlement.Evaluate(1000m, 0m, 300m, 0m, 0m);
        Assert.Equal(300m, settlement.ObligationReducedAmount);
        Assert.Equal(0m, settlement.RefundDue);
    }

    [Fact]
    public void Utang_settlement_path_is_credit_reduced()
    {
        var sale = BuildSale(SalePaymentMethod.Utang, SaleStatus.Completed, 1000m);
        Assert.Equal(0m, SaleReturnSettlementResolver.ResolveSettledPayments(sale));
    }

    private static Sale BuildSale(
        SalePaymentMethod method,
        SaleStatus status,
        decimal total,
        CheckSettlementStatus? checkStatus = null)
    {
        var org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        var actor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var now = new DateTimeOffset(2026, 9, 18, 18, 0, 0, TimeSpan.Zero);
        var saleId = SaleId.New();
        var line = SaleLine.Rehydrate(
            SaleLineId.New(),
            saleId,
            org,
            CatalogProductId.New(),
            1,
            "Item",
            "SKU",
            null,
            UnitOfMeasure.Piece,
            unitPrice: total,
            quantity: 1m,
            lineTotal: total,
            grossLineTotal: total,
            lineDiscountAmount: 0m);
        return Sale.Rehydrate(
            saleId,
            org,
            "SALE-SETTLEMENT-1",
            status,
            method,
            total,
            total,
            taxAmount: 0m,
            amountTendered: method == SalePaymentMethod.Cash ? total : null,
            changeAmount: method == SalePaymentMethod.Cash ? 0m : null,
            gcashReference: null,
            recordedAtUtc: now,
            recordedBy: actor,
            voidedAtUtc: null,
            voidedBy: null,
            voidReason: null,
            updatedAtUtc: now,
            lines: [line],
            checkSettlementStatus: checkStatus);
    }
}
