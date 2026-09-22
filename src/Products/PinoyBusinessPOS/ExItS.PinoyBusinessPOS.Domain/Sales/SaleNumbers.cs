using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>
/// Organization-scoped sale number: <c>YYMMDD-NNN</c> (shared POS document format).
/// Allocated server-side per organization and business date; clients never propose one.
/// </summary>
public static class SaleNumbers
{
    public const int MaxLength = PosDocumentNumbers.MaxLength;
    public const long MaxSequence = PosDocumentNumbers.MaxSequence;

    public static string Format(DateOnly businessDate, long sequence) =>
        Map(() => PosDocumentNumbers.Format(businessDate, sequence));

    public static string Normalize(string? saleNumber) =>
        Map(() => PosDocumentNumbers.NormalizeRoot(saleNumber));

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
            throw new DomainException(DomainErrorCodes.InvalidSaleNumber, ex.Message);
        }
    }
}
