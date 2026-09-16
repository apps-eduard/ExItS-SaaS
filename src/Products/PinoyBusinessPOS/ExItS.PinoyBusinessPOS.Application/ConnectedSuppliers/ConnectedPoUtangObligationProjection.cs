using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

/// <summary>
/// Shared math/labels for converting received Utang (PO receipt or Direct Purchase)
/// into matching seller receivable and buyer payable projections.
/// </summary>
public static class ConnectedPoUtangObligationProjection
{
    public const string GoodsReceiptRemarkPrefix = "grn:";
    public const string DirectPurchaseRemarkPrefix = "dpr:";

    /// <summary>
    /// For Utang POs, omitted PaidNow means 0 (credit), never default to full receipt total.
    /// Cash / other terms keep ADR-023 payable default (null → fully paid at receipt).
    /// </summary>
    public static decimal? ResolvePaidAtReceipt(
        ConnectedPoPaymentTerm paymentTerm,
        decimal receivedAmount,
        decimal? requestedPaidNow)
    {
        var received = SaleMoney.RoundMoney(receivedAmount);
        if (received <= 0m)
        {
            return 0m;
        }

        if (!ConnectedPoUtangCredit.UsesUtang(paymentTerm))
        {
            return requestedPaidNow;
        }

        var paid = requestedPaidNow is null
            ? 0m
            : SaleMoney.RoundMoney(requestedPaidNow.Value);
        if (paid < 0m)
        {
            paid = 0m;
        }

        return paid > received ? received : paid;
    }

    /// <summary>
    /// Net obligation to post on both seller BusinessCreditEntry and buyer SupplierPayable balance.
    /// </summary>
    public static decimal ObligationAmount(decimal receivedAmount, decimal paidAtReceipt)
    {
        var received = SaleMoney.RoundMoney(receivedAmount);
        var paid = SaleMoney.RoundMoney(paidAtReceipt);
        var obligation = received - paid;
        return obligation <= 0m ? 0m : SaleMoney.RoundMoney(obligation);
    }

    public static string BuildGoodsReceiptRemark(Guid goodsReceiptId, string? poNumber, Guid purchaseOrderId)
    {
        var label = string.IsNullOrWhiteSpace(poNumber)
            ? purchaseOrderId.ToString("D")
            : poNumber.Trim();
        return $"{GoodsReceiptRemarkPrefix}{goodsReceiptId:D}|Connected PO {label} receipt";
    }

    public static bool TryParseGoodsReceiptId(string? remarks, out Guid goodsReceiptId)
    {
        goodsReceiptId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(remarks)
            || !remarks.StartsWith(GoodsReceiptRemarkPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var rest = remarks[GoodsReceiptRemarkPrefix.Length..];
        var pipe = rest.IndexOf('|');
        var token = pipe >= 0 ? rest[..pipe] : rest;
        return Guid.TryParse(token, out goodsReceiptId);
    }

    public static string BuildDirectPurchaseRemark(Guid directPurchaseReceiptId, string? receiptNumber)
    {
        var label = string.IsNullOrWhiteSpace(receiptNumber)
            ? directPurchaseReceiptId.ToString("D")
            : receiptNumber.Trim();
        return $"{DirectPurchaseRemarkPrefix}{directPurchaseReceiptId:D}|Direct purchase {label}";
    }

    public static bool TryParseDirectPurchaseReceiptId(string? remarks, out Guid directPurchaseReceiptId)
    {
        directPurchaseReceiptId = Guid.Empty;
        if (string.IsNullOrWhiteSpace(remarks)
            || !remarks.StartsWith(DirectPurchaseRemarkPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var rest = remarks[DirectPurchaseRemarkPrefix.Length..];
        var pipe = rest.IndexOf('|');
        var token = pipe >= 0 ? rest[..pipe] : rest;
        return Guid.TryParse(token, out directPurchaseReceiptId);
    }

    /// <summary>
    /// Human label for payable/credit UI: "PO-100" or "Direct purchase DPR-…".
    /// </summary>
    public static string? TryFormatSourceLabelFromRemark(string? remarks)
    {
        if (string.IsNullOrWhiteSpace(remarks))
        {
            return null;
        }

        if (remarks.StartsWith(GoodsReceiptRemarkPrefix, StringComparison.Ordinal))
        {
            var pipe = remarks.IndexOf('|');
            if (pipe < 0 || pipe >= remarks.Length - 1)
            {
                return "PO";
            }

            var label = remarks[(pipe + 1)..].Trim();
            const string prefix = "Connected PO ";
            const string suffix = " receipt";
            if (label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                label = label[prefix.Length..];
            }

            if (label.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                label = label[..^suffix.Length].Trim();
            }

            return string.IsNullOrWhiteSpace(label) ? "PO" : label;
        }

        if (remarks.StartsWith(DirectPurchaseRemarkPrefix, StringComparison.Ordinal))
        {
            var pipe = remarks.IndexOf('|');
            if (pipe < 0 || pipe >= remarks.Length - 1)
            {
                return "Direct purchase";
            }

            var label = remarks[(pipe + 1)..].Trim();
            const string prefix = "Direct purchase ";
            if (label.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var number = label[prefix.Length..].Trim();
                return string.IsNullOrWhiteSpace(number)
                    ? "Direct purchase"
                    : $"Direct purchase {number}";
            }

            return label;
        }

        return null;
    }
}
