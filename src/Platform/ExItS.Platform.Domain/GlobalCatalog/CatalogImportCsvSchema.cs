using System.Globalization;
using System.Text;
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

    /// <summary>Required header names in the exact import order.</summary>
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

    public static readonly IReadOnlySet<string> RequiredColumnSet =
        new HashSet<string>(RequiredColumns, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Validates uploaded headers against <see cref="RequiredColumns"/>.
    /// Fails on missing, unknown, duplicate, or out-of-order headers.
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
            .Select(h => (h ?? string.Empty).Trim())
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
            .Where(h => !RequiredColumnSet.Contains(h))
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
                $"Expected headers in order: {string.Join(", ", RequiredColumns)}.");
            throw new DomainException(
                DomainErrorCodes.CatalogImportHeadersInvalid,
                string.Join(" ", parts));
        }

        if (trimmed.Count != RequiredColumns.Count)
        {
            throw new DomainException(
                DomainErrorCodes.CatalogImportHeadersInvalid,
                $"Expected exactly {RequiredColumns.Count} columns in order: {string.Join(", ", RequiredColumns)}.");
        }

        for (var i = 0; i < RequiredColumns.Count; i++)
        {
            if (!string.Equals(trimmed[i], RequiredColumns[i], StringComparison.OrdinalIgnoreCase))
            {
                throw new DomainException(
                    DomainErrorCodes.CatalogImportHeadersInvalid,
                    $"Header order mismatch at column {i + 1}: expected '{RequiredColumns[i]}', found '{trimmed[i]}'. "
                    + $"Expected headers in order: {string.Join(", ", RequiredColumns)}.");
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
    /// Header row plus three clearly marked sample rows.
    /// Multi-value fields use <see cref="MultiValueSeparator"/>; decimals use invariant culture.
    /// </summary>
    public static string GenerateTemplateCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(',', RequiredColumns.Select(EscapeCsvField)));

        AppendSampleRow(
            sb,
            productName: "SAMPLE Soft Drink 320ml",
            category: "Beverages",
            description: "Carbonated soft drink can — replace or delete this sample row",
            brand: "SampleBrand",
            unit: nameof(ProductUnit.Can),
            barcode: GlobalCatalogBarcodeChecksum.WithCheckDigit("480001000000"),
            sku: "SAMPLE-SD-320",
            sellingPrice: 25.50m,
            costPrice: 18.25m,
            taxHint: "VAT",
            tags: "beverage|cola|sample",
            businessTypes: $"{LegacyBusinessTypeSeeds.SariSariCode}|{LegacyBusinessTypeSeeds.MiniGroceryCode}",
            status: nameof(GlobalProductStatus.Draft));

        AppendSampleRow(
            sb,
            productName: "SAMPLE Crackers 10s",
            category: "Snacks",
            description: "Crackers multipack — replace or delete this sample row",
            brand: "SampleBrand",
            unit: nameof(ProductUnit.Pack),
            barcode: GlobalCatalogBarcodeChecksum.WithCheckDigit("480001000001"),
            sku: "SAMPLE-CR-10",
            sellingPrice: 12.00m,
            costPrice: 8.50m,
            taxHint: "VAT",
            tags: "snack|sample",
            businessTypes: LegacyBusinessTypeSeeds.SariSariCode,
            status: nameof(GlobalProductStatus.Active));

        AppendSampleRow(
            sb,
            productName: "SAMPLE Pandesal Pack",
            category: "Bakery",
            description: "Fresh bread pack — replace or delete this sample row",
            brand: "SampleBakery",
            unit: nameof(ProductUnit.Pack),
            barcode: GlobalCatalogBarcodeChecksum.WithCheckDigit("480001000002"),
            sku: "SAMPLE-PS-12",
            sellingPrice: 35.75m,
            costPrice: 22.00m,
            taxHint: "VAT-EXEMPT",
            tags: "bakery|bread|sample",
            businessTypes: $"{LegacyBusinessTypeSeeds.BakeryCode}|{LegacyBusinessTypeSeeds.CafeCode}",
            status: nameof(GlobalProductStatus.Draft));

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
        string status)
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
            status
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
