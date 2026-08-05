using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.Application.GlobalCatalog;

/// <summary>Maps sanitized file rows into domain import items with duplicate detection.</summary>
public static class CatalogImportRowMapper
{
    /// <summary>Reserved tag prefix storing intended <see cref="GlobalProductStatus"/> until apply.</summary>
    public const string ImportStatusTagPrefix = "import.status:";

    public const string BrandTagPrefix = "brand:";
    public const string TaxHintTagPrefix = "tax:";

    // Authoritative schema names first; legacy aliases retained for programmatic/unit-test rows.
    private static readonly string[] NameKeys = ["productname", "name", "product_name"];
    private static readonly string[] UnitKeys = ["unit", "productunit", "product_unit"];
    private static readonly string[] DescriptionKeys = ["description", "desc"];
    private static readonly string[] BrandKeys = ["brand"];
    private static readonly string[] SkuKeys = ["suggestedsku", "sku"];
    private static readonly string[] BarcodeKeys = ["barcode", "ean", "upc"];
    private static readonly string[] CategoryIdKeys = ["categoryid", "globalcategoryid", "category_id"];
    private static readonly string[] CategoryNameKeys = ["category", "categoryname", "category_name"];
    private static readonly string[] PriceKeys =
        ["suggestedsellingprice", "suggestedprice", "price", "suggested_price"];
    private static readonly string[] CostKeys =
        ["suggestedcostprice", "suggestedcost", "cost", "suggested_cost"];
    private static readonly string[] ImageKeys = ["imagereference", "image", "image_reference", "imageurl"];
    private static readonly string[] TaxHintKeys = ["taxhint", "tax_hint"];
    private static readonly string[] TagsKeys = ["tags", "searchtags", "search_tags"];
    private static readonly string[] BusinessTypeKeys = ["businesstypes", "businesstype", "business_types"];
    private static readonly string[] StatusKeys = ["status"];

    public static async Task<IReadOnlyList<CatalogImportItem>> MapRowsAsync(
        IReadOnlyList<CatalogImportRawRow> rows,
        IGlobalCategoryRepository categories,
        IGlobalProductRepository products,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var items = new List<CatalogImportItem>(rows.Count);
        var barcodesInFile = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var skusInFile = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            items.Add(await MapOneAsync(
                    row,
                    categories,
                    products,
                    barcodesInFile,
                    skusInFile,
                    utcNow,
                    cancellationToken)
                .ConfigureAwait(false));
        }

        return items;
    }

    private static async Task<CatalogImportItem> MapOneAsync(
        CatalogImportRawRow row,
        IGlobalCategoryRepository categories,
        IGlobalProductRepository products,
        Dictionary<string, int> barcodesInFile,
        Dictionary<string, int> skusInFile,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        var cells = NormalizeKeys(row.Cells);
        var rawName = Get(cells, NameKeys);
        var rawUnit = Get(cells, UnitKeys);
        var rawDescription = Get(cells, DescriptionKeys);
        var rawBrand = Get(cells, BrandKeys);
        var rawSku = Get(cells, SkuKeys);
        var rawBarcode = Get(cells, BarcodeKeys);
        var rawCategoryId = Get(cells, CategoryIdKeys);
        var rawCategoryName = Get(cells, CategoryNameKeys);
        var rawPrice = Get(cells, PriceKeys);
        var rawCost = Get(cells, CostKeys);
        var rawImage = Get(cells, ImageKeys);
        var rawTaxHint = Get(cells, TaxHintKeys);
        var rawTags = Get(cells, TagsKeys);
        var rawBusinessTypes = Get(cells, BusinessTypeKeys);
        var rawStatus = Get(cells, StatusKeys);

        try
        {
            if (CatalogImportRules.LooksLikeFormulaInjection(rawName)
                || CatalogImportRules.LooksLikeFormulaInjection(rawSku)
                || CatalogImportRules.LooksLikeFormulaInjection(rawBarcode)
                || CatalogImportRules.LooksLikeFormulaInjection(rawDescription)
                || CatalogImportRules.LooksLikeFormulaInjection(rawBrand)
                || CatalogImportRules.LooksLikeFormulaInjection(rawCategoryName)
                || CatalogImportRules.LooksLikeFormulaInjection(rawImage)
                || CatalogImportRules.LooksLikeFormulaInjection(rawTaxHint)
                || CatalogImportRules.LooksLikeFormulaInjection(rawTags)
                || CatalogImportRules.LooksLikeFormulaInjection(rawBusinessTypes)
                || CatalogImportRules.LooksLikeFormulaInjection(rawUnit)
                || CatalogImportRules.LooksLikeFormulaInjection(rawPrice)
                || CatalogImportRules.LooksLikeFormulaInjection(rawCost)
                || CatalogImportRules.LooksLikeFormulaInjection(rawStatus)
                || CatalogImportRules.LooksLikeFormulaInjection(rawCategoryId))
            {
                // Sanitize for storage, but mark failed so formula payloads are never imported silently.
                return CatalogImportItem.CreateFailed(
                    row.RowNumber,
                    CatalogImportRules.SanitizeCell(rawName),
                    CatalogImportRules.SanitizeCell(rawUnit),
                    DomainErrorCodes.CatalogImportFormulaInjection,
                    "One or more cells look like spreadsheet formulas (=, +, -, @).",
                    CatalogImportRules.SanitizeCell(rawDescription),
                    NullIfEmpty(CatalogImportRules.SanitizeCell(rawSku)),
                    NullIfEmpty(CatalogImportRules.SanitizeCell(rawBarcode)),
                    categoryName: NullIfEmpty(CatalogImportRules.SanitizeCell(rawCategoryName)),
                    imageReference: NullIfEmpty(CatalogImportRules.SanitizeCell(rawImage)),
                    searchTagsRaw: NullIfEmpty(CatalogImportRules.SanitizeCell(rawTags)),
                    businessTypesRaw: NullIfEmpty(CatalogImportRules.SanitizeCell(rawBusinessTypes)));
            }

            var name = GlobalCatalogRules.NormalizeName(CatalogImportRules.SanitizeCell(rawName));
            var unitText = CatalogImportRules.SanitizeCell(rawUnit);
            if (string.IsNullOrWhiteSpace(unitText)
                || !Enum.TryParse<ProductUnit>(unitText, ignoreCase: true, out var unit))
            {
                return CatalogImportItem.CreateFailed(
                    row.RowNumber,
                    name,
                    unitText,
                    DomainErrorCodes.InvalidGlobalProductUnit,
                    $"Unrecognized or missing product unit '{unitText}'.");
            }

            var description = NullIfEmpty(CatalogImportRules.SanitizeCell(rawDescription));
            var sku = GlobalCatalogRules.NormalizeSku(CatalogImportRules.SanitizeCell(rawSku));
            var barcode = GlobalCatalogRules.NormalizeBarcode(CatalogImportRules.SanitizeCell(rawBarcode));
            var image = GlobalCatalogRules.NormalizeOptionalText(
                CatalogImportRules.SanitizeCell(rawImage),
                GlobalCatalogRules.ImageReferenceMaxLength,
                DomainErrorCodes.InvalidGlobalProductImage);
            var price = ParseMoney(
                CatalogImportRules.SanitizeCell(rawPrice),
                CatalogImportCsvSchema.SuggestedSellingPrice);
            var cost = ParseMoney(
                CatalogImportRules.SanitizeCell(rawCost),
                CatalogImportCsvSchema.SuggestedCostPrice);
            description = GlobalCatalogRules.NormalizeOptionalText(
                description,
                GlobalCatalogRules.DescriptionMaxLength,
                DomainErrorCodes.InvalidGlobalProductDescription);

            var brand = NullIfEmpty(CatalogImportRules.SanitizeCell(rawBrand));
            var taxHint = NullIfEmpty(CatalogImportRules.SanitizeCell(rawTaxHint));

            GlobalProductStatus productStatus = GlobalProductStatus.Draft;
            var statusText = CatalogImportRules.SanitizeCell(rawStatus);
            if (!string.IsNullOrWhiteSpace(statusText))
            {
                if (!Enum.TryParse(statusText, ignoreCase: true, out productStatus)
                    || !Enum.IsDefined(productStatus))
                {
                    return CatalogImportItem.CreateFailed(
                        row.RowNumber,
                        name,
                        unit.ToString(),
                        DomainErrorCodes.InvalidGlobalProductStatus,
                        $"Unrecognized product status '{statusText}'. Use Draft, Active, or Archived.");
                }
            }

            Guid? categoryId = null;
            var categoryName = NullIfEmpty(CatalogImportRules.SanitizeCell(rawCategoryName));
            var categoryIdText = CatalogImportRules.SanitizeCell(rawCategoryId);
            if (!string.IsNullOrWhiteSpace(categoryIdText))
            {
                if (!Guid.TryParse(categoryIdText, out var parsedId) || parsedId == Guid.Empty)
                {
                    return CatalogImportItem.CreateFailed(
                        row.RowNumber,
                        name,
                        unit.ToString(),
                        DomainErrorCodes.InvalidGlobalCategoryId,
                        $"Invalid category id '{categoryIdText}'.",
                        description,
                        sku,
                        barcode,
                        categoryName: categoryName,
                        suggestedPrice: price,
                        suggestedCost: cost,
                        imageReference: image,
                        searchTagsRaw: NullIfEmpty(CatalogImportRules.SanitizeCell(rawTags)),
                        businessTypesRaw: NullIfEmpty(CatalogImportRules.SanitizeCell(rawBusinessTypes)));
                }

                var category = await categories
                    .GetByIdAsync(GlobalCategoryId.From(parsedId), cancellationToken)
                    .ConfigureAwait(false);
                if (category is null)
                {
                    return CatalogImportItem.CreateFailed(
                        row.RowNumber,
                        name,
                        unit.ToString(),
                        ApplicationErrorCodes.GlobalCategoryNotFound,
                        "Category was not found.",
                        description,
                        sku,
                        barcode,
                        categoryName: categoryName,
                        suggestedPrice: price,
                        suggestedCost: cost,
                        imageReference: image,
                        searchTagsRaw: NullIfEmpty(CatalogImportRules.SanitizeCell(rawTags)),
                        businessTypesRaw: NullIfEmpty(CatalogImportRules.SanitizeCell(rawBusinessTypes)));
                }

                categoryId = category.Id.Value;
                categoryName ??= category.Name;
            }
            else if (!string.IsNullOrWhiteSpace(categoryName))
            {
                var matches = await categories
                    .FindByNormalizedNameAsync(categoryName.ToUpperInvariant(), cancellationToken)
                    .ConfigureAwait(false);
                if (matches.Count == 0)
                {
                    return CatalogImportItem.CreateFailed(
                        row.RowNumber,
                        name,
                        unit.ToString(),
                        ApplicationErrorCodes.GlobalCategoryNotFound,
                        $"Unknown category '{categoryName}'.",
                        description,
                        sku,
                        barcode,
                        categoryName: categoryName,
                        suggestedPrice: price,
                        suggestedCost: cost,
                        imageReference: image,
                        searchTagsRaw: NullIfEmpty(CatalogImportRules.SanitizeCell(rawTags)),
                        businessTypesRaw: NullIfEmpty(CatalogImportRules.SanitizeCell(rawBusinessTypes)));
                }

                if (matches.Count > 1)
                {
                    return CatalogImportItem.CreateFailed(
                        row.RowNumber,
                        name,
                        unit.ToString(),
                        DomainErrorCodes.CatalogImportRowInvalid,
                        $"Category name '{categoryName}' is ambiguous; use CategoryId.",
                        description,
                        sku,
                        barcode,
                        categoryName: categoryName,
                        suggestedPrice: price,
                        suggestedCost: cost,
                        imageReference: image,
                        searchTagsRaw: NullIfEmpty(CatalogImportRules.SanitizeCell(rawTags)),
                        businessTypesRaw: NullIfEmpty(CatalogImportRules.SanitizeCell(rawBusinessTypes)));
                }

                categoryId = matches[0].Id.Value;
            }

            var tagsRaw = ComposeTagsRaw(
                NullIfEmpty(CatalogImportRules.SanitizeCell(rawTags)),
                brand,
                taxHint,
                productStatus);
            var businessTypesRaw = NullIfEmpty(CatalogImportRules.SanitizeCell(rawBusinessTypes));
            _ = GlobalCatalogRules.NormalizeSearchTags(SplitList(tagsRaw));
            _ = ParseBusinessTypes(businessTypesRaw);

            if (barcode is not null)
            {
                if (barcodesInFile.TryGetValue(barcode, out var priorRow))
                {
                    return CatalogImportItem.CreateFailed(
                        row.RowNumber,
                        name,
                        unit.ToString(),
                        DomainErrorCodes.CatalogImportDuplicateInFile,
                        $"Duplicate barcode '{barcode}' also appears on row {priorRow}.",
                        description,
                        sku,
                        barcode,
                        categoryId,
                        categoryName,
                        price,
                        cost,
                        image,
                        tagsRaw,
                        businessTypesRaw);
                }

                barcodesInFile[barcode] = row.RowNumber;

                if (await products.ExistsWithBarcodeAsync(barcode, excludingId: null, cancellationToken)
                        .ConfigureAwait(false))
                {
                    return CatalogImportItem.CreateSkipped(
                        row.RowNumber,
                        name,
                        unit.ToString(),
                        ApplicationErrorCodes.DuplicateGlobalProductBarcode,
                        $"Barcode '{barcode}' already exists in the global catalog.",
                        utcNow,
                        description,
                        sku,
                        barcode,
                        categoryId,
                        categoryName,
                        price,
                        cost,
                        image,
                        tagsRaw,
                        businessTypesRaw);
                }
            }

            if (sku is not null)
            {
                if (skusInFile.TryGetValue(sku, out var priorRow))
                {
                    return CatalogImportItem.CreateFailed(
                        row.RowNumber,
                        name,
                        unit.ToString(),
                        DomainErrorCodes.CatalogImportDuplicateInFile,
                        $"Duplicate SKU '{sku}' also appears on row {priorRow}.",
                        description,
                        sku,
                        barcode,
                        categoryId,
                        categoryName,
                        price,
                        cost,
                        image,
                        tagsRaw,
                        businessTypesRaw);
                }

                skusInFile[sku] = row.RowNumber;

                if (await products.ExistsWithSkuAsync(sku, excludingId: null, cancellationToken)
                        .ConfigureAwait(false))
                {
                    return CatalogImportItem.CreateSkipped(
                        row.RowNumber,
                        name,
                        unit.ToString(),
                        ApplicationErrorCodes.DuplicateGlobalProductSku,
                        $"SKU '{sku}' already exists in the global catalog.",
                        utcNow,
                        description,
                        sku,
                        barcode,
                        categoryId,
                        categoryName,
                        price,
                        cost,
                        image,
                        tagsRaw,
                        businessTypesRaw);
                }
            }

            return CatalogImportItem.CreatePending(
                row.RowNumber,
                name,
                unit.ToString(),
                description,
                sku,
                barcode,
                categoryId,
                categoryName,
                price,
                cost,
                image,
                tagsRaw,
                businessTypesRaw);
        }
        catch (DomainException ex)
        {
            return CatalogImportItem.CreateFailed(
                row.RowNumber,
                CatalogImportRules.SanitizeCell(rawName),
                CatalogImportRules.SanitizeCell(rawUnit),
                ex.ErrorCode,
                ex.Message,
                CatalogImportRules.SanitizeCell(rawDescription),
                NullIfEmpty(CatalogImportRules.SanitizeCell(rawSku)),
                NullIfEmpty(CatalogImportRules.SanitizeCell(rawBarcode)),
                categoryName: NullIfEmpty(CatalogImportRules.SanitizeCell(rawCategoryName)),
                imageReference: NullIfEmpty(CatalogImportRules.SanitizeCell(rawImage)),
                searchTagsRaw: NullIfEmpty(CatalogImportRules.SanitizeCell(rawTags)),
                businessTypesRaw: NullIfEmpty(CatalogImportRules.SanitizeCell(rawBusinessTypes)));
        }
    }

    public static string ComputeSha256Hex(ReadOnlySpan<byte> bytes)
    {
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static IReadOnlyList<string> SplitList(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        // Prefer '|' (documented template separator); also accept ';' for compatibility.
        return raw.Split(
            [CatalogImportCsvSchema.MultiValueSeparator, ';'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public static (IReadOnlyList<string> Tags, GlobalProductStatus Status) SplitTagsAndStatus(string? searchTagsRaw)
    {
        var parts = SplitList(searchTagsRaw);
        var status = GlobalProductStatus.Draft;
        var tags = new List<string>(parts.Count);
        foreach (var part in parts)
        {
            if (part.StartsWith(ImportStatusTagPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var value = part[ImportStatusTagPrefix.Length..];
                if (Enum.TryParse(value, ignoreCase: true, out GlobalProductStatus parsed)
                    && Enum.IsDefined(parsed))
                {
                    status = parsed;
                }

                continue;
            }

            tags.Add(part);
        }

        return (tags, status);
    }

    public static IReadOnlyList<BusinessType> ParseBusinessTypes(string? raw)
    {
        var parts = SplitList(raw);
        if (parts.Count == 0)
        {
            return Array.Empty<BusinessType>();
        }

        var parsed = new List<BusinessType>(parts.Count);
        foreach (var part in parts)
        {
            if (!Enum.TryParse<BusinessType>(part, ignoreCase: true, out var type)
                || !Enum.IsDefined(type))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidGlobalCatalogBusinessType,
                    $"Unrecognized business type '{part}'.");
            }

            parsed.Add(type);
        }

        return GlobalCatalogRules.NormalizeBusinessTypes(parsed);
    }

    private static string? ComposeTagsRaw(
        string? tags,
        string? brand,
        string? taxHint,
        GlobalProductStatus status)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(tags))
        {
            parts.AddRange(SplitList(tags));
        }

        if (!string.IsNullOrWhiteSpace(brand))
        {
            parts.Add(BrandTagPrefix + brand);
        }

        if (!string.IsNullOrWhiteSpace(taxHint))
        {
            parts.Add(TaxHintTagPrefix + taxHint);
        }

        if (status != GlobalProductStatus.Draft)
        {
            parts.Add(ImportStatusTagPrefix + status);
        }

        return parts.Count == 0
            ? null
            : string.Join(CatalogImportCsvSchema.MultiValueSeparator, parts);
    }

    private static decimal? ParseMoney(string? raw, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalProductMoney,
                $"{fieldName} '{raw}' is not a valid invariant decimal (example: 25.50).");
        }

        return GlobalCatalogRules.NormalizeMoney(amount, fieldName);
    }

    private static Dictionary<string, string> NormalizeKeys(IReadOnlyDictionary<string, string> cells)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in cells)
        {
            var key = pair.Key.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
            map[key] = pair.Value;
        }

        return map;
    }

    private static string? Get(Dictionary<string, string> cells, string[] keys)
    {
        foreach (var key in keys)
        {
            if (cells.TryGetValue(key, out var value))
            {
                return value;
            }
        }

        return null;
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}

/// <summary>Safe CSV reader — no formula execution; quoted fields supported.</summary>
public static class CatalogImportCsvParser
{
    public static IReadOnlyList<CatalogImportRawRow> Parse(Stream content)
    {
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var headerLine = reader.ReadLine();
        if (string.IsNullOrWhiteSpace(headerLine))
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportFileInvalid,
                "CSV header row is missing.");
        }

        var headers = SplitCsvLine(headerLine);
        if (headers.Count == 0 || headers.All(string.IsNullOrWhiteSpace))
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportFileInvalid,
                "CSV header row is empty.");
        }

        CatalogImportCsvSchema.ValidateHeaders(headers);

        var rows = new List<CatalogImportRawRow>();
        var rowNumber = 1; // header is row 1 in spreadsheet terms; data starts at 2
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            rowNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = SplitCsvLine(line);
            var cells = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
            {
                var header = headers[i].Trim();
                if (string.IsNullOrWhiteSpace(header))
                {
                    continue;
                }

                // Preserve authoritative casing from schema when matched.
                var canonical = CatalogImportCsvSchema.RequiredColumns.FirstOrDefault(c =>
                    string.Equals(c, header, StringComparison.OrdinalIgnoreCase)) ?? header;
                cells[canonical] = i < fields.Count ? fields[i] : string.Empty;
            }

            rows.Add(new CatalogImportRawRow(rowNumber, cells));
            if (rows.Count > CatalogImportRules.MaxRows)
            {
                throw new DomainException(
                    DomainErrorCodes.CatalogImportFileInvalid,
                    $"CSV exceeds the maximum of {CatalogImportRules.MaxRows} data rows.");
            }
        }

        return rows;
    }

    public static IReadOnlyList<string> SplitCsvLine(string line)
    {
        var fields = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(sb.ToString());
                sb.Clear();
            }
            else
            {
                sb.Append(c);
            }
        }

        fields.Add(sb.ToString());
        return fields;
    }
}
