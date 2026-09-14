using System.Globalization;
using System.Text.RegularExpressions;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Quotations;

/// <summary>
/// Organization-scoped human-readable quotation number: <c>QT-YYYYMMDD-NNNNNN</c>.
/// Allocated server-side per organization and business date on Issue.
/// </summary>
public static partial class QuotationNumbers
{
    public const string Prefix = "QT";
    public const int SequenceDigits = 6;
    public const int MaxLength = 32;
    public const long MaxSequence = 999_999L;

    private static readonly Regex ValidPattern = CreateValidPattern();

    public static string Format(DateOnly businessDate, long sequence)
    {
        if (sequence is < 1 or > MaxSequence)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidQuotationNumber,
                $"Quotation sequence must be between 1 and {MaxSequence} for a single business date.");
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Prefix}-{businessDate:yyyyMMdd}-{sequence.ToString($"D{SequenceDigits}", CultureInfo.InvariantCulture)}");
    }

    public static string Normalize(string? quotationNumber)
    {
        if (string.IsNullOrWhiteSpace(quotationNumber))
        {
            throw new DomainException(DomainErrorCodes.InvalidQuotationNumber, "Quotation number is required.");
        }

        var trimmed = quotationNumber.Trim().ToUpperInvariant();
        if (trimmed.Length > MaxLength || !ValidPattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidQuotationNumber,
                "Quotation number must look like QT-YYYYMMDD-NNNNNN.");
        }

        return trimmed;
    }

    public static DateOnly BusinessDateOf(DateTimeOffset utcNow) => DateOnly.FromDateTime(utcNow.UtcDateTime);

    [GeneratedRegex(@"^QT-\d{8}-\d{6,}$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateValidPattern();
}
