using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Expenses;

/// <summary>
/// Organization-scoped expense number: <c>EXP-YYMMDD-NNN</c>.
/// Allocated server-side on expense post.
/// </summary>
public static class ExpenseNumbers
{
    public const string Prefix = PosDocumentPrefixes.Expense;
    public const int MaxLength = PosDocumentNumbers.MaxLength;
    public const long MaxSequence = PosDocumentNumbers.MaxSequence;

    public static string Format(DateOnly businessDate, long sequence) =>
        Map(() => PosDocumentNumbers.Format(Prefix, businessDate, sequence));

    public static string Normalize(string? expenseNumber) =>
        Map(() => PosDocumentNumbers.NormalizeRoot(expenseNumber, Prefix));

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