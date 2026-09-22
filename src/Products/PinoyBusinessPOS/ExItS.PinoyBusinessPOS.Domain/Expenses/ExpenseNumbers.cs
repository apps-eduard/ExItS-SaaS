using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Expenses;

/// <summary>
/// Organization-scoped expense number: <c>YYMMDD-NNN</c> (shared POS document format).
/// Allocated server-side per organization and business date; clients never propose an expense number.
/// </summary>
public static class ExpenseNumbers
{
    public const int MaxLength = PosDocumentNumbers.MaxLength;
    public const long MaxSequence = PosDocumentNumbers.MaxSequence;

    public static string Format(DateOnly businessDate, long sequence) =>
        Map(() => PosDocumentNumbers.Format(businessDate, sequence));

    public static string Normalize(string? expenseNumber) =>
        Map(() => PosDocumentNumbers.NormalizeRoot(expenseNumber));

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
            throw new DomainException(DomainErrorCodes.InvalidExpenseNumber, ex.Message);
        }
    }
}
