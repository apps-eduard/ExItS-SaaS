using System.Globalization;
using System.Text.RegularExpressions;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Purchasing;

/// <summary>
/// Organization-scoped human-readable PO number: <c>PO-YYYYMMDD-NNNNNN</c>.
/// Allocated server-side per organization and business date on submit.
/// </summary>
public static partial class PurchaseOrderNumbers
{
    public const string Prefix = "PO";
    public const int SequenceDigits = 6;
    public const int MaxLength = 32;
    public const long MaxSequence = 999_999L;

    private static readonly Regex ValidPattern = CreateValidPattern();

    public static string Format(DateOnly businessDate, long sequence)
    {
        if (sequence is < 1 or > MaxSequence)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderNumber,
                $"PO sequence must be between 1 and {MaxSequence} for a single business date.");
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Prefix}-{businessDate:yyyyMMdd}-{sequence.ToString($"D{SequenceDigits}", CultureInfo.InvariantCulture)}");
    }

    public static string Normalize(string? poNumber)
    {
        if (string.IsNullOrWhiteSpace(poNumber))
        {
            throw new DomainException(DomainErrorCodes.InvalidPurchaseOrderNumber, "PO number is required.");
        }

        var trimmed = poNumber.Trim().ToUpperInvariant();
        if (trimmed.Length > MaxLength || !ValidPattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPurchaseOrderNumber,
                "PO number must look like PO-YYYYMMDD-NNNNNN.");
        }

        return trimmed;
    }

    public static DateOnly BusinessDateOf(DateTimeOffset utcNow) => DateOnly.FromDateTime(utcNow.UtcDateTime);

    [GeneratedRegex(@"^PO-\d{8}-\d{6,}$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateValidPattern();
}
