using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Payments;

namespace ExItS.PinoyBusinessPOS.UnitTests.Purchasing;

public sealed class PoReceiptPaymentMethodLockTests
{
    [Fact]
    public void Validate_accepts_matching_cash_method_and_defaults_paid_now()
    {
        var result = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.Cash,
            350m,
            new ReceivePurchaseOrderRequest(
                [new ReceivePurchaseOrderLineRequest(Guid.NewGuid(), 5m)],
                PaymentMethodAtReceipt: "Cash"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Cash", result.Value!.PaymentMethodAtReceipt);
        Assert.Equal(350m, result.Value.PaidNow);
    }

    [Fact]
    public void Validate_rejects_payment_method_mismatch()
    {
        var result = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.Cash,
            100m,
            new ReceivePurchaseOrderRequest(
                [new ReceivePurchaseOrderLineRequest(Guid.NewGuid(), 1m)],
                PaymentMethodAtReceipt: "BankTransfer"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PurchaseReceiptPaymentMethodMismatch, result.ErrorCode);
    }

    [Fact]
    public void Validate_utang_requires_zero_paid_now()
    {
        var ok = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.Utang,
            643m,
            new ReceivePurchaseOrderRequest(
                [new ReceivePurchaseOrderLineRequest(Guid.NewGuid(), 1m)],
                PaidNow: 0m));

        Assert.True(ok.IsSuccess);
        Assert.Null(ok.Value!.PaymentMethodAtReceipt);
        Assert.Equal(0m, ok.Value.PaidNow);

        var bad = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.Utang,
            643m,
            new ReceivePurchaseOrderRequest(
                [new ReceivePurchaseOrderLineRequest(Guid.NewGuid(), 1m)],
                PaidNow: 100m));

        Assert.False(bad.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PurchaseReceiptPaymentInvalid, bad.ErrorCode);
    }

    [Fact]
    public void Validate_check_forces_paid_now_zero_and_pending_clearing()
    {
        var result = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.Check,
            500m,
            new ReceivePurchaseOrderRequest(
                [new ReceivePurchaseOrderLineRequest(Guid.NewGuid(), 1m)],
                PaymentMethodAtReceipt: "Check",
                PaidNow: 500m,
                BankName: "BDO",
                CheckNumber: "1001",
                CheckDate: new DateOnly(2026, 9, 17)));

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value!.PaidNow);
        Assert.Equal(UtangCheckClearingStatus.PendingClearing, result.Value.Settlement.CheckClearingStatus);
    }

    [Fact]
    public void Validate_gcash_requires_reference()
    {
        var bad = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.ManualGCash,
            200m,
            new ReceivePurchaseOrderRequest(
                [new ReceivePurchaseOrderLineRequest(Guid.NewGuid(), 1m)],
                PaymentMethodAtReceipt: "GCash",
                PaidNow: 200m));

        Assert.False(bad.IsSuccess);

        var ok = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.ManualGCash,
            200m,
            new ReceivePurchaseOrderRequest(
                [new ReceivePurchaseOrderLineRequest(Guid.NewGuid(), 1m)],
                PaymentMethodAtReceipt: "ManualGCash",
                PaidNow: 200m,
                GCashReference: "GCASH-123"));

        Assert.True(ok.IsSuccess);
        Assert.Equal("GCASH-123", ok.Value!.Settlement.GCashReference);
    }
}
