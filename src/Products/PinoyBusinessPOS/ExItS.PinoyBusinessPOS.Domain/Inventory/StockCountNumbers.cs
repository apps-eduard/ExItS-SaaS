using System.Globalization;
using System.Text.RegularExpressions;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Organization-scoped human-readable stock count number. New allocations use
/// <c>CNT-YYYYMMDD-NN</c> (two digits, expanding naturally past 99). Historical values such as
/// <c>CNT-YYYYMMDD-000001</c> remain valid. Allocated server-side per organization and business date.
/// </summary>
public static partial class StockCountNumbers
{
    public const string Prefix = "CNT";
    public const int MinSequenceDigits = 2;
    public const int MaxLength = 32;
    public const long MaxSequence = 999_999L;

    private static readonly Regex ValidPattern = CreateValidPattern();

    public static string Format(DateOnly businessDate, long sequence)
    {
        if (sequence is < 1 or > MaxSequence)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockCountNumber,
                $"Stock count sequence must be between 1 and {MaxSequence} for a single business date.");
        }

        var sequenceText = sequence.ToString($"D{MinSequenceDigits}", CultureInfo.InvariantCulture);
        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Prefix}-{businessDate:yyyyMMdd}-{sequenceText}");
    }

    public static string Normalize(string? countNumber)
    {
        if (string.IsNullOrWhiteSpace(countNumber))
        {
            throw new DomainException(DomainErrorCodes.InvalidStockCountNumber, "Stock count number is required.");
        }

        var trimmed = countNumber.Trim().ToUpperInvariant();
        if (trimmed.Length > MaxLength || !ValidPattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStockCountNumber,
                "Stock count number must look like CNT-YYYYMMDD-NN.");
        }

        return trimmed;
    }

    public static DateOnly BusinessDateOf(DateTimeOffset utcNow) => DateOnly.FromDateTime(utcNow.UtcDateTime);

    [GeneratedRegex(@"^CNT-\d{8}-\d{2,}$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateValidPattern();
}
