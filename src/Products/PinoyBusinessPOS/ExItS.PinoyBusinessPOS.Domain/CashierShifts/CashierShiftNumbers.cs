using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.CashierShifts;

/// <summary>
/// Organization-scoped cashier shift number: <c>YYMMDD-NNN</c> (shared POS document format).
/// Allocated server-side per organization and business date on open.
/// </summary>
public static class CashierShiftNumbers
{
    public const int MaxLength = PosDocumentNumbers.MaxLength;
    public const long MaxSequence = PosDocumentNumbers.MaxSequence;

    public static string Format(DateOnly businessDate, long sequence) =>
        Map(() => PosDocumentNumbers.Format(businessDate, sequence));

    public static string Normalize(string? shiftNumber) =>
        Map(() => PosDocumentNumbers.NormalizeRoot(shiftNumber));

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
            throw new DomainException(DomainErrorCodes.InvalidCashierShiftNumber, ex.Message);
        }
    }
}
