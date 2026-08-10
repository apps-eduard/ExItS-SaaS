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
    public const int BrandMaxLength = 120;
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

    /// <summary>Uppercase + trim; blank/whitespace rejected.</summary>
    public static string NormalizeBarcode(string? barcode) =>
        NormalizeRequiredCode(barcode, BarcodeMaxLength, DomainErrorCodes.InvalidGlobalProductBarcode, "Barcode");

    /// <summary>Uppercase + trim; blank/whitespace rejected.</summary>
    public static string NormalizeSku(string? sku) =>
        NormalizeRequiredCode(sku, SkuMaxLength, DomainErrorCodes.InvalidGlobalProductSku, "SKU");

    /// <summary>Trim collapsed whitespace; blank/whitespace rejected.</summary>
    public static string NormalizeBrand(string? brand)
    {
        if (string.IsNullOrWhiteSpace(brand))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalProductBrand,
                "Brand cannot be blank.");
        }

        var trimmed = System.Text.RegularExpressions.Regex.Replace(brand.Trim(), @"\s+", " ");
        if (trimmed.Length > BrandMaxLength)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalProductBrand,
                $"Brand must be at most {BrandMaxLength} characters.");
        }

        return trimmed;
    }

    /// <summary>Category is required for create/update.</summary>
    public static GlobalCategoryId RequireCategory(GlobalCategoryId? categoryId)
    {
        if (categoryId is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalProductCategory,
                "Category is required.");
        }

        return categoryId;
    }

    /// <summary>
    /// Optional filter/search code: blank becomes null; otherwise same rules as required codes.
    /// </summary>
    public static string? NormalizeOptionalFilterCode(string? value, int maxLength, string errorCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return NormalizeRequiredCode(value, maxLength, errorCode, "Value");
    }

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

    /// <summary>Requires a non-null monetary amount; rejects negative values.</summary>
    public static decimal NormalizeRequiredMoney(decimal? amount, string fieldName)
    {
        if (amount is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalProductMoney,
                $"{fieldName} is required.");
        }

        return NormalizeMoney(amount, fieldName)!.Value;
    }

    /// <summary>Requires both prices and enforces selling &gt;= cost.</summary>
    public static (decimal CostPrice, decimal SellingPrice) NormalizeProductPrices(
        decimal? costPrice,
        decimal? sellingPrice)
    {
        var cost = NormalizeRequiredMoney(costPrice, "CostPrice");
        var selling = NormalizeRequiredMoney(sellingPrice, "SellingPrice");
        if (selling < cost)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalProductPriceRelationship,
                "SellingPrice must be greater than or equal to CostPrice.");
        }

        return (cost, selling);
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

    public static IReadOnlyList<BusinessTypeId> NormalizeBusinessTypeIds(IEnumerable<BusinessTypeId>? ids)
    {
        if (ids is null)
        {
            return Array.Empty<BusinessTypeId>();
        }

        var set = new SortedSet<Guid>();
        var result = new List<BusinessTypeId>();
        foreach (var id in ids)
        {
            if (id is null || id.Value == Guid.Empty)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidGlobalCatalogBusinessType,
                    "Business type id cannot be empty.");
            }

            if (set.Add(id.Value))
            {
                result.Add(id);
            }
        }

        return result.OrderBy(i => i.Value).ToList();
    }

    public static BusinessTypeId NormalizePrimaryBusinessTypeId(BusinessTypeId id)
    {
        if (id is null || id.Value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalCatalogBusinessType,
                "Primary business type is required.");
        }

        return id;
    }

    /// <summary>Stable business-type code: trim, PascalCase alphanumeric (no spaces).</summary>
    public static string NormalizeBusinessTypeCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalCatalogBusinessType,
                "Business type code cannot be blank.");
        }

        var trimmed = code.Trim();
        if (trimmed.Length > 64)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalCatalogBusinessType,
                "Business type code must be at most 64 characters.");
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^[A-Za-z][A-Za-z0-9]*$"))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalCatalogBusinessType,
                "Business type code must be alphanumeric and start with a letter (e.g. SariSari).");
        }

        return trimmed;
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

    private static string NormalizeRequiredCode(string? value, int maxLength, string errorCode, string fieldLabel)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(errorCode, $"{fieldLabel} cannot be blank.");
        }

        var normalized = value.Trim().ToUpperInvariant();
        if (normalized.Length > maxLength)
        {
            throw new DomainException(errorCode, $"{fieldLabel} exceeds maximum length of {maxLength}.");
        }

        // Reject control characters / whitespace inside the code after trim.
        if (normalized.Any(char.IsWhiteSpace))
        {
            throw new DomainException(errorCode, $"{fieldLabel} cannot contain whitespace.");
        }

        return normalized;
    }
}
