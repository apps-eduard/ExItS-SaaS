using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.GlobalCatalog;

/// <summary>Normalization and validation helpers for global merchandise catalog fields.</summary>
public static class GlobalCatalogRules
{
    public const int NameMinLength = 1;
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int BarcodeMaxLength = 64;
    public const int SkuMaxLength = 64;
    public const int IconReferenceMaxLength = 512;
    public const int ImageReferenceMaxLength = 512;
    public const int SearchTagMaxLength = 64;
    public const int SearchTagMaxCount = 32;

    public static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalCatalogName,
                "Name cannot be blank.");
        }

        var trimmed = System.Text.RegularExpressions.Regex.Replace(name.Trim(), @"\s+", " ");
        if (trimmed.Length is < NameMinLength or > NameMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalCatalogName,
                $"Name must be {NameMinLength}–{NameMaxLength} characters.");
        }

        return trimmed;
    }

    /// <summary>Uppercase + trim; blank becomes null.</summary>
    public static string? NormalizeBarcode(string? barcode) =>
        NormalizeOptionalCode(barcode, BarcodeMaxLength, DomainErrorCodes.InvalidGlobalProductBarcode);

    /// <summary>Uppercase + trim; blank becomes null.</summary>
    public static string? NormalizeSku(string? sku) =>
        NormalizeOptionalCode(sku, SkuMaxLength, DomainErrorCodes.InvalidGlobalProductSku);

    public static string? NormalizeOptionalText(string? value, int maxLength, string errorCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new DomainException(errorCode, $"Value exceeds maximum length of {maxLength}.");
        }

        return trimmed;
    }

    public static decimal? NormalizeMoney(decimal? amount, string fieldName)
    {
        if (amount is null)
        {
            return null;
        }

        if (amount.Value < 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalProductMoney,
                $"{fieldName} cannot be negative.");
        }

        return Math.Round(amount.Value, 2, MidpointRounding.AwayFromZero);
    }

    public static IReadOnlyList<string> NormalizeSearchTags(IEnumerable<string>? tags)
    {
        if (tags is null)
        {
            return Array.Empty<string>();
        }

        var normalized = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            var trimmed = tag.Trim();
            if (trimmed.Length > SearchTagMaxLength)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidGlobalProductSearchTag,
                    $"Search tag exceeds maximum length of {SearchTagMaxLength}.");
            }

            if (!seen.Add(trimmed))
            {
                continue;
            }

            normalized.Add(trimmed);
            if (normalized.Count > SearchTagMaxCount)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidGlobalProductSearchTag,
                    $"At most {SearchTagMaxCount} search tags are allowed.");
            }
        }

        return normalized;
    }

    public static IReadOnlyList<BusinessType> NormalizeBusinessTypes(IEnumerable<BusinessType>? types)
    {
        if (types is null)
        {
            return Array.Empty<BusinessType>();
        }

        var set = new SortedSet<BusinessType>();
        foreach (var type in types)
        {
            if (!Enum.IsDefined(type))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidGlobalCatalogBusinessType,
                    $"Unrecognized business type '{type}'.");
            }

            set.Add(type);
        }

        return set.ToList();
    }

    private static string? NormalizeOptionalCode(string? value, int maxLength, string errorCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length > maxLength)
        {
            throw new DomainException(errorCode, $"Value exceeds maximum length of {maxLength}.");
        }

        // Reject control characters / whitespace inside the code after trim.
        if (normalized.Any(char.IsWhiteSpace))
        {
            throw new DomainException(errorCode, "Value cannot contain whitespace.");
        }

        return normalized;
    }
}
