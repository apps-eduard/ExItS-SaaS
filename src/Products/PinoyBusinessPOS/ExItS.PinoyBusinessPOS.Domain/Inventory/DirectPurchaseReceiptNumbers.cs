using System.Globalization;
using System.Text.RegularExpressions;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Organization-scoped human-readable receipt number: <c>DPR-YYYYMMDD-NNNNNN</c>.
/// Allocated server-side per organization and business date on create.
/// </summary>
public static partial class DirectPurchaseReceiptNumbers
{
    public const string Prefix = "DPR";
    public const int SequenceDigits = 6;
    public const int MaxLength = 32;
    public const long MaxSequence = 999_999L;

    private static readonly Regex ValidPattern = CreateValidPattern();

    public static string Format(DateOnly businessDate, long sequence)
    {
        if (sequence is < 1 or > MaxSequence)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDirectPurchaseReceiptNumber,
                $"Direct purchase receipt sequence must be between 1 and {MaxSequence} for a single business date.");
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Prefix}-{businessDate:yyyyMMdd}-{sequence.ToString($"D{SequenceDigits}", CultureInfo.InvariantCulture)}");
    }

    public static string Normalize(string? receiptNumber)
    {
        if (string.IsNullOrWhiteSpace(receiptNumber))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDirectPurchaseReceiptNumber,
                "Direct purchase receipt number is required.");
        }

        var trimmed = receiptNumber.Trim().ToUpperInvariant();
        if (trimmed.Length > MaxLength || !ValidPattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDirectPurchaseReceiptNumber,
                "Direct purchase receipt number must look like DPR-YYYYMMDD-NNNNNN.");
        }

        return trimmed;
    }

    public static DateOnly BusinessDateOf(DateTimeOffset utcNow) => DateOnly.FromDateTime(utcNow.UtcDateTime);

    [GeneratedRegex(@"^DPR-\d{8}-\d{6,}$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateValidPattern();
}
