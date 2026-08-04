using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.GlobalCatalog;

/// <summary>Limits and cell sanitization for Platform global-catalog bulk imports.</summary>
public static class CatalogImportRules
{
    public const int MaxFileBytes = 5 * 1024 * 1024;
    public const int MaxRows = 5_000;
    public const int MaxPreviewRows = 50;
    public const int ProcessingChunkSize = 50;
    public const int MaxTransientAttempts = 3;
    public const int HeartbeatStaleSeconds = 120;
    public const int FileNameMaxLength = 260;
    public const int ErrorMessageMaxLength = 1000;
    public const int IdempotencyKeyMaxLength = 128;
    public const int ContentTypeMaxLength = 128;
    public const int Sha256HexLength = 64;

    private static readonly HashSet<char> FormulaPrefixes = ['=', '+', '-', '@'];

    /// <summary>
    /// Detects spreadsheet formula-injection prefixes (= + - @) after leading whitespace.
    /// </summary>
    public static bool LooksLikeFormulaInjection(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.TrimStart();
        return trimmed.Length > 0 && FormulaPrefixes.Contains(trimmed[0]);
    }

    /// <summary>
    /// Strips a leading formula-injection prefix and trims. Blank becomes empty string.
    /// Does not throw — callers decide whether to reject or keep sanitized text.
    /// </summary>
    public static string SanitizeCell(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        if (FormulaPrefixes.Contains(trimmed[0]))
        {
            trimmed = trimmed[1..].TrimStart();
        }

        return trimmed;
    }

    /// <summary>
    /// Sanitizes then rejects when the original value looked like formula injection
    /// and the remaining text is empty (pure formula payload).
    /// </summary>
    public static string SanitizeRequiredCell(string? value, string fieldName)
    {
        var lookedLikeFormula = LooksLikeFormulaInjection(value);
        var sanitized = SanitizeCell(value);
        if (lookedLikeFormula && string.IsNullOrWhiteSpace(sanitized))
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportFormulaInjection,
                $"{fieldName} looks like a spreadsheet formula and was rejected.");
        }

        return sanitized;
    }

    public static string NormalizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportFileInvalid,
                "File name is required.");
        }

        var name = Path.GetFileName(fileName.Trim());
        if (string.IsNullOrWhiteSpace(name) || name.Length > FileNameMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportFileInvalid,
                "File name is invalid.");
        }

        return name;
    }

    public static CatalogImportFileFormat ResolveFormat(string fileName, string? contentType)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var ct = (contentType ?? string.Empty).Trim().ToLowerInvariant();

        if (ext == ".csv"
            || ct is "text/csv" or "application/csv" or "text/plain")
        {
            return CatalogImportFileFormat.Csv;
        }

        if (ext == ".xlsx"
            || ct is "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        {
            return CatalogImportFileFormat.Xlsx;
        }

        throw new DomainException(
            DomainErrorCodes.CatalogImportFileInvalid,
            "Only .csv and .xlsx files are supported.");
    }

    public static void EnsureFileSize(long byteLength)
    {
        if (byteLength <= 0)
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportFileInvalid,
                "Uploaded file is empty.");
        }

        if (byteLength > MaxFileBytes)
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportFileInvalid,
                $"File exceeds the maximum size of {MaxFileBytes / (1024 * 1024)} MB.");
        }
    }

    public static string? NormalizeOptionalError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var trimmed = message.Trim();
        return trimmed.Length <= ErrorMessageMaxLength
            ? trimmed
            : trimmed[..ErrorMessageMaxLength];
    }

    public static string NormalizeIdempotencyKey(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportFileInvalid,
                "Idempotency key cannot be blank when provided.");
        }

        var trimmed = key.Trim();
        if (trimmed.Length > IdempotencyKeyMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportFileInvalid,
                $"Idempotency key exceeds {IdempotencyKeyMaxLength} characters.");
        }

        return trimmed;
    }
}
