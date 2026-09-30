using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.CashierShifts;

/// <summary>
/// Organization-scoped cashier shift number: <c>SH-YYMMDD-NNN</c>.
/// Allocated server-side on shift open.
/// </summary>
public static class CashierShiftNumbers
{
    public const string Prefix = PosDocumentPrefixes.CashierShift;
    public const int MaxLength = PosDocumentNumbers.MaxLength;
    public const long MaxSequence = PosDocumentNumbers.MaxSequence;

    public static string Format(DateOnly businessDate, long sequence) =>
        Map(() => PosDocumentNumbers.Format(Prefix, businessDate, sequence));

    public static string Normalize(string? shiftNumber) =>
        Map(() => PosDocumentNumbers.NormalizeRoot(shiftNumber, Prefix));

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