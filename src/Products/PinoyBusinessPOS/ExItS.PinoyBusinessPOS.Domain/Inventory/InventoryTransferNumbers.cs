using System.Globalization;
using System.Text.RegularExpressions;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Inventory;

/// <summary>
/// Organization-scoped transfer number: <c>TR-YYYYMMDD-NNNNNN</c>. Allocated on dispatch.
/// </summary>
public static partial class InventoryTransferNumbers
{
    public const string Prefix = "TR";
    public const int SequenceDigits = 6;
    public const int MaxLength = 32;
    public const long MaxSequence = 999_999L;

    private static readonly Regex ValidPattern = CreateValidPattern();

    public static string Format(DateOnly businessDate, long sequence)
    {
        if (sequence is < 1 or > MaxSequence)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferNumber,
                $"Transfer sequence must be between 1 and {MaxSequence} for a single business date.");
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Prefix}-{businessDate:yyyyMMdd}-{sequence.ToString($"D{SequenceDigits}", CultureInfo.InvariantCulture)}");
    }

    public static string Normalize(string? transferNumber)
    {
        if (string.IsNullOrWhiteSpace(transferNumber))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferNumber,
                "Transfer number is required.");
        }

        var trimmed = transferNumber.Trim().ToUpperInvariant();
        if (trimmed.Length > MaxLength || !ValidPattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidInventoryTransferNumber,
                "Transfer number must look like TR-YYYYMMDD-NNNNNN.");
        }

        return trimmed;
    }

    public static DateOnly BusinessDateOf(DateTimeOffset utcNow) => DateOnly.FromDateTime(utcNow.UtcDateTime);

    [GeneratedRegex(@"^TR-\d{8}-\d{6,}$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateValidPattern();
}
