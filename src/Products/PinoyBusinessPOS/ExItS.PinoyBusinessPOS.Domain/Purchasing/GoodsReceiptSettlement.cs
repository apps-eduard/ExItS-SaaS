using ExItS.PinoyBusinessPOS.Domain.Payments;

namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

/// <summary>Optional payment-at-receipt settlement metadata captured on a goods receipt.</summary>
public sealed record GoodsReceiptSettlement(
    string? GCashReference,
    string? BankName,
    string? TransferOrDepositReference,
    DateOnly? SettlementDate,
    string? CheckNumber,
    DateOnly? CheckDate,
    string? SettlementNotes,
    UtangCheckClearingStatus? CheckClearingStatus)
{
    public const int GCashReferenceMaxLength = 64;
    public const int SettlementNotesMaxLength = 512;

    public static GoodsReceiptSettlement Empty { get; } = new(null, null, null, null, null, null, null, null);
}
