using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.GlobalCatalog;

/// <summary>
/// Authoritative CSV column contract for Platform global-product imports.
/// Template generation and importer header validation share these constants.
/// </summary>
public static class CatalogImportCsvSchema
{
    public const string DownloadFileName = "exits-global-product-import-template.csv";
    public const string ContentType = "text/csv; charset=utf-8";
    public const char MultiValueSeparator = '|';

    public const string ProductName = "ProductName";
    public const string Category = "Category";
    public const string Description = "Description";
    public const string Brand = "Brand";
    public const string Unit = "Unit";
    public const string Barcode = "Barcode";
    public const string SuggestedSku = "SuggestedSku";
    public const string SellingPrice = "SellingPrice";
    public const string CostPrice = "CostPrice";
    public const string TaxHint = "TaxHint";
    public const string Tags = "Tags";
    public const string BusinessTypes = "BusinessTypes";
    public const string Status = "Status";
    /// <summary>Optional. Omitted or blank ⇒ PerItem. Canonical values: PerItem, ByWeight.</summary>
    public const string SellingMode = "SellingMode";

    /// <summary>Required header names in the exact import order (canonical, without markers).</summary>
    public static readonly IReadOnlyList<string> RequiredColumns =
    [
        ProductName,
        Category,
        Description,
        Brand,
        Unit,
        Barcode,
        SuggestedSku,
        SellingPrice,
        CostPrice,
        TaxHint,
        Tags,
        BusinessTypes,
        Status
    ];

    /// <summary>Optional trailing columns accepted after <see cref="RequiredColumns"/> (order among optionals free).</summary>
    public static readonly IReadOnlyList<string> OptionalColumns =
    [
        SellingMode
    ];

    public static readonly IReadOnlySet<string> OptionalColumnSet =
        new HashSet<string>(OptionalColumns, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Columns whose cell values are required for a successful product import row.
    /// Other columns must still appear as headers but may be blank.
    /// </summary>
    public static readonly IReadOnlySet<string> RequiredValueColumns =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ProductName,
            Category,
            Brand,
            Unit,
            SuggestedSku,
            SellingPrice,
            CostPrice
        };

    public static readonly IReadOnlySet<string> RequiredColumnSet =
        new HashSet<string>(RequiredColumns, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Strips download-template markers such as trailing <c>*</c> or
    /// <c>(required)</c>/<c>(optional)</c> so uploads remain compatible.
    /// </summary>
    public static string NormalizeHeaderName(string? header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return string.Empty;
        }

        var trimmed = header.Trim();
        while (trimmed.EndsWith('*'))
        {
            trimmed = trimmed[..^1].TrimEnd();
        }

        trimmed = Regex.Replace(
            trimmed,
            @"\s*\((required|optional)\)\s*$",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        return trimmed.Trim();
    }

    /// <summary>Download headers: required-value columns marked with a trailing asterisk; optional columns unmarked.</summary>
    public static IReadOnlyList<string> TemplateDownloadHeaders =>
        RequiredColumns
            .Select(c => RequiredValueColumns.Contains(c) ? c + "*" : c)
            .Concat(OptionalColumns)
            .ToList();

    /// <summary>
    /// Validates uploaded headers against <see cref="RequiredColumns"/>.
    /// Required columns must appear first in order. Optional known columns may follow.
    /// Download markers (<c>*</c>, <c>(required)</c>, <c>(optional)</c>) are accepted.
    /// Legacy files with only <see cref="RequiredColumns"/> remain valid (SellingMode defaults to PerItem).
    /// </summary>
    public static void ValidateHeaders(IReadOnlyList<string> headers)
    {
        if (headers is null || headers.Count == 0 || headers.All(string.IsNullOrWhiteSpace))
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportHeadersInvalid,
                "CSV header row is missing or empty.");
        }

        var trimmed = headers
            .Select(h => NormalizeHeaderName(h))
            .ToList();

        var emptyIndexes = trimmed
            .Select((h, i) => (h, i))
            .Where(x => string.IsNullOrWhiteSpace(x.h))
            .Select(x => x.i + 1)
            .ToList();
        if (emptyIndexes.Count > 0)
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportHeadersInvalid,
                $"Blank header name(s) at column position(s): {string.Join(", ", emptyIndexes)}.");
        }

        var duplicates = trimmed
            .GroupBy(h => h, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (duplicates.Count > 0)
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportHeadersInvalid,
                $"Duplicate header(s): {string.Join(", ", duplicates)}.");
        }

        var missing = RequiredColumns
            .Where(required => trimmed.All(h => !string.Equals(h, required, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var unknown = trimmed
            .Where(h => !RequiredColumnSet.Contains(h) && !OptionalColumnSet.Contains(h))
            .ToList();

        if (missing.Count > 0 || unknown.Count > 0)
        {
            var parts = new List<string>();
            if (missing.Count > 0)
            {
                parts.Add($"Missing required column(s): {string.Join(", ", missing)}");
            }

            if (unknown.Count > 0)
            {
                parts.Add($"Unknown column(s): {string.Join(", ", unknown)}");
            }

            parts.Add(
                $"Expected required headers in order: {string.Join(", ", RequiredColumns)}. "
                + $"Optional: {string.Join(", ", OptionalColumns)}.");
            throw new DomainException(
                DomainErrorCodes.CatalogImportHeadersInvalid,
                string.Join(" ", parts));
        }

        if (trimmed.Count < RequiredColumns.Count)
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportHeadersInvalid,
                $"Expected at least {RequiredColumns.Count} columns in order: {string.Join(", ", RequiredColumns)}.");
        }

        for (var i = 0; i < RequiredColumns.Count; i++)
        {
            if (!string.Equals(trimmed[i], RequiredColumns[i], StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainException(
                    DomainErrorCodes.CatalogImportHeadersInvalid,
                    $"Header order mismatch at column {i + 1}: expected '{RequiredColumns[i]}', found '{trimmed[i]}'. "
                    + $"Expected required headers in order: {string.Join(", ", RequiredColumns)}.");
            }
        }

        for (var i = RequiredColumns.Count; i < trimmed.Count; i++)
        {
            if (!OptionalColumnSet.Contains(trimmed[i]))
            {
                throw new DomainException(
                    DomainErrorCodes.CatalogImportHeadersInvalid,
                    $"Unknown optional column '{trimmed[i]}' at position {i + 1}.");
            }
        }
    }

    /// <summary>UTF-8 (with BOM) bytes for the downloadable import template.</summary>
    public static byte[] GenerateTemplateUtf8Bytes()
    {
        var csv = GenerateTemplateCsv();
        var preamble = Encoding.UTF8.GetPreamble();
        var body = Encoding.UTF8.GetBytes(csv);
        var result = new byte[preamble.Length + body.Length];
        Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
        Buffer.BlockCopy(body, 0, result, preamble.Length, body.Length);
        return result;
    }

    /// <summary>
    /// Header row (required-value columns marked with <c>*</c>) plus three sample rows.
    /// Multi-value fields use <see cref="MultiValueSeparator"/>; decimals use invariant culture.
    /// Barcode samples use valid GS1 digits; one sample leaves Barcode blank (optional).
    /// </summary>
    public static string GenerateTemplateCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', TemplateDownloadHeaders.Select(EscapeCsvField)));

        AppendSampleRow(
            sb,
            productName: "SAMPLE Soft Drink 320ml",
            category: "Beverages",
            description: "Carbonated soft drink can — replace or delete this sample row. Barcode is optional GS1 digits (8-14) with check digit; put codes like BAKERY000001 in SuggestedSku.",
            brand: "SampleBrand",
            unit: nameof(ProductUnit.Can),
            barcode: GlobalCatalogBarcodeChecksum.WithCheckDigit("480001000000"),
            sku: "SAMPLE-SD-320",
            sellingPrice: 25.50m,
            costPrice: 18.25m,
            taxHint: "VAT",
            tags: "beverage|cola|sample",
            businessTypes: $"{LegacyBusinessTypeSeeds.SariSariCode}|{LegacyBusinessTypeSeeds.MiniGroceryCode}",
            status: nameof(GlobalProductStatus.Draft),
            sellingMode: nameof(ProductSellingMode.PerItem));

        AppendSampleRow(
            sb,
            productName: "SAMPLE Crackers 10s",
            category: "Snacks",
            description: "Crackers multipack — replace or delete this sample row. Columns marked * in the header require a value.",
            brand: "SampleBrand",
            unit: nameof(ProductUnit.Pack),
            barcode: GlobalCatalogBarcodeChecksum.WithCheckDigit("480001000001"),
            sku: "SAMPLE-CR-10",
            sellingPrice: 12.00m,
            costPrice: 8.50m,
            taxHint: "VAT",
            tags: "snack|sample",
            businessTypes: LegacyBusinessTypeSeeds.SariSariCode,
            status: nameof(GlobalProductStatus.Active),
            sellingMode: nameof(ProductSellingMode.PerItem));

        AppendSampleRow(
            sb,
            productName: "SAMPLE Tomato per kg",
            category: "Produce",
            description: "ByWeight sample — Unit must be Kilogram; SellingPrice is PHP per kilogram. Blank Barcode is valid.",
            brand: "SampleFresh",
            unit: nameof(ProductUnit.Kilogram),
            barcode: string.Empty,
            sku: "VEG-TOMATO-KG",
            sellingPrice: 120.00m,
            costPrice: 80.00m,
            taxHint: "VAT",
            tags: "produce|sample",
            businessTypes: LegacyBusinessTypeSeeds.SariSariCode,
            status: nameof(GlobalProductStatus.Draft),
            sellingMode: nameof(ProductSellingMode.ByWeight));

        return sb.ToString();
    }

    private static void AppendSampleRow(
        StringBuilder sb,
        string productName,
        string category,
        string description,
        string brand,
        string unit,
        string barcode,
        string sku,
        decimal sellingPrice,
        decimal costPrice,
        string taxHint,
        string tags,
        string businessTypes,
        string status,
        string sellingMode)
    {
        var fields = new[]
        {
            productName,
            category,
            description,
            brand,
            unit,
            barcode,
            sku,
            sellingPrice.ToString("0.00", CultureInfo.InvariantCulture),
            costPrice.ToString("0.00", CultureInfo.InvariantCulture),
            taxHint,
            tags,
            businessTypes,
            status,
            sellingMode
        };
        sb.AppendLine(string.Join(',', fields.Select(EscapeCsvField)));
    }

    public static string EscapeCsvField(string? value)
    {
        var text = value ?? string.Empty;
        if (text.Contains('"') || text.Contains(',') || text.Contains('\r') || text.Contains('\n'))
        {
            return "\"" + text.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        return text;
    }
}
