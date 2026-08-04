using System.Text;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.UnitTests.GlobalCatalog;

public sealed class CatalogImportRulesTests
{
    [Theory]
    [InlineData("=CMD|'/C calc'!A0", true)]
    [InlineData("+1234+567", true)]
    [InlineData("-2+3", true)]
    [InlineData("@SUM(A1)", true)]
    [InlineData("  =HYPERLINK()", true)]
    [InlineData("Coke", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void LooksLikeFormulaInjection_detects_prefixes(string? value, bool expected)
    {
        Assert.Equal(expected, CatalogImportRules.LooksLikeFormulaInjection(value));
    }

    [Theory]
    [InlineData("=CMD", "CMD")]
    [InlineData("+123", "123")]
    [InlineData("  Soft Drink  ", "Soft Drink")]
    public void SanitizeCell_strips_formula_prefix(string input, string expected)
    {
        Assert.Equal(expected, CatalogImportRules.SanitizeCell(input));
    }

    [Fact]
    public void ResolveFormat_accepts_csv_and_xlsx()
    {
        Assert.Equal(CatalogImportFileFormat.Csv, CatalogImportRules.ResolveFormat("products.csv", "text/csv"));
        Assert.Equal(
            CatalogImportFileFormat.Xlsx,
            CatalogImportRules.ResolveFormat(
                "products.xlsx",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"));
    }

    [Fact]
    public void ResolveFormat_rejects_macros_workbook()
    {
        var ex = Assert.Throws<DomainException>(() =>
            CatalogImportRules.ResolveFormat("evil.xlsm", "application/vnd.ms-excel.sheet.macroEnabled.12"));
        Assert.Equal(DomainErrorCodes.CatalogImportFileInvalid, ex.ErrorCode);
    }

    [Fact]
    public void EnsureFileSize_rejects_oversized()
    {
        var ex = Assert.Throws<DomainException>(() =>
            CatalogImportRules.EnsureFileSize(CatalogImportRules.MaxFileBytes + 1));
        Assert.Equal(DomainErrorCodes.CatalogImportFileInvalid, ex.ErrorCode);
    }
}

public sealed class CatalogImportCsvParserTests
{
    [Fact]
    public void Parse_reads_quoted_commas_and_row_numbers()
    {
        var csv = """
            Name,Unit,Sku,Barcode
            "Soft, Drink",Bottle,SKU-1,480001
            Chips,Pack,SKU-2,480002
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var rows = CatalogImportCsvParser.Parse(stream);
        Assert.Equal(2, rows.Count);
        Assert.Equal(2, rows[0].RowNumber);
        Assert.Equal("Soft, Drink", rows[0].Cells["Name"]);
        Assert.Equal("Bottle", rows[0].Cells["Unit"]);
        Assert.Equal(3, rows[1].RowNumber);
    }
}

public sealed class CatalogImportRowMapperTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task MapRows_marks_formula_injection_as_failed()
    {
        var rows = new List<CatalogImportRawRow>
        {
            new(2, new Dictionary<string, string>
            {
                ["Name"] = "=CMD|'/C calc'!A0",
                ["Unit"] = "Piece"
            })
        };

        var items = await CatalogImportRowMapper.MapRowsAsync(
            rows,
            new FakeCategoryRepository(),
            new FakeProductRepository(),
            T0);

        Assert.Single(items);
        Assert.Equal(CatalogImportItemStatus.Failed, items[0].Status);
        Assert.Equal(DomainErrorCodes.CatalogImportFormulaInjection, items[0].ErrorCode);
    }

    [Fact]
    public async Task MapRows_detects_duplicate_barcode_in_file()
    {
        var rows = new List<CatalogImportRawRow>
        {
            new(2, new Dictionary<string, string>
            {
                ["Name"] = "A",
                ["Unit"] = "Piece",
                ["Barcode"] = "480001"
            }),
            new(3, new Dictionary<string, string>
            {
                ["Name"] = "B",
                ["Unit"] = "Piece",
                ["Barcode"] = "480001"
            })
        };

        var items = await CatalogImportRowMapper.MapRowsAsync(
            rows,
            new FakeCategoryRepository(),
            new FakeProductRepository(),
            T0);

        Assert.Equal(CatalogImportItemStatus.Pending, items[0].Status);
        Assert.Equal(CatalogImportItemStatus.Failed, items[1].Status);
        Assert.Equal(DomainErrorCodes.CatalogImportDuplicateInFile, items[1].ErrorCode);
    }

    [Fact]
    public async Task MapRows_skips_existing_catalog_barcode()
    {
        var products = new FakeProductRepository { ExistingBarcodes = { "480099" } };
        var rows = new List<CatalogImportRawRow>
        {
            new(2, new Dictionary<string, string>
            {
                ["Name"] = "Existing",
                ["Unit"] = "Piece",
                ["Barcode"] = "480099"
            })
        };

        var items = await CatalogImportRowMapper.MapRowsAsync(
            rows,
            new FakeCategoryRepository(),
            products,
            T0);

        Assert.Equal(CatalogImportItemStatus.Skipped, items[0].Status);
        Assert.Equal(ApplicationErrorCodes.DuplicateGlobalProductBarcode, items[0].ErrorCode);
    }
}

public sealed class CatalogImportJobLifecycleTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Confirm_is_idempotent_after_queue_via_status_guard()
    {
        var pending = CatalogImportItem.CreatePending(2, "Coke", "Bottle", barcode: "480001");
        var job = CatalogImportJob.CreateValidated(
            "products.csv",
            CatalogImportFileFormat.Csv,
            128,
            new string('a', 64),
            "platform-user:test",
            [pending],
            T0,
            idempotencyKey: "idem-1");

        job.Confirm(T0.AddMinutes(1));
        Assert.Equal(CatalogImportJobStatus.Queued, job.Status);

        var ex = Assert.Throws<DomainException>(() => job.Confirm(T0.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.InvalidCatalogImportStatusTransition, ex.ErrorCode);
    }

    [Fact]
    public void Item_mark_imported_is_idempotent()
    {
        var item = CatalogImportItem.CreatePending(2, "Coke", "Bottle", barcode: "480001");
        var productId = Guid.NewGuid();
        item.MarkImported(productId, T0);
        item.MarkImported(productId, T0.AddSeconds(1));
        Assert.Equal(CatalogImportItemStatus.Imported, item.Status);
        Assert.Equal(productId, item.CreatedGlobalProductId);
        Assert.Equal(1, item.AttemptCount);
    }

    [Fact]
    public void Job_complete_with_warnings_when_skips_exist()
    {
        var a = CatalogImportItem.CreatePending(2, "A", "Piece", barcode: "1");
        var b = CatalogImportItem.CreatePending(3, "B", "Piece", barcode: "2");
        var job = CatalogImportJob.CreateValidated(
            "products.csv",
            CatalogImportFileFormat.Csv,
            64,
            new string('c', 64),
            "actor",
            [a, b],
            T0);

        job.Confirm(T0.AddMinutes(1));
        job.BeginProcessing(T0.AddMinutes(2));
        a.MarkImported(Guid.NewGuid(), T0.AddMinutes(3));
        b.MarkSkipped(ApplicationErrorCodes.DuplicateGlobalProductSku, "sku exists", T0.AddMinutes(3));
        job.Complete(T0.AddMinutes(4));

        Assert.Equal(CatalogImportJobStatus.CompletedWithWarnings, job.Status);
        Assert.Equal(1, job.ImportedCount);
        Assert.Equal(1, job.SkippedCount);
    }
}

file sealed class FakeCategoryRepository : IGlobalCategoryRepository
{
    public Task AddAsync(GlobalCategory category, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<bool> ExistsWithNameUnderParentAsync(
        string name,
        GlobalCategoryId? parentId,
        GlobalCategoryId? excludingId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<IReadOnlyList<GlobalCategory>> FindByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GlobalCategory>>([]);

    public Task<GlobalCategory?> GetByIdAsync(GlobalCategoryId id, CancellationToken cancellationToken = default) =>
        Task.FromResult<GlobalCategory?>(null);

    public Task<(IReadOnlyList<GlobalCategory> Items, int TotalCount)> ListAsync(
        GlobalCategoryStatus? status,
        GlobalCategoryId? parentId,
        BusinessType? businessType,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<(IReadOnlyList<GlobalCategory>, int)>(([], 0));

    public Task UpdateAsync(GlobalCategory category, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

file sealed class FakeProductRepository : IGlobalProductRepository
{
    public HashSet<string> ExistingBarcodes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> ExistingSkus { get; } = new(StringComparer.OrdinalIgnoreCase);

    public Task AddAsync(GlobalProduct product, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<bool> ExistsWithBarcodeAsync(
        string barcode,
        GlobalProductId? excludingId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ExistingBarcodes.Contains(barcode));

    public Task<bool> ExistsWithSkuAsync(
        string sku,
        GlobalProductId? excludingId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ExistingSkus.Contains(sku));

    public Task<GlobalProduct?> GetByIdAsync(GlobalProductId id, CancellationToken cancellationToken = default) =>
        Task.FromResult<GlobalProduct?>(null);

    public Task<(IReadOnlyList<GlobalProduct> Items, int TotalCount)> ListAsync(
        GlobalProductStatus? status,
        GlobalCategoryId? categoryId,
        BusinessType? businessType,
        string? search,
        string? barcode,
        string? sku,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<(IReadOnlyList<GlobalProduct>, int)>(([], 0));

    public Task UpdateAsync(GlobalProduct product, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
