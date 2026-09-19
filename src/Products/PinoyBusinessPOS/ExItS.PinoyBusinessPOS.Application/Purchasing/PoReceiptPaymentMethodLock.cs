using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Domain.SupplierPayables;

namespace ExItS.PinoyBusinessPOS.Application.Purchasing;

public sealed record PoReceiptPaymentResolution(
    string? PaymentMethodAtReceipt,
    decimal? PaidNow,
    GoodsReceiptSettlement Settlement);

public static class PoReceiptPaymentMethodLock
{
    public static string? ToReceiptPaymentMethodCode(ConnectedPoPaymentTerm term) =>
        term switch
        {
            ConnectedPoPaymentTerm.Cash => SupplierPayablePaymentMethods.ToCode(SupplierPayablePaymentMethod.Cash),
            ConnectedPoPaymentTerm.ManualGCash => SupplierPayablePaymentMethods.ToCode(SupplierPayablePaymentMethod.GCash),
            ConnectedPoPaymentTerm.BankTransfer => SupplierPayablePaymentMethods.ToCode(
                SupplierPayablePaymentMethod.BankTransfer),
            ConnectedPoPaymentTerm.BankDeposit => SupplierPayablePaymentMethods.ToCode(
                SupplierPayablePaymentMethod.BankDeposit),
            ConnectedPoPaymentTerm.Check => SupplierPayablePaymentMethods.ToCode(SupplierPayablePaymentMethod.Check),
            ConnectedPoPaymentTerm.Utang => null,
            _ => null
        };

    public static ApplicationResult<PoReceiptPaymentResolution> Validate(
        ConnectedPoPaymentTerm effectiveTerm,
        decimal receivedAmount,
        ReceivePurchaseOrderRequest request,
        ConnectedPoPaymentTiming? paymentTiming = null,
        decimal? amountPaidSnapshot = null,
        ConnectedPoFinancialSettlementStatus? financialSettlementStatus = null)
    {
        var received = SaleMoney.RoundMoney(receivedAmount);
        var expectedMethod = ToReceiptPaymentMethodCode(effectiveTerm);

        // Pay-before is settled before fulfillment. Goods receipt must never re-require
        // GCash/bank/check settlement fields or double-post PaidNow.
        if (paymentTiming == ConnectedPoPaymentTiming.PayBeforeFulfillment)
        {
            if (!string.IsNullOrWhiteSpace(request.PaymentMethodAtReceipt)
                && !PaymentMethodMatches(effectiveTerm, request.PaymentMethodAtReceipt))
            {
                return ApplicationResult<PoReceiptPaymentResolution>.Failure(
                    ApplicationErrorCodes.PurchaseReceiptPaymentMethodMismatch,
                    "Payment method at receipt must match the purchase order payment term.");
            }

            return ApplicationResult<PoReceiptPaymentResolution>.Success(
                new PoReceiptPaymentResolution(
                    expectedMethod,
                    PaidNow: 0m,
                    GoodsReceiptSettlement.Empty));
        }

        if (!PaymentMethodMatches(effectiveTerm, request.PaymentMethodAtReceipt))
        {
            return ApplicationResult<PoReceiptPaymentResolution>.Failure(
                ApplicationErrorCodes.PurchaseReceiptPaymentMethodMismatch,
                "Payment method at receipt must match the purchase order payment term.");
        }

        decimal? resolvedPaidNow = request.PaidNow;
        if (effectiveTerm == ConnectedPoPaymentTerm.Utang
            || paymentTiming == ConnectedPoPaymentTiming.SupplierCredit)
        {
            var paid = request.PaidNow ?? 0m;
            if (paid != 0m)
            {
                return ApplicationResult<PoReceiptPaymentResolution>.Failure(
                    ApplicationErrorCodes.PurchaseReceiptPaymentInvalid,
                    "Utang purchase orders require PaidNow to be zero at receipt.");
            }

            resolvedPaidNow = 0m;
        }
        else if (effectiveTerm == ConnectedPoPaymentTerm.Check)
        {
            resolvedPaidNow = 0m;
        }
        else if (received > 0m && effectiveTerm == ConnectedPoPaymentTerm.Cash)
        {
            resolvedPaidNow = request.PaidNow ?? received;
        }

        if (resolvedPaidNow is decimal paidNowValue)
        {
            var normalized = SaleMoney.RoundMoney(paidNowValue);
            if (normalized < 0m || normalized > received)
            {
                return ApplicationResult<PoReceiptPaymentResolution>.Failure(
                    ApplicationErrorCodes.PurchaseReceiptPaymentInvalid,
                    "PaidNow must be between zero and the good-quantity receipt total.");
            }

            resolvedPaidNow = normalized;
        }

        try
        {
            var settlement = BuildSettlement(effectiveTerm, request, received);
            return ApplicationResult<PoReceiptPaymentResolution>.Success(
                new PoReceiptPaymentResolution(expectedMethod, resolvedPaidNow, settlement));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PoReceiptPaymentResolution>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    public static bool PaymentMethodMatches(ConnectedPoPaymentTerm term, string? submitted)
    {
        var expected = ToReceiptPaymentMethodCode(term);
        if (expected is null)
        {
            return string.IsNullOrWhiteSpace(submitted);
        }

        if (string.IsNullOrWhiteSpace(submitted))
        {
            return false;
        }

        if (term == ConnectedPoPaymentTerm.ManualGCash)
        {
            return submitted.Trim().Equals("GCash", StringComparison.OrdinalIgnoreCase)
                || submitted.Trim().Equals("ManualGCash", StringComparison.OrdinalIgnoreCase);
        }

        return submitted.Trim().Equals(expected, StringComparison.OrdinalIgnoreCase);
    }

    private static GoodsReceiptSettlement BuildSettlement(
        ConnectedPoPaymentTerm term,
        ReceivePurchaseOrderRequest request,
        decimal receivedAmount)
    {
        if (receivedAmount <= 0m && term != ConnectedPoPaymentTerm.Check)
        {
            return GoodsReceiptSettlement.Empty;
        }

        return term switch
        {
            ConnectedPoPaymentTerm.ManualGCash => BuildGCashSettlement(request),
            ConnectedPoPaymentTerm.BankTransfer => BuildBankSettlement(request),
            ConnectedPoPaymentTerm.BankDeposit => BuildBankSettlement(request),
            ConnectedPoPaymentTerm.Check => BuildCheckSettlement(request),
            _ => ClearStaleSettlementFields(term, request)
        };
    }

    private static GoodsReceiptSettlement BuildGCashSettlement(ReceivePurchaseOrderRequest request)
    {
        var reference = NormalizeRequired(
            request.GCashReference,
            GoodsReceiptSettlement.GCashReferenceMaxLength,
            ApplicationErrorCodes.PurchaseReceiptPaymentInvalid,
            "GCash reference is required.");
        return new GoodsReceiptSettlement(reference, null, null, null, null, null, NormalizeNotes(request.SettlementNotes), null);
    }

    private static GoodsReceiptSettlement BuildBankSettlement(ReceivePurchaseOrderRequest request)
    {
        var bankName = UtangCheckPayment.NormalizeRequiredBankName(request.BankName);
        var reference = NormalizeRequired(
            request.TransferOrDepositReference,
            UtangCheckPayment.ReferenceMaxLength,
            ApplicationErrorCodes.PurchaseReceiptPaymentInvalid,
            "Transfer or deposit reference is required.");

        if (request.SettlementDate is null)
        {
            throw new DomainException(
                ApplicationErrorCodes.PurchaseReceiptPaymentInvalid,
                "Settlement date is required.");
        }

        return new GoodsReceiptSettlement(
            null,
            bankName,
            reference,
            request.SettlementDate,
            null,
            null,
            NormalizeNotes(request.SettlementNotes),
            null);
    }

    private static GoodsReceiptSettlement BuildCheckSettlement(ReceivePurchaseOrderRequest request)
    {
        var checkNumber = UtangCheckPayment.NormalizeRequiredCheckNumber(request.CheckNumber);
        var bankName = UtangCheckPayment.NormalizeRequiredBankName(request.BankName);
        var checkDate = UtangCheckPayment.NormalizeRequiredCheckDate(request.CheckDate);
        var clearing = UtangCheckClearingStatus.PendingClearing;
        if (!string.IsNullOrWhiteSpace(request.CheckClearingStatus)
            && UtangCheckClearingStatuses.TryParse(request.CheckClearingStatus, out var parsed)
            && parsed != UtangCheckClearingStatus.PendingClearing)
        {
            throw new DomainException(
                ApplicationErrorCodes.PurchaseReceiptPaymentInvalid,
                "Check clearing status must be PendingClearing when receiving a check.");
        }

        return new GoodsReceiptSettlement(
            null,
            bankName,
            null,
            null,
            checkNumber,
            checkDate,
            NormalizeNotes(request.SettlementNotes),
            clearing);
    }

    private static GoodsReceiptSettlement ClearStaleSettlementFields(
        ConnectedPoPaymentTerm term,
        ReceivePurchaseOrderRequest request)
    {
        if (term is ConnectedPoPaymentTerm.Cash or ConnectedPoPaymentTerm.Utang)
        {
            RejectIfAnySettlementFieldPresent(request);
            return GoodsReceiptSettlement.Empty;
        }

        return GoodsReceiptSettlement.Empty;
    }

    private static void RejectIfAnySettlementFieldPresent(ReceivePurchaseOrderRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.GCashReference)
            || !string.IsNullOrWhiteSpace(request.BankName)
            || !string.IsNullOrWhiteSpace(request.TransferOrDepositReference)
            || request.SettlementDate is not null
            || !string.IsNullOrWhiteSpace(request.CheckNumber)
            || request.CheckDate is not null
            || !string.IsNullOrWhiteSpace(request.SettlementNotes)
            || !string.IsNullOrWhiteSpace(request.CheckClearingStatus))
        {
            throw new DomainException(
                ApplicationErrorCodes.PurchaseReceiptPaymentInvalid,
                "Settlement fields are not allowed for this payment method.");
        }
    }

    private static string NormalizeRequired(
        string? value,
        int maxLength,
        string errorCode,
        string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(errorCode, message);
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(errorCode, $"{message.TrimEnd('.')}. Maximum length is {maxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var trimmed = notes.Trim();
        if (trimmed.Length > GoodsReceiptSettlement.SettlementNotesMaxLength)
        {
            throw new DomainException(
                ApplicationErrorCodes.PurchaseReceiptPaymentInvalid,
                $"Settlement notes must be at most {GoodsReceiptSettlement.SettlementNotesMaxLength} characters.");
        }

        return trimmed;
    }
}
