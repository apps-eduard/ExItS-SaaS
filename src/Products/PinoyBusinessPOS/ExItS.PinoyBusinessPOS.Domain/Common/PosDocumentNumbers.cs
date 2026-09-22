using System.Globalization;
using System.Text.RegularExpressions;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Common;

/// <summary>
/// Shared POS business-document reference format: <c>YYMMDD-NNN</c>.
/// Sequence is scoped per organization + document type + business date (not one global counter).
/// Child documents that belong to a true family append <c>-Rn</c> to the root number.
/// </summary>
public static partial class PosDocumentNumbers
{
    public const int MinSequenceDigits = 3;
    public const int MaxLength = 24;
    public const long MaxSequence = 999_999L;
    public const int MaxChildSequence = 9_999;

    private static readonly Regex ValidPattern = CreateValidPattern();
    private static readonly Regex RootPattern = CreateRootPattern();

    public static string Format(DateOnly businessDate, long sequence)
    {
        if (sequence is < 1 or > MaxSequence)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPosDocumentNumber,
                $"Document sequence must be between 1 and {MaxSequence} for a single business date.");
        }

        var datePart = businessDate.ToString("yyMMdd", CultureInfo.InvariantCulture);
        var sequencePart = FormatSequence(sequence);
        return string.Create(CultureInfo.InvariantCulture, $"{datePart}-{sequencePart}");
    }

    public static string FormatChild(string rootNumber, int childSequence)
    {
        var root = NormalizeRoot(rootNumber);
        if (childSequence is < 1 or > MaxChildSequence)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPosDocumentChildSequence,
                $"Child sequence must be between 1 and {MaxChildSequence}.");
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{root}-R{childSequence.ToString(CultureInfo.InvariantCulture)}");
    }

    public static string Normalize(string? documentNumber)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPosDocumentNumber,
                "Document number is required.");
        }

        var trimmed = documentNumber.Trim().ToUpperInvariant();
        if (trimmed.Length > MaxLength || !ValidPattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPosDocumentNumber,
                "Document number must look like YYMMDD-NNN or YYMMDD-NNN-Rn.");
        }

        return trimmed;
    }

    public static string NormalizeRoot(string? documentNumber)
    {
        var normalized = Normalize(documentNumber);
        if (!RootPattern.IsMatch(normalized))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPosDocumentNumber,
                "Root document number must look like YYMMDD-NNN.");
        }

        return normalized;
    }

    public static bool TryNormalize(string? documentNumber, out string normalized)
    {
        normalized = string.Empty;
        try
        {
            normalized = Normalize(documentNumber);
            return true;
        }
        catch (DomainException)
        {
            return false;
        }
    }

    public static DateOnly BusinessDateOf(DateTimeOffset utcNow) => DateOnly.FromDateTime(utcNow.UtcDateTime);

    private static string FormatSequence(long sequence)
    {
        var raw = sequence.ToString(CultureInfo.InvariantCulture);
        return raw.Length < MinSequenceDigits
            ? raw.PadLeft(MinSequenceDigits, '0')
            : raw;
    }

    [GeneratedRegex(@"^\d{6}-\d{3,}(-R\d+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateValidPattern();

    [GeneratedRegex(@"^\d{6}-\d{3,}$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateRootPattern();
}
