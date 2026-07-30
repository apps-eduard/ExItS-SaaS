using System.Globalization;
using System.Text.RegularExpressions;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Expenses;

/// <summary>
/// Organization-scoped human-readable expense number: <c>EXP-YYYYMMDD-NNNNNN</c>.
/// The sequence is allocated server-side per organization and business date; clients never propose
/// an expense number. Uniqueness is enforced per organization.
/// </summary>
public static partial class ExpenseNumbers
{
    public const string Prefix = "EXP";
    public const int SequenceDigits = 6;
    public const int MaxLength = 32;
    public const long MaxSequence = 999_999L;

    private static readonly Regex ValidPattern = CreateValidPattern();

    public static string Format(DateOnly businessDate, long sequence)
    {
        if (sequence is < 1 or > MaxSequence)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpenseNumber,
                $"Expense sequence must be between 1 and {MaxSequence} for a single business date.");
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Prefix}-{businessDate:yyyyMMdd}-{sequence.ToString($"D{SequenceDigits}", CultureInfo.InvariantCulture)}");
    }

    public static string Normalize(string? expenseNumber)
    {
        if (string.IsNullOrWhiteSpace(expenseNumber))
        {
            throw new DomainException(DomainErrorCodes.InvalidExpenseNumber, "Expense number is required.");
        }

        var trimmed = expenseNumber.Trim().ToUpperInvariant();
        if (trimmed.Length > MaxLength || !ValidPattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidExpenseNumber,
                "Expense number must look like EXP-YYYYMMDD-NNNNNN.");
        }

        return trimmed;
    }

    /// <summary>Derives the business date used for sequence allocation from a UTC timestamp.</summary>
    public static DateOnly BusinessDateOf(DateTimeOffset utcNow) => DateOnly.FromDateTime(utcNow.UtcDateTime);

    [GeneratedRegex(@"^EXP-\d{8}-\d{6,}$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateValidPattern();
}
