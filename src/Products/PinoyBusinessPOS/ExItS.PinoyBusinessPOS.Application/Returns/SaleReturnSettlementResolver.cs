using ExItS.PinoyBusinessPOS.Domain.Returns;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Returns;

public static class SaleReturnSettlementResolver
{
    public static decimal ResolveSettledPayments(Sale sale) =>
        sale.PaymentMethod switch
        {
            SalePaymentMethod.Utang => 0m,
            SalePaymentMethod.Check => ResolveCheckSettled(sale),
            SalePaymentMethod.Card or SalePaymentMethod.GCash => sale.Status == SaleStatus.Completed ? sale.Total : 0m,
            SalePaymentMethod.Cash or SalePaymentMethod.ManualGCash or SalePaymentMethod.BankTransfer or SalePaymentMethod.ManualMaya
                => sale.Status == SaleStatus.Completed ? sale.Total : 0m,
            _ => 0m
        };

    private static decimal ResolveCheckSettled(Sale sale)
    {
        if (sale.Status == SaleStatus.AwaitingPayment)
        {
            return 0m;
        }

        return sale.CheckSettlementStatus switch
        {
            Domain.Sales.CheckSettlementStatus.Cleared => sale.Total,
            Domain.Sales.CheckSettlementStatus.Pending => 0m,
            Domain.Sales.CheckSettlementStatus.Bounced => 0m,
            _ => 0m
        };
    }
}
