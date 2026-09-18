using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Application.SupplierPayables;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

/// <summary>
/// Authoritative PayBefore settlement check for connected PO fulfillment transitions.
/// Returns a business failure (never throws) when settlement is insufficient.
/// </summary>
public static class ConnectedPoPayBeforeFulfillmentGate
{
    public sealed record SettlementInputs(
        ConnectedPurchaseOrder Order,
        PurchaseOrder? BuyerPo,
        IReadOnlyList<GoodsReceipt> Receipts,
        IReadOnlyList<SupplierPayable> PayablesForReceipts);

    public sealed record SettlementEvaluation(
        decimal RequiredAmount,
        decimal SettledAmount,
        bool IsSatisfied,
        string? FailureMessage);

    public static SettlementEvaluation Evaluate(SettlementInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(inputs.Order);

        if (inputs.Order.EffectivePaymentTiming != ConnectedPoPaymentTiming.PayBeforeFulfillment)
        {
            return new SettlementEvaluation(0m, 0m, IsSatisfied: true, FailureMessage: null);
        }

        var required = SaleMoney.RoundMoney(
            inputs.Order.ConfirmedTotalAmount > 0m
                ? inputs.Order.ConfirmedTotalAmount
                : inputs.Order.TotalAmount);
        var settled = ComputeSettledAmount(inputs);
        if (settled + 0.0000001m >= required)
        {
            return new SettlementEvaluation(required, settled, IsSatisfied: true, FailureMessage: null);
        }

        var message = inputs.Order.EffectivePaymentTerm == ConnectedPoPaymentTerm.Check
            ? "Fulfillment is blocked until full prepayment is settled and check payment is Cleared."
            : "Fulfillment is blocked until full prepayment is settled.";
        return new SettlementEvaluation(required, settled, IsSatisfied: false, FailureMessage: message);
    }

    public static ApplicationResult Fail(SettlementEvaluation evaluation) =>
        ApplicationResult.Failure(
            ConnectedSupplierDomainErrorCodes.PaymentRequiredBeforeFulfillment,
            evaluation.FailureMessage ?? "Fulfillment is blocked until full prepayment is settled.");

    public static ApplicationResult<T> Fail<T>(SettlementEvaluation evaluation) =>
        ApplicationResult<T>.Failure(
            ConnectedSupplierDomainErrorCodes.PaymentRequiredBeforeFulfillment,
            evaluation.FailureMessage ?? "Fulfillment is blocked until full prepayment is settled.");

    public static async Task<SettlementEvaluation> EvaluateAsync(
        ConnectedPurchaseOrder order,
        PurchaseOrder? buyerPo,
        IPurchaseOrderRepository? buyerOrders,
        ISupplierPayableRepository? payables,
        CancellationToken cancellationToken = default)
    {
        if (order.EffectivePaymentTiming != ConnectedPoPaymentTiming.PayBeforeFulfillment)
        {
            return new SettlementEvaluation(0m, 0m, IsSatisfied: true, FailureMessage: null);
        }

        IReadOnlyList<GoodsReceipt> receipts = Array.Empty<GoodsReceipt>();
        var payableRows = new List<SupplierPayable>();
        if (buyerPo is not null && buyerOrders is not null)
        {
            receipts = await buyerOrders
                .ListGoodsReceiptsForPurchaseOrderAsync(
                    order.BuyerOrganizationId,
                    order.BuyerPurchaseOrderId,
                    cancellationToken)
                .ConfigureAwait(false);

            if (payables is not null)
            {
                foreach (var receipt in receipts.Where(r => r.Status == GoodsReceiptStatus.Posted))
                {
                    var payable = await payables
                        .FindBySourceAsync(
                            order.BuyerOrganizationId,
                            SupplierPayableSourceType.GoodsReceipt,
                            receipt.Id.Value,
                            cancellationToken)
                        .ConfigureAwait(false);
                    if (payable is not null)
                    {
                        payableRows.Add(payable);
                    }
                }
            }
        }

        return Evaluate(new SettlementInputs(order, buyerPo, receipts, payableRows));
    }

    private static decimal ComputeSettledAmount(SettlementInputs inputs)
    {
        var snapshot = SaleMoney.RoundMoney(inputs.BuyerPo?.AmountPaidSnapshot ?? 0m);
        var fromPayables = 0m;
        var receiptById = (inputs.Receipts ?? Array.Empty<GoodsReceipt>())
            .Where(r => r.Status != GoodsReceiptStatus.Voided)
            .ToDictionary(r => r.Id.Value);

        foreach (var payable in inputs.PayablesForReceipts ?? Array.Empty<SupplierPayable>())
        {
            if (payable.Status == SupplierPayableStatus.Voided)
            {
                continue;
            }

            if (payable.SourceType != SupplierPayableSourceType.GoodsReceipt)
            {
                continue;
            }

            if (!receiptById.TryGetValue(payable.SourceId, out var receipt))
            {
                continue;
            }

            var isCheck = payable.PaymentMethodAtReceipt == SupplierPayablePaymentMethod.Check
                || inputs.Order.EffectivePaymentTerm == ConnectedPoPaymentTerm.Check
                || receipt.Settlement.CheckClearingStatus is not null;

            if (isCheck)
            {
                if (receipt.Settlement.CheckClearingStatus != UtangCheckClearingStatus.Cleared)
                {
                    continue;
                }
            }

            fromPayables += payable.PaidAmount;
        }

        fromPayables = SaleMoney.RoundMoney(fromPayables);

        // Check PayBefore: only Cleared settlement counts (snapshot alone is insufficient unless
        // it was recorded after an explicit Cleared confirm — still require Cleared evidence when
        // any check-related receipt exists; otherwise allow confirmed snapshot prepayment).
        if (inputs.Order.EffectivePaymentTerm == ConnectedPoPaymentTerm.Check)
        {
            var anyCheckReceipt = receiptById.Values.Any(r => r.Settlement.CheckClearingStatus is not null);
            if (anyCheckReceipt)
            {
                return fromPayables;
            }

            // No receipt yet: seller may confirm Cleared prepayment onto AmountPaidSnapshot.
            return snapshot;
        }

        return SaleMoney.RoundMoney(Math.Max(snapshot, fromPayables));
    }
}
