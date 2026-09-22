using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Returns;

/// <summary>
/// Organization-scoped return number: <c>YYMMDD-NNN</c> (shared POS document format).
/// Allocated server-side per organization and business date.
/// </summary>
public static class ReturnNumbers
{
    public const int MaxLength = PosDocumentNumbers.MaxLength;
    public const long MaxSequence = PosDocumentNumbers.MaxSequence;

    public static string Format(DateOnly businessDate, long sequence) =>
        Map(() => PosDocumentNumbers.Format(businessDate, sequence));

    public static string Normalize(string? returnNumber) =>
        Map(() => PosDocumentNumbers.NormalizeRoot(returnNumber));

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
            throw new DomainException(DomainErrorCodes.InvalidSaleReturnNumber, ex.Message);
        }
    }
}
