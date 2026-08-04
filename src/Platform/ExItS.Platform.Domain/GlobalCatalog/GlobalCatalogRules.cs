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
    public const int SlugMinLength = 2;
    public const int SlugMaxLength = 120;
    public const int DefaultBatchSizeMin = 1;
    public const int DefaultBatchSizeMax = 500;
    public const int DefaultBatchSizeFallback = 50;

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

    /// <summary>Lowercase kebab-case slug from name or explicit slug text.</summary>
    public static string NormalizeSlug(string? slugOrName)
    {
        if (string.IsNullOrWhiteSpace(slugOrName))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogTemplateSlug,
                "Slug cannot be blank.");
        }

        var lowered = slugOrName.Trim().ToLowerInvariant();
        var chars = new char[lowered.Length];
        var len = 0;
        var lastWasHyphen = false;
        foreach (var c in lowered)
        {
            if (char.IsLetterOrDigit(c))
            {
                chars[len++] = c;
                lastWasHyphen = false;
            }
            else if ((c is ' ' or '-' or '_') && len > 0 && !lastWasHyphen)
            {
                chars[len++] = '-';
                lastWasHyphen = true;
            }
        }

        while (len > 0 && chars[len - 1] == '-')
        {
            len--;
        }

        var slug = new string(chars, 0, len);
        if (slug.Length is < SlugMinLength or > SlugMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogTemplateSlug,
                $"Slug must be {SlugMinLength}–{SlugMaxLength} characters.");
        }

        return slug;
    }

    public static int NormalizeDefaultBatchSize(int? batchSize)
    {
        var value = batchSize ?? DefaultBatchSizeFallback;
        if (value is < DefaultBatchSizeMin or > DefaultBatchSizeMax)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogTemplateBatchSize,
                $"DefaultBatchSize must be {DefaultBatchSizeMin}–{DefaultBatchSizeMax}.");
        }

        return value;
    }

    public static SelectionMode NormalizeSelectionMode(SelectionMode mode)
    {
        if (!Enum.IsDefined(mode))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogTemplateSelectionMode,
                $"Unrecognized selection mode '{mode}'.");
        }

        return mode;
    }

    public static BusinessType NormalizePrimaryBusinessType(BusinessType type)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalCatalogBusinessType,
                $"Unrecognized business type '{type}'.");
        }

        return type;
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
