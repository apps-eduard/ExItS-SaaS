using System.Globalization;
using System.Text.RegularExpressions;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Sales;

/// <summary>
/// Organization-scoped human-readable sale number: <c>SALE-YYYYMMDD-NNNNNN</c>.
/// The sequence is allocated server-side per organization and business date; clients never propose
/// a sale number. Uniqueness is enforced per organization, so two organizations may legitimately
/// hold the same sale number.
/// </summary>
public static partial class SaleNumbers
{
    public const string Prefix = "SALE";
    public const int SequenceDigits = 6;
    public const int MaxLength = 32;
    public const long MaxSequence = 999_999L;

    private static readonly Regex ValidPattern = CreateValidPattern();

    public static string Format(DateOnly businessDate, long sequence)
    {
        if (sequence is < 1 or > MaxSequence)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleNumber,
                $"Sale sequence must be between 1 and {MaxSequence} for a single business date.");
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Prefix}-{businessDate:yyyyMMdd}-{sequence.ToString($"D{SequenceDigits}", CultureInfo.InvariantCulture)}");
    }

    public static string Normalize(string? saleNumber)
    {
        if (string.IsNullOrWhiteSpace(saleNumber))
        {
            throw new DomainException(DomainErrorCodes.InvalidSaleNumber, "Sale number is required.");
        }

        var trimmed = saleNumber.Trim().ToUpperInvariant();
        if (trimmed.Length > MaxLength || !ValidPattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSaleNumber,
                "Sale number must look like SALE-YYYYMMDD-NNNNNN.");
        }

        return trimmed;
    }

    /// <summary>Derives the business date used for sequence allocation from a UTC timestamp.</summary>
    public static DateOnly BusinessDateOf(DateTimeOffset utcNow) => DateOnly.FromDateTime(utcNow.UtcDateTime);

    [GeneratedRegex(@"^SALE-\d{8}-\d{6,}$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateValidPattern();
}
