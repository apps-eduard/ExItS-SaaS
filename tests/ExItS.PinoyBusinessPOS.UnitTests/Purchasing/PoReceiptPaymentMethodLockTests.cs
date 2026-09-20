using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.UnitTests.Purchasing;

public sealed class PoReceiptPaymentMethodLockTests
{
    private static ReceivePurchaseOrderRequest Req(
        string? method = null,
        decimal? paidNow = null,
        string? gCash = null,
        string? bank = null,
        string? transfer = null,
        DateOnly? settlementDate = null,
        string? checkNumber = null,
        DateOnly? checkDate = null) =>
        new(
            [new ReceivePurchaseOrderLineRequest(Guid.NewGuid(), 1m)],
            PaymentMethodAtReceipt: method,
            PaidNow: paidNow,
            GCashReference: gCash,
            BankName: bank,
            TransferOrDepositReference: transfer,
            SettlementDate: settlementDate,
            CheckNumber: checkNumber,
            CheckDate: checkDate);

    [Fact]
    public void Validate_accepts_matching_cash_method_and_defaults_paid_now()
    {
        var result = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.Cash,
            350m,
            Req("Cash"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Cash", result.Value!.PaymentMethodAtReceipt);
        Assert.Equal(350m, result.Value.PaidNow);
    }

    [Fact]
    public void Validate_null_timing_uses_method_based_rules_not_pay_before_default()
    {
        // Non-connected POs default PaymentTiming to PayBefore in persistence, but receipt
        // validation must pass null timing so Cash still settles at receipt.
        var result = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.Cash,
            210m,
            Req("Cash", paidNow: 210m),
            paymentTiming: null);

        Assert.True(result.IsSuccess);
        Assert.Equal(210m, result.Value!.PaidNow);
    }

    [Fact]
    public void Validate_rejects_payment_method_mismatch()
    {
        var result = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.Cash,
            100m,
            Req("BankTransfer"));

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PurchaseReceiptPaymentMethodMismatch, result.ErrorCode);
    }

    [Fact]
    public void Validate_utang_requires_zero_paid_now()
    {
        var ok = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.Utang,
            643m,
            Req(paidNow: 0m));

        Assert.True(ok.IsSuccess);
        Assert.Null(ok.Value!.PaymentMethodAtReceipt);
        Assert.Equal(0m, ok.Value.PaidNow);

        var bad = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.Utang,
            643m,
            Req(paidNow: 100m));

        Assert.False(bad.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PurchaseReceiptPaymentInvalid, bad.ErrorCode);
    }

    [Fact]
    public void Validate_check_forces_paid_now_zero_and_pending_clearing()
    {
        var result = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.Check,
            500m,
            Req(
                "Check",
                paidNow: 500m,
                bank: "BDO",
                checkNumber: "1001",
                checkDate: new DateOnly(2026, 9, 17)),
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value!.PaidNow);
        Assert.Equal(UtangCheckClearingStatus.PendingClearing, result.Value.Settlement.CheckClearingStatus);
    }

    [Fact]
    public void Validate_pending_check_is_not_treated_as_settled()
    {
        var result = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.Check,
            500m,
            Req(
                "Check",
                bank: "BDO",
                checkNumber: "1001",
                checkDate: new DateOnly(2026, 9, 17)),
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt,
            amountPaidSnapshot: 0m,
            financialSettlementStatus: ConnectedPoFinancialSettlementStatus.AwaitingPayment);

        Assert.True(result.IsSuccess);
        Assert.Equal(UtangCheckClearingStatus.PendingClearing, result.Value!.Settlement.CheckClearingStatus);
        Assert.Equal(0m, result.Value.PaidNow);
    }

    [Fact]
    public void Validate_gcash_requires_reference()
    {
        var bad = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.ManualGCash,
            200m,
            Req("GCash", paidNow: 200m),
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt);

        Assert.False(bad.IsSuccess);

        var ok = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.ManualGCash,
            200m,
            Req("ManualGCash", paidNow: 200m, gCash: "GCASH-123"),
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt);

        Assert.True(ok.IsSuccess);
        Assert.Equal("GCASH-123", ok.Value!.Settlement.GCashReference);
    }

    [Fact]
    public void Validate_pay_before_settled_gcash_skips_reference_and_zeros_paid_now()
    {
        var result = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.ManualGCash,
            850m,
            Req(method: null, paidNow: 850m),
            ConnectedPoPaymentTiming.PayBeforeFulfillment,
            amountPaidSnapshot: 850m,
            confirmedTotalAmount: 850m);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value!.PaidNow);
        Assert.Null(result.Value.Settlement.GCashReference);
        Assert.Equal("GCash", result.Value.PaymentMethodAtReceipt);
    }

    [Fact]
    public void Validate_pay_before_settled_bank_skips_bank_fields()
    {
        var result = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.BankTransfer,
            500m,
            Req(),
            ConnectedPoPaymentTiming.PayBeforeFulfillment,
            amountPaidSnapshot: 500m,
            confirmedTotalAmount: 500m);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value!.PaidNow);
        Assert.Null(result.Value.Settlement.TransferOrDepositReference);
    }

    [Fact]
    public void Validate_pay_before_settled_check_skips_check_fields()
    {
        var result = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.Check,
            250m,
            Req(),
            ConnectedPoPaymentTiming.PayBeforeFulfillment,
            amountPaidSnapshot: 250m,
            confirmedTotalAmount: 250m);

        Assert.True(result.IsSuccess);
        Assert.Equal(0m, result.Value!.PaidNow);
        Assert.Null(result.Value.Settlement.CheckNumber);
        Assert.Null(result.Value.Settlement.CheckClearingStatus);
    }

    [Fact]
    public void Validate_pay_before_missing_settlement_returns_integrity_error_not_gcash()
    {
        var result = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.ManualGCash,
            850m,
            Req("GCash", paidNow: 850m),
            ConnectedPoPaymentTiming.PayBeforeFulfillment,
            amountPaidSnapshot: 0m,
            confirmedTotalAmount: 850m);

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.PurchaseReceiptPrepaymentMissing, result.ErrorCode);
        Assert.Equal(PoReceiptPaymentMethodLock.PrepaymentMissingMessage, result.ErrorMessage);
    }

    [Fact]
    public void Validate_pay_on_delivery_gcash_still_requires_reference()
    {
        var bad = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.ManualGCash,
            200m,
            Req("GCash", paidNow: 200m),
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt,
            amountPaidSnapshot: 0m);

        Assert.False(bad.IsSuccess);
        Assert.Contains("GCash", bad.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_pay_on_delivery_cash_does_not_require_gcash()
    {
        var result = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.Cash,
            200m,
            Req("Cash"),
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Settlement.GCashReference);
        Assert.Equal(200m, result.Value.PaidNow);
    }

    [Fact]
    public void Validate_pay_on_delivery_already_settled_skips_gcash()
    {
        var result = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.ManualGCash,
            200m,
            Req("GCash"),
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt,
            amountPaidSnapshot: 200m,
            financialSettlementStatus: ConnectedPoFinancialSettlementStatus.Settled);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Settlement.GCashReference);
        Assert.Equal(0m, result.Value.PaidNow);
    }

    [Fact]
    public void Validate_pay_on_delivery_bank_transfer_requires_fields()
    {
        var bad = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.BankTransfer,
            300m,
            Req("BankTransfer", paidNow: 300m),
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt);

        Assert.False(bad.IsSuccess);

        var ok = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.BankTransfer,
            300m,
            Req(
                "BankTransfer",
                paidNow: 300m,
                bank: "BDO",
                transfer: "TRF-1",
                settlementDate: new DateOnly(2026, 9, 19)),
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt);

        Assert.True(ok.IsSuccess);
        Assert.Equal("TRF-1", ok.Value!.Settlement.TransferOrDepositReference);
    }

    [Fact]
    public void Validate_pay_on_delivery_bank_deposit_requires_fields()
    {
        var ok = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.BankDeposit,
            300m,
            Req(
                "BankDeposit",
                paidNow: 300m,
                bank: "BPI",
                transfer: "DEP-9",
                settlementDate: new DateOnly(2026, 9, 19)),
            ConnectedPoPaymentTiming.PayOnDeliveryOrReceipt);

        Assert.True(ok.IsSuccess);
        Assert.Equal("DEP-9", ok.Value!.Settlement.TransferOrDepositReference);
    }

    [Fact]
    public void Validate_supplier_credit_timing_requires_zero_paid_now()
    {
        var ok = PoReceiptPaymentMethodLock.Validate(
            ConnectedPoPaymentTerm.Utang,
            300m,
            Req(paidNow: 0m),
            ConnectedPoPaymentTiming.SupplierCredit,
            amountPaidSnapshot: 0m);

        Assert.True(ok.IsSuccess);
        Assert.Equal(0m, ok.Value!.PaidNow);
    }

    [Fact]
    public void HasAuthoritativePrepayment_matches_snapshot_and_settled_status()
    {
        Assert.True(PoReceiptPaymentMethodLock.HasAuthoritativePrepayment(
            850m,
            ConnectedPoFinancialSettlementStatus.NotRequired,
            850m));
        Assert.True(PoReceiptPaymentMethodLock.HasAuthoritativePrepayment(
            0m,
            ConnectedPoFinancialSettlementStatus.Settled,
            850m));
        Assert.False(PoReceiptPaymentMethodLock.HasAuthoritativePrepayment(
            0m,
            ConnectedPoFinancialSettlementStatus.NotRequired,
            850m));
    }
}
