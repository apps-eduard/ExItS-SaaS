using System.Globalization;
using System.Text.RegularExpressions;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

/// <summary>
/// Organization-scoped human-readable GRN number: <c>GRN-YYYYMMDD-NNNNNN</c>.
/// Allocated server-side per organization and business date on receive.
/// </summary>
public static partial class GoodsReceiptNumbers
{
    public const string Prefix = "GRN";
    public const int SequenceDigits = 6;
    public const int MaxLength = 32;
    public const long MaxSequence = 999_999L;

    private static readonly Regex ValidPattern = CreateValidPattern();

    public static string Format(DateOnly businessDate, long sequence)
    {
        if (sequence is < 1 or > MaxSequence)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGoodsReceiptNumber,
                $"GRN sequence must be between 1 and {MaxSequence} for a single business date.");
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Prefix}-{businessDate:yyyyMMdd}-{sequence.ToString($"D{SequenceDigits}", CultureInfo.InvariantCulture)}");
    }

    public static string Normalize(string? grnNumber)
    {
        if (string.IsNullOrWhiteSpace(grnNumber))
        {
            throw new DomainException(DomainErrorCodes.InvalidGoodsReceiptNumber, "GRN number is required.");
        }

        var trimmed = grnNumber.Trim().ToUpperInvariant();
        if (trimmed.Length > MaxLength || !ValidPattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGoodsReceiptNumber,
                "GRN number must look like GRN-YYYYMMDD-NNNNNN.");
        }

        return trimmed;
    }

    public static DateOnly BusinessDateOf(DateTimeOffset utcNow) => DateOnly.FromDateTime(utcNow.UtcDateTime);

    [GeneratedRegex(@"^GRN-\d{8}-\d{6,}$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateValidPattern();
}
