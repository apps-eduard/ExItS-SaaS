using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>
/// Payment methods for a retail sale. Exactly one per sale — no split or partial tender.
/// <c>ManualGCash</c> / <c>ManualMaya</c> / <c>BankTransfer</c> are manually confirmed:
/// no gateway, QR, or provider verification is performed.
/// <c>Check</c> completes the sale with deferred settlement status (Pending / Cleared / Bounced).
/// <c>Card</c> / <c>GCash</c> are provider-backed electronic paths (AwaitingPayment).
/// Online catalog channels (OnlineMaya, QrPh, …) are entitlement/UI foundations only until integrated.
/// Member names are the stable persistence codes; localized labels live in UI resource files only.
/// </summary>
public enum SalePaymentMethod
{
    Cash = 0,
    ManualGCash = 1,
    Utang = 2,
    Card = 3,
    GCash = 4,
    BankTransfer = 5,
    Check = 6,
    ManualMaya = 7
}

public static class SalePaymentMethods
{
    public const int CodeMaxLength = 32;

    /// <summary>Stable persistence codes in canonical display order.</summary>
    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(SalePaymentMethod.Cash),
        nameof(SalePaymentMethod.ManualGCash),
        nameof(SalePaymentMethod.Utang),
        nameof(SalePaymentMethod.Card),
        nameof(SalePaymentMethod.GCash),
        nameof(SalePaymentMethod.BankTransfer),
        nameof(SalePaymentMethod.Check),
        nameof(SalePaymentMethod.ManualMaya)
    ];

    public static bool IsElectronic(SalePaymentMethod method) =>
        method is SalePaymentMethod.Card or SalePaymentMethod.GCash;

    /// <summary>Manual confirmation tenders (no cash drawer tender/change).</summary>
    public static bool IsManualConfirmation(SalePaymentMethod method) =>
        method is SalePaymentMethod.ManualGCash
            or SalePaymentMethod.BankTransfer
            or SalePaymentMethod.Check
            or SalePaymentMethod.ManualMaya;

    /// <summary>
    /// True when checkout creates a customer receivable / credit exposure (Utang).
    /// Distinct from <see cref="PaymentSettlementMode.Deferred"/> (e.g. Check) which is not debt.
    /// </summary>
    public static bool CreatesReceivable(SalePaymentMethod method) =>
        method is SalePaymentMethod.Utang;

    public static string ToCode(SalePaymentMethod method) => method.ToString();

    public static bool TryParse(string? code, out SalePaymentMethod method)
    {
        method = SalePaymentMethod.Cash;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var trimmed = code.Trim();
        var match = Codes.FirstOrDefault(c => string.Equals(c, trimmed, StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return false;
        }

        method = Enum.Parse<SalePaymentMethod>(match, ignoreCase: false);
        return true;
    }

    public static SalePaymentMethod Parse(string? code)
    {
        if (!TryParse(code, out var method))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSalePaymentMethod,
                $"Payment method must be one of: {string.Join(", ", Codes)}.");
        }

        return method;
    }
}

/// <summary>Settlement lifecycle for Check tenders. Not used for Cash / ManualGCash / Utang.</summary>
public enum CheckSettlementStatus
{
    Pending = 0,
    Cleared = 1,
    Bounced = 2
}

public static class CheckSettlementStatuses
{
    public static IReadOnlyList<string> Codes { get; } =
    [
        nameof(CheckSettlementStatus.Pending),
        nameof(CheckSettlementStatus.Cleared),
        nameof(CheckSettlementStatus.Bounced)
    ];

    public static bool TryParse(string? code, out CheckSettlementStatus status)
    {
        status = CheckSettlementStatus.Pending;
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var match = Codes.FirstOrDefault(c =>
            string.Equals(c, code.Trim(), StringComparison.OrdinalIgnoreCase));
        if (match is null)
        {
            return false;
        }

        status = Enum.Parse<CheckSettlementStatus>(match, ignoreCase: false);
        return true;
    }
}
