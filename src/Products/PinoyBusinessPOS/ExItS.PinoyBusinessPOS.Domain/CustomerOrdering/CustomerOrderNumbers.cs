using System.Globalization;
using System.Text.RegularExpressions;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;

/// <summary>
/// Organization-scoped human-readable customer order number: <c>SO-000001</c>.
/// Sequence is allocated server-side per organization; clients never propose one.
/// </summary>
public static partial class CustomerOrderNumbers
{
    public const string Prefix = "SO";
    public const int SequenceDigits = 6;
    public const int MaxLength = 16;
    public const long MaxSequence = 999_999L;

    private static readonly Regex ValidPattern = CreateValidPattern();

    public static string Format(long sequence)
    {
        if (sequence is < 1 or > MaxSequence)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderNumber,
                $"Customer order sequence must be between 1 and {MaxSequence}.");
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Prefix}-{sequence.ToString($"D{SequenceDigits}", CultureInfo.InvariantCulture)}");
    }

    public static string Normalize(string? orderNumber)
    {
        if (string.IsNullOrWhiteSpace(orderNumber))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderNumber,
                "Customer order number is required.");
        }

        var trimmed = orderNumber.Trim().ToUpperInvariant();
        if (trimmed.Length > MaxLength || !ValidPattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCustomerOrderNumber,
                "Customer order number must look like SO-000001.");
        }

        return trimmed;
    }

    [GeneratedRegex(@"^SO-\d{6,}$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateValidPattern();
}
