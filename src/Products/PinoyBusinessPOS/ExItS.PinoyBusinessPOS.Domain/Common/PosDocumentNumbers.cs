using System.Collections.Frozen;
using System.Globalization;
using System.Text.RegularExpressions;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Common;

/// <summary>
/// Canonical short prefixes for POS business-document numbers (<c>PREFIX-YYMMDD-NNN</c>).
/// Each transaction-specific <c>*Numbers</c> class owns its prefix constant; this set is the
/// server-side allow-list for validation (clients never choose a prefix).
/// </summary>
public static class PosDocumentPrefixes
{
    public const string Sale = "SAL";
    public const string SaleReturn = "RET";
    public const string Expense = "EXP";
    public const string StockUse = "SU";
    public const string ReturnBatch = "RB";
    public const string WasteLoss = "WL";
    public const string Production = "PRD";
    public const string Quotation = "QUO";
    public const string StockCount = "SC";
    public const string StockRequest = "SR";
    public const string GoodsReceipt = "GRN";
    public const string PurchaseOrder = "PO";
    public const string CashierShift = "SH";
    public const string InventoryTransfer = "TR";
    public const string CustomerOrder = "ORD";
    public const string DirectPurchase = "DP";

    public static readonly FrozenSet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Sale,
        SaleReturn,
        Expense,
        StockUse,
        ReturnBatch,
        WasteLoss,
        Production,
        Quotation,
        StockCount,
        StockRequest,
        GoodsReceipt,
        PurchaseOrder,
        CashierShift,
        InventoryTransfer,
        CustomerOrder,
        DirectPurchase,
    }.ToFrozenSet(StringComparer.Ordinal);
}

/// <summary>
/// Shared POS business-document reference format: <c>PREFIX-YYMMDD-NNN</c>.
/// Sequence is scoped per organization + document type + business date (not one global counter).
/// Child documents that belong to a true family append <c>-Rn</c> to the root number.
/// Legacy unprefixed <c>YYMMDD-NNN</c> values remain readable for historical rows.
/// </summary>
public static partial class PosDocumentNumbers
{
    public const int MinSequenceDigits = 3;
    public const int MaxLength = 24;
    public const long MaxSequence = 999_999L;
    public const int MaxChildSequence = 9_999;
    public const int MinPrefixLength = 2;
    public const int MaxPrefixLength = 4;

    private static readonly Regex ValidPattern = CreateValidPattern();
    private static readonly Regex RootPattern = CreateRootPattern();
    private static readonly Regex LegacyRootPattern = CreateLegacyRootPattern();
    private static readonly Regex LegacyValidPattern = CreateLegacyValidPattern();

    /// <summary>Formats <c>PREFIX-YYMMDD-NNN</c> (prefix uppercased; sequence min 3 digits).</summary>
    public static string Format(string prefix, DateOnly businessDate, long sequence)
    {
        var normalizedPrefix = NormalizePrefix(prefix);
        if (sequence is < 1 or > MaxSequence)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPosDocumentNumber,
                $"Document sequence must be between 1 and {MaxSequence} for a single business date.");
        }

        var datePart = businessDate.ToString("yyMMdd", CultureInfo.InvariantCulture);
        var sequencePart = FormatSequence(sequence);
        var result = string.Create(
            CultureInfo.InvariantCulture,
            $"{normalizedPrefix}-{datePart}-{sequencePart}");
        if (result.Length > MaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPosDocumentNumber,
                $"Document number exceeds max length {MaxLength}.");
        }

        return result;
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

        var result = string.Create(
            CultureInfo.InvariantCulture,
            $"{root}-R{childSequence.ToString(CultureInfo.InvariantCulture)}");
        if (result.Length > MaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPosDocumentNumber,
                $"Document number exceeds max length {MaxLength}.");
        }

        return result;
    }

    /// <summary>
    /// Normalizes a document number (root or child). Accepts current
    /// <c>PREFIX-YYMMDD-NNN[-Rn]</c> and legacy unprefixed <c>YYMMDD-NNN[-Rn]</c>.
    /// When <paramref name="expectedPrefix"/> is set, a prefixed value must use that prefix.
    /// </summary>
    public static string Normalize(string? documentNumber, string? expectedPrefix = null)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPosDocumentNumber,
                "Document number is required.");
        }

        var trimmed = documentNumber.Trim().ToUpperInvariant();
        if (trimmed.Length > MaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPosDocumentNumber,
                "Document number must look like PREFIX-YYMMDD-NNN or PREFIX-YYMMDD-NNN-Rn.");
        }

        if (LegacyValidPattern.IsMatch(trimmed))
        {
            // Historical unprefixed rows remain readable; do not rewrite.
            return trimmed;
        }

        if (!ValidPattern.IsMatch(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPosDocumentNumber,
                "Document number must look like PREFIX-YYMMDD-NNN or PREFIX-YYMMDD-NNN-Rn.");
        }

        var prefix = ExtractPrefix(trimmed);
        if (!PosDocumentPrefixes.All.Contains(prefix))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPosDocumentNumber,
                $"Unknown document number prefix '{prefix}'.");
        }

        if (!string.IsNullOrWhiteSpace(expectedPrefix))
        {
            var expected = NormalizePrefix(expectedPrefix);
            if (!string.Equals(prefix, expected, StringComparison.Ordinal))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPosDocumentNumber,
                    $"Document number prefix must be {expected}.");
            }
        }

        return trimmed;
    }

    public static string NormalizeRoot(string? documentNumber, string? expectedPrefix = null)
    {
        var normalized = Normalize(documentNumber, expectedPrefix);
        if (RootPattern.IsMatch(normalized) || LegacyRootPattern.IsMatch(normalized))
        {
            return normalized;
        }

        throw new DomainException(
            DomainErrorCodes.InvalidPosDocumentNumber,
            "Root document number must look like PREFIX-YYMMDD-NNN.");
    }

    public static bool TryNormalize(string? documentNumber, out string normalized, string? expectedPrefix = null)
    {
        normalized = string.Empty;
        try
        {
            normalized = Normalize(documentNumber, expectedPrefix);
            return true;
        }
        catch (DomainException)
        {
            return false;
        }
    }

    public static DateOnly BusinessDateOf(DateTimeOffset utcNow) => DateOnly.FromDateTime(utcNow.UtcDateTime);

    public static string NormalizePrefix(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPosDocumentNumber,
                "Document number prefix is required.");
        }

        var trimmed = prefix.Trim().ToUpperInvariant();
        if (trimmed.Length is < MinPrefixLength or > MaxPrefixLength
            || trimmed.Any(c => c is < 'A' or > 'Z'))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPosDocumentNumber,
                $"Document number prefix must be {MinPrefixLength}–{MaxPrefixLength} letters.");
        }

        if (!PosDocumentPrefixes.All.Contains(trimmed))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPosDocumentNumber,
                $"Unknown document number prefix '{trimmed}'.");
        }

        return trimmed;
    }

    private static string ExtractPrefix(string normalizedNumber)
    {
        var dash = normalizedNumber.IndexOf('-');
        return dash <= 0 ? string.Empty : normalizedNumber[..dash];
    }

    private static string FormatSequence(long sequence)
    {
        var raw = sequence.ToString(CultureInfo.InvariantCulture);
        return raw.Length < MinSequenceDigits
            ? raw.PadLeft(MinSequenceDigits, '0')
            : raw;
    }

    // PREFIX-YYMMDD-NNN optional -Rn
    [GeneratedRegex(@"^[A-Z]{2,4}-\d{6}-\d{3,}(-R\d+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateValidPattern();

    [GeneratedRegex(@"^[A-Z]{2,4}-\d{6}-\d{3,}$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateRootPattern();

    // Legacy unprefixed (2026-09 standardization without prefixes)
    [GeneratedRegex(@"^\d{6}-\d{3,}(-R\d+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateLegacyValidPattern();

    [GeneratedRegex(@"^\d{6}-\d{3,}$", RegexOptions.CultureInvariant)]
    private static partial Regex CreateLegacyRootPattern();
}
