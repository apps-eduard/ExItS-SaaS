using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Organization-scoped direct purchase receipt number: <c>DP-YYMMDD-NNN</c>.
/// Allocated server-side on direct-purchase post.
/// </summary>
public static class DirectPurchaseReceiptNumbers
{
    public const string Prefix = PosDocumentPrefixes.DirectPurchase;
    public const int MaxLength = PosDocumentNumbers.MaxLength;
    public const long MaxSequence = PosDocumentNumbers.MaxSequence;

    public static string Format(DateOnly businessDate, long sequence) =>
        Map(() => PosDocumentNumbers.Format(Prefix, businessDate, sequence));

    public static string Normalize(string? receiptNumber) =>
        Map(() => PosDocumentNumbers.NormalizeRoot(receiptNumber, Prefix));

    public static DateOnly BusinessDateOf(DateTimeOffset utcNow) => PosDocumentNumbers.BusinessDateOf(utcNow);

    private static string Map(Func<string> action)
    {
        try
        {
            return action();
        }
        catch (DomainException ex) when (
            ex.ErrorCode is DomainErrorCodes.InvalidPosDocumentNumber
                or DomainErrorCodes.InvalidPosDocumentChildSequence)
        {
            throw new DomainException(DomainErrorCodes.InvalidDirectPurchaseReceiptNumber, ex.Message);
        }
    }
}