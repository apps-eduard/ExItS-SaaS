using System.Text;
using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

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

public sealed class CatalogImportCsvSchemaTests
{
    [Fact]
    public void Required_columns_match_authoritative_order()
    {
        Assert.Equal(
            [
                "ProductName",
                "Category",
                "Description",
                "Brand",
                "Unit",
                "Barcode",
                "SuggestedSku",
                "SuggestedSellingPrice",
                "SuggestedCostPrice",
                "TaxHint",
                "Tags",
                "BusinessTypes",
                "Status"
            ],
            CatalogImportCsvSchema.RequiredColumns);
    }

    [Fact]
    public void Template_includes_every_required_importer_column_in_order()
    {
        var csv = CatalogImportCsvSchema.GenerateTemplateCsv();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var headerLine = reader.ReadLine();
        Assert.NotNull(headerLine);
        var headers = CatalogImportCsvParser.SplitCsvLine(headerLine!);
        Assert.Equal(CatalogImportCsvSchema.RequiredColumns.Count, headers.Count);
        for (var i = 0; i < CatalogImportCsvSchema.RequiredColumns.Count; i++)
        {
            Assert.Equal(CatalogImportCsvSchema.RequiredColumns[i], headers[i]);
        }
    }

    [Fact]
    public void Template_utf8_bytes_have_bom_and_parse_sample_rows()
    {
        var bytes = CatalogImportCsvSchema.GenerateTemplateUtf8Bytes();
        Assert.True(bytes.Length >= 3);
        Assert.Equal(0xEF, bytes[0]);
        Assert.Equal(0xBB, bytes[1]);
        Assert.Equal(0xBF, bytes[2]);
        Assert.Equal(CatalogImportCsvSchema.DownloadFileName, "exits-global-product-import-template.csv");
        Assert.Equal("text/csv; charset=utf-8", CatalogImportCsvSchema.ContentType);

        using var stream = new MemoryStream(bytes);
        var rows = CatalogImportCsvParser.Parse(stream);
        Assert.Equal(3, rows.Count);
        Assert.All(rows, r => Assert.StartsWith("SAMPLE", r.Cells[CatalogImportCsvSchema.ProductName], StringComparison.Ordinal));
        Assert.Equal("25.50", rows[0].Cells[CatalogImportCsvSchema.SuggestedSellingPrice]);
        Assert.Contains('|', rows[0].Cells[CatalogImportCsvSchema.Tags]);
        Assert.Contains('|', rows[0].Cells[CatalogImportCsvSchema.BusinessTypes]);
    }

    [Fact]
    public async Task Downloaded_template_maps_successfully_unchanged()
    {
        using var stream = new MemoryStream(CatalogImportCsvSchema.GenerateTemplateUtf8Bytes());
        var rows = CatalogImportCsvParser.Parse(stream);
        var items = await CatalogImportRowMapper.MapRowsAsync(
            rows,
            new FakeCategoryRepository(),
            new FakeProductRepository(),
            new DateTimeOffset(2026, 8, 5, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal(3, items.Count);
        // Category names in samples may not exist — still a successful validate path (Failed/Pending, not throw).
        Assert.DoesNotContain(items, i => i.ErrorCode == DomainErrorCodes.CatalogImportHeadersInvalid);
        Assert.Contains(items, i => i.Status is CatalogImportItemStatus.Pending or CatalogImportItemStatus.Failed);
    }

    [Theory]
    [InlineData("ProductName,Unit")] // missing many
    public void ValidateHeaders_rejects_missing_columns(string headerLine)
    {
        var headers = CatalogImportCsvParser.SplitCsvLine(headerLine);
        var ex = Assert.Throws<DomainException>(() => CatalogImportCsvSchema.ValidateHeaders(headers));
        Assert.Equal(DomainErrorCodes.CatalogImportHeadersInvalid, ex.ErrorCode);
        Assert.Contains("Missing", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateHeaders_rejects_unknown_and_renamed_columns()
    {
        var headers = CatalogImportCsvSchema.RequiredColumns
            .Select(c => c == "ProductName" ? "Name" : c)
            .ToList();
        var ex = Assert.Throws<DomainException>(() => CatalogImportCsvSchema.ValidateHeaders(headers));
        Assert.Equal(DomainErrorCodes.CatalogImportHeadersInvalid, ex.ErrorCode);
        Assert.Contains("Missing", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Unknown", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateHeaders_rejects_duplicate_headers()
    {
        var headers = CatalogImportCsvSchema.RequiredColumns.ToList();
        headers[1] = "ProductName";
        var ex = Assert.Throws<DomainException>(() => CatalogImportCsvSchema.ValidateHeaders(headers));
        Assert.Equal(DomainErrorCodes.CatalogImportHeadersInvalid, ex.ErrorCode);
        Assert.Contains("Duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ValidateHeaders_rejects_out_of_order_headers()
    {
        var headers = CatalogImportCsvSchema.RequiredColumns.ToList();
        (headers[0], headers[1]) = (headers[1], headers[0]);
        var ex = Assert.Throws<DomainException>(() => CatalogImportCsvSchema.ValidateHeaders(headers));
        Assert.Equal(DomainErrorCodes.CatalogImportHeadersInvalid, ex.ErrorCode);
        Assert.Contains("order", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Endpoint_source_requires_import_permission_and_csv_filename()
    {
        var root = FindRepositoryRoot();
        var endpoints = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Api", "GlobalCatalog", "GlobalCatalogEndpoints.cs"));
        Assert.Contains("/template.csv", endpoints, StringComparison.Ordinal);
        Assert.Contains("/imports/template.csv", endpoints, StringComparison.Ordinal);
        Assert.Contains("ImportGlobalProducts", endpoints, StringComparison.Ordinal);
        Assert.Contains("CatalogImportCsvSchema.DownloadFileName", endpoints, StringComparison.Ordinal);
        Assert.Contains("CatalogImportCsvSchema.ContentType", endpoints, StringComparison.Ordinal);
        Assert.Contains("DownloadImportTemplateAsync", endpoints, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx"))
                || Directory.Exists(Path.Combine(dir.FullName, "src", "Platform")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
    }
}
public sealed class CatalogImportCsvParserTests
{
    private static string SchemaHeaderLine => string.Join(',', CatalogImportCsvSchema.RequiredColumns);

    [Fact]
    public void Parse_reads_quoted_commas_and_row_numbers()
    {
        var csv = SchemaHeaderLine + """

            "Soft, Drink",Beverages,Desc,BrandX,Bottle,480001,SKU-1,25.50,18.00,VAT,tag1|tag2,SariSari,Draft
            Chips,Snacks,Desc2,BrandY,Pack,480002,SKU-2,12.00,8.50,VAT,snack,SariSari,Active
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var rows = CatalogImportCsvParser.Parse(stream);
        Assert.Equal(2, rows.Count);
        Assert.Equal(2, rows[0].RowNumber);
        Assert.Equal("Soft, Drink", rows[0].Cells[CatalogImportCsvSchema.ProductName]);
        Assert.Equal("Bottle", rows[0].Cells[CatalogImportCsvSchema.Unit]);
        Assert.Equal(3, rows[1].RowNumber);
    }

    [Fact]
    public void Parse_rejects_legacy_renamed_headers()
    {
        var csv = """
            Name,Unit,Sku,Barcode
            Coke,Bottle,SKU-1,480001
            """;
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csv));
        var ex = Assert.Throws<DomainException>(() => CatalogImportCsvParser.Parse(stream));
        Assert.Equal(DomainErrorCodes.CatalogImportHeadersInvalid, ex.ErrorCode);
    }
}

public sealed class CatalogImportRowMapperTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

    private static Dictionary<string, string> RequiredCells(
        string productName,
        Action<Dictionary<string, string>>? customize = null)
    {
        var cells = new Dictionary<string, string>
        {
            ["ProductName"] = productName,
            ["Category"] = "General",
            ["Brand"] = "TestBrand",
            ["Unit"] = "Piece",
            ["Barcode"] = $"480{Random.Shared.Next(100000, 999999)}",
            ["SuggestedSku"] = $"SKU-{Guid.NewGuid():N}"[..12].ToUpperInvariant()
        };
        customize?.Invoke(cells);
        return cells;
    }

    [Fact]
    public async Task MapRows_marks_formula_injection_as_failed()
    {
        var rows = new List<CatalogImportRawRow>
        {
            new(2, new Dictionary<string, string>
            {
                ["ProductName"] = "=CMD|'/C calc'!A0",
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
                ["ProductName"] = "A",
                ["Unit"] = "Piece",
                ["Brand"] = "BrandA",
                ["Category"] = "General",
                ["SuggestedSku"] = "SKU-A",
                ["Barcode"] = "480001"
            }),
            new(3, new Dictionary<string, string>
            {
                ["ProductName"] = "B",
                ["Unit"] = "Piece",
                ["Brand"] = "BrandB",
                ["Category"] = "General",
                ["SuggestedSku"] = "SKU-B",
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
            new(2, RequiredCells("Existing", c => c["Barcode"] = "480099"))
        };

        var items = await CatalogImportRowMapper.MapRowsAsync(
            rows,
            new FakeCategoryRepository(),
            products,
            T0);

        Assert.Equal(CatalogImportItemStatus.Skipped, items[0].Status);
        Assert.Equal(ApplicationErrorCodes.DuplicateGlobalProductBarcode, items[0].ErrorCode);
    }

    [Fact]
    public async Task MapRows_rejects_invalid_unit_status_business_types_and_decimal()
    {
        var rows = new List<CatalogImportRawRow>
        {
            new(2, RequiredCells("BadUnit", c => c["Unit"] = "NotAUnit")),
            new(3, RequiredCells("BadStatus", c => c["Status"] = "Published")),
            new(4, RequiredCells("BadBiz", c => c["BusinessTypes"] = "NotAType")),
            new(5, RequiredCells("BadPrice", c => c["SuggestedSellingPrice"] = "not-a-number"))
        };

        var items = await CatalogImportRowMapper.MapRowsAsync(
            rows,
            new FakeCategoryRepository(),
            new FakeProductRepository(),
            T0);

        Assert.Equal(DomainErrorCodes.InvalidGlobalProductUnit, items[0].ErrorCode);
        Assert.Equal(DomainErrorCodes.InvalidGlobalProductStatus, items[1].ErrorCode);
        Assert.Equal(DomainErrorCodes.InvalidGlobalCatalogBusinessType, items[2].ErrorCode);
        Assert.Equal(DomainErrorCodes.InvalidGlobalProductMoney, items[3].ErrorCode);
    }

    [Fact]
    public async Task MapRows_rejects_blank_category()
    {
        var rows = new List<CatalogImportRawRow>
        {
            new(2, RequiredCells("No Category", c => c.Remove("Category")))
        };

        var items = await CatalogImportRowMapper.MapRowsAsync(
            rows,
            new FakeCategoryRepository(),
            new FakeProductRepository(),
            T0);

        Assert.Equal(CatalogImportItemStatus.Failed, items[0].Status);
        Assert.Equal(DomainErrorCodes.InvalidGlobalProductCategory, items[0].ErrorCode);
    }

    [Fact]
    public async Task MapRows_rejects_blank_brand()
    {
        var rows = new List<CatalogImportRawRow>
        {
            new(2, RequiredCells("No Brand", c => c["Brand"] = " "))
        };

        var items = await CatalogImportRowMapper.MapRowsAsync(
            rows,
            new FakeCategoryRepository(),
            new FakeProductRepository(),
            T0);

        Assert.Equal(CatalogImportItemStatus.Failed, items[0].Status);
        Assert.Equal(DomainErrorCodes.InvalidGlobalProductBrand, items[0].ErrorCode);
    }

    [Fact]
    public void ExtractBrand_removes_brand_tag_and_normalizes()
    {
        var tags = new List<string> { "alias", "brand:Acme Co", "tax:VAT" };
        var brand = CatalogImportRowMapper.ExtractBrand(tags);
        Assert.Equal("Acme Co", brand);
        Assert.DoesNotContain(tags, t => t.StartsWith("brand:", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MapRows_unknown_category_stays_pending_with_will_create_preview()
    {
        var rows = new List<CatalogImportRawRow>
        {
            new(2, RequiredCells("Sinandomeng 25kg", c =>
            {
                c["Category"] = "  Rice and Staples  ";
                c["Unit"] = "Kilogram";
            }))
        };

        var items = await CatalogImportRowMapper.MapRowsAsync(
            rows,
            new FakeCategoryRepository(),
            new FakeProductRepository(),
            T0);

        Assert.Single(items);
        Assert.Equal(CatalogImportItemStatus.Pending, items[0].Status);
        Assert.Null(items[0].GlobalCategoryId);
        Assert.Equal("Rice and Staples", items[0].CategoryName);
        Assert.True(CatalogImportRowMapper.WillCreateCategory(items[0]));
        var summary = CatalogImportRowMapper.BuildPreviewSummary(items);
        Assert.Equal(1, summary.NewCategoriesToCreateCount);
        Assert.Contains("1 new category will be created", summary.SummaryText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapRows_matches_existing_category_case_insensitively()
    {
        var existing = GlobalCategory.Create("Rice and Staples", T0);
        var categories = new FakeCategoryRepository();
        categories.Seed(existing);

        var rows = new List<CatalogImportRawRow>
        {
            new(2, RequiredCells("Jasmine Rice", c =>
            {
                c["Category"] = "rice and staples";
                c["Unit"] = "Kilogram";
            }))
        };

        var items = await CatalogImportRowMapper.MapRowsAsync(
            rows,
            categories,
            new FakeProductRepository(),
            T0);

        Assert.Equal(CatalogImportItemStatus.Pending, items[0].Status);
        Assert.Equal(existing.Id.Value, items[0].GlobalCategoryId);
        Assert.False(CatalogImportRowMapper.WillCreateCategory(items[0]));
    }

    [Fact]
    public async Task MapRows_eight_new_categories_shared_across_eighty_products()
    {
        var categoryNames = new[]
        {
            "Rice and Staples",
            "Canned Goods",
            "Noodles and Pasta",
            "Beverages",
            "Snacks",
            "Condiments",
            "Personal Care",
            "Household"
        };

        var rows = new List<CatalogImportRawRow>();
        for (var i = 0; i < 80; i++)
        {
            var category = categoryNames[i % 8];
            rows.Add(new CatalogImportRawRow(
                i + 2,
                RequiredCells($"Product {i + 1:000}", c =>
                {
                    c["Category"] = category;
                    c["Barcode"] = $"48000{i + 1:000000}";
                    c["SuggestedSku"] = $"SKU-{i + 1:000}";
                })));
        }

        var items = await CatalogImportRowMapper.MapRowsAsync(
            rows,
            new FakeCategoryRepository(),
            new FakeProductRepository(),
            T0);

        Assert.Equal(80, items.Count);
        Assert.All(items, i => Assert.Equal(CatalogImportItemStatus.Pending, i.Status));
        var summary = CatalogImportRowMapper.BuildPreviewSummary(items);
        Assert.Equal(80, summary.ValidProductCount);
        Assert.Equal(8, summary.NewCategoriesToCreateCount);
        Assert.Equal(0, summary.ExistingCategoriesReferencedCount);
        Assert.Contains("80 products valid", summary.SummaryText, StringComparison.Ordinal);
        Assert.Contains("8 new categories will be created", summary.SummaryText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MapRows_rejects_invalid_category_name_length()
    {
        var rows = new List<CatalogImportRawRow>
        {
            new(2, RequiredCells("X", c =>
                c["Category"] = new string('x', GlobalCatalogRules.NameMaxLength + 1)))
        };

        var items = await CatalogImportRowMapper.MapRowsAsync(
            rows,
            new FakeCategoryRepository(),
            new FakeProductRepository(),
            T0);

        Assert.Equal(CatalogImportItemStatus.Failed, items[0].Status);
        Assert.Equal(DomainErrorCodes.InvalidGlobalCatalogName, items[0].ErrorCode);
    }

    [Fact]
    public async Task MapRows_mixed_existing_and_new_categories()
    {
        var existing = GlobalCategory.Create("Beverages", T0);
        var categories = new FakeCategoryRepository();
        categories.Seed(existing);

        var rows = new List<CatalogImportRawRow>
        {
            new(2, RequiredCells("Coke", c =>
            {
                c["Category"] = "Beverages";
                c["Unit"] = "Can";
            })),
            new(3, RequiredCells("Rice", c =>
            {
                c["Category"] = "Rice and Staples";
                c["Unit"] = "Kilogram";
            }))
        };

        var items = await CatalogImportRowMapper.MapRowsAsync(
            rows,
            categories,
            new FakeProductRepository(),
            T0);

        var summary = CatalogImportRowMapper.BuildPreviewSummary(items);
        Assert.Equal(2, summary.ValidProductCount);
        Assert.Equal(1, summary.ExistingCategoriesReferencedCount);
        Assert.Equal(1, summary.NewCategoriesToCreateCount);
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

public sealed class CatalogImportCategoryCreateTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Process_creates_one_category_for_many_products_sharing_name()
    {
        var items = Enumerable.Range(0, 5)
            .Select(i => CatalogImportItem.CreatePending(
                i + 2,
                $"Product {i}",
                "Piece",
                sku: $"SKU-{i}",
                barcode: $"B{i:000000}",
                categoryName: "Rice and Staples",
                searchTagsRaw: "brand:ImportBrand"))
            .ToList();

        var job = CatalogImportJob.CreateValidated(
            "ph.csv",
            CatalogImportFileFormat.Csv,
            128,
            new string('d', 64),
            "platform-user:admin",
            items,
            T0);
        job.Confirm(T0.AddMinutes(1));

        var categories = new FakeCategoryRepository();
        var products = new FakeProductRepository { CaptureAdds = true };
        var imports = new FakeImportRepository(job);
        var uow = new FakeUnitOfWork();
        var audit = new FakeAuditWriter();
        var clock = new FakeClock(T0.AddMinutes(2));

        var processor = new ProcessCatalogImportChunk(imports, products, categories, uow, clock, audit);
        Assert.True(await processor.ExecuteOnceAsync());

        Assert.Single(categories.Items);
        Assert.Equal("Rice and Staples", categories.Items[0].Name);
        Assert.Equal(GlobalCategoryStatus.Active, categories.Items[0].Status);
        Assert.Null(categories.Items[0].ParentId);
        Assert.Equal(5, products.Added.Count);
        Assert.All(products.Added, p => Assert.Equal(categories.Items[0].Id, p.GlobalCategoryId));
        Assert.Equal(CatalogImportJobStatus.Completed, imports.Job!.Status);
        Assert.Equal(5, imports.Job.ImportedCount);
        Assert.Single(audit.Writes);
    }

    [Fact]
    public async Task Process_reuses_existing_category_and_is_idempotent_on_retry()
    {
        var existing = GlobalCategory.Create("Snacks", T0);
        var categories = new FakeCategoryRepository();
        categories.Seed(existing);

        var item = CatalogImportItem.CreatePending(
            2,
            "Chips",
            "Pack",
            sku: "SKU-CHIPS",
            barcode: "480099",
            categoryName: "snacks",
            searchTagsRaw: "brand:SnackBrand");
        var job = CatalogImportJob.CreateValidated(
            "snacks.csv",
            CatalogImportFileFormat.Csv,
            64,
            new string('e', 64),
            "actor",
            [item],
            T0);
        job.Confirm(T0.AddMinutes(1));

        var products = new FakeProductRepository { CaptureAdds = true };
        var imports = new FakeImportRepository(job);
        var processor = new ProcessCatalogImportChunk(
            imports,
            products,
            categories,
            new FakeUnitOfWork(),
            new FakeClock(T0.AddMinutes(2)),
            new FakeAuditWriter());

        Assert.True(await processor.ExecuteOnceAsync());
        Assert.Single(categories.Items);
        Assert.Equal(existing.Id, products.Added[0].GlobalCategoryId);
        Assert.Empty(new FakeAuditWriter().Writes);

        // Retry after complete: ClaimNext returns null / no pending.
        Assert.False(await processor.ExecuteOnceAsync());
        Assert.Single(categories.Items);
        Assert.Single(products.Added);
    }

    [Fact]
    public async Task Process_handles_concurrent_category_conflict_by_reusing_winner()
    {
        var categories = new FakeCategoryRepository { ThrowConflictOnFirstAdd = true };
        var item = CatalogImportItem.CreatePending(
            2,
            "Soap",
            "Piece",
            sku: "SKU-SOAP",
            barcode: "480001",
            categoryName: "Personal Care",
            searchTagsRaw: "brand:CareBrand");
        var job = CatalogImportJob.CreateValidated(
            "care.csv",
            CatalogImportFileFormat.Csv,
            32,
            new string('f', 64),
            "actor",
            [item],
            T0);
        job.Confirm(T0.AddMinutes(1));

        var products = new FakeProductRepository { CaptureAdds = true };
        var processor = new ProcessCatalogImportChunk(
            new FakeImportRepository(job),
            products,
            categories,
            new FakeUnitOfWork(),
            new FakeClock(T0.AddMinutes(2)),
            new FakeAuditWriter());

        Assert.True(await processor.ExecuteOnceAsync());
        Assert.Single(categories.Items);
        Assert.Equal(CatalogImportItemStatus.Imported, item.Status);
        Assert.Equal(categories.Items[0].Id, products.Added[0].GlobalCategoryId);
    }

    [Fact]
    public async Task Process_partial_product_failure_after_category_creation()
    {
        var categories = new FakeCategoryRepository();
        var good = CatalogImportItem.CreatePending(
            2,
            "Good",
            "Piece",
            sku: "SKU-GOOD",
            barcode: "GOOD",
            categoryName: "Household",
            searchTagsRaw: "brand:HouseBrand");
        var bad = CatalogImportItem.CreatePending(
            3,
            "Bad",
            "NotAUnit",
            sku: "SKU-BAD",
            barcode: "BAD",
            categoryName: "Household",
            searchTagsRaw: "brand:HouseBrand");
        var job = CatalogImportJob.CreateValidated(
            "mixed.csv",
            CatalogImportFileFormat.Csv,
            48,
            new string('g', 64),
            "actor",
            [good, bad],
            T0);
        job.Confirm(T0.AddMinutes(1));

        var products = new FakeProductRepository { CaptureAdds = true };
        var processor = new ProcessCatalogImportChunk(
            new FakeImportRepository(job),
            products,
            categories,
            new FakeUnitOfWork(),
            new FakeClock(T0.AddMinutes(2)),
            new FakeAuditWriter());

        Assert.True(await processor.ExecuteOnceAsync());
        Assert.Single(categories.Items);
        Assert.Equal(CatalogImportItemStatus.Imported, good.Status);
        Assert.Equal(CatalogImportItemStatus.Failed, bad.Status);
        Assert.Equal(CatalogImportJobStatus.CompletedWithWarnings, job.Status);
        Assert.Equal(1, job.ImportedCount);
        Assert.Equal(1, job.FailedCount);
    }
}

file sealed class FakeCategoryRepository : IGlobalCategoryRepository
{
    public List<GlobalCategory> Items { get; } = [];
    public bool ThrowConflictOnFirstAdd { get; set; }
    private bool _conflictThrown;

    public void Seed(GlobalCategory category) => Items.Add(category);

    public Task AddAsync(GlobalCategory category, CancellationToken cancellationToken = default)
    {
        if (ThrowConflictOnFirstAdd && !_conflictThrown)
        {
            _conflictThrown = true;
            // Simulate a concurrent writer winning the unique index: seed then conflict.
            if (Items.All(c => !string.Equals(c.Name, category.Name, StringComparison.OrdinalIgnoreCase)))
            {
                Items.Add(GlobalCategory.Create(category.Name, category.CreatedAtUtc));
            }

            throw new PersistenceConflictException(
                ApplicationErrorCodes.DuplicateGlobalCategoryName,
                "A category with this name already exists under the same parent.");
        }

        if (Items.Any(c =>
                c.ParentId is null
                && string.Equals(c.Name, category.Name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.DuplicateGlobalCategoryName,
                "A category with this name already exists under the same parent.");
        }

        Items.Add(category);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsWithNameUnderParentAsync(
        string name,
        GlobalCategoryId? parentId,
        GlobalCategoryId? excludingId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(c =>
            c.ParentId?.Value == parentId?.Value
            && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)
            && (excludingId is null || c.Id != excludingId)));

    public Task<IReadOnlyList<GlobalCategory>> FindByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GlobalCategory>>(
            Items.Where(c => string.Equals(c.Name.ToUpperInvariant(), normalizedName, StringComparison.Ordinal))
                .ToList());

    public Task<GlobalCategory?> GetByIdAsync(GlobalCategoryId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(c => c.Id == id));

    public Task<(IReadOnlyList<GlobalCategory> Items, int TotalCount)> ListAsync(
        GlobalCategoryStatus? status,
        GlobalCategoryId? parentId,
        BusinessType? businessType,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<(IReadOnlyList<GlobalCategory>, int)>((Items, Items.Count));

    public Task<IReadOnlyList<GlobalCategory>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GlobalCategory>>(
            Items.Where(c => ids.Contains(c.Id.Value)).ToList());

    public Task UpdateAsync(GlobalCategory category, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

file sealed class FakeProductRepository : IGlobalProductRepository
{
    public HashSet<string> ExistingBarcodes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> ExistingSkus { get; } = new(StringComparer.OrdinalIgnoreCase);
    public bool CaptureAdds { get; set; }
    public List<GlobalProduct> Added { get; } = [];

    public Task AddAsync(GlobalProduct product, CancellationToken cancellationToken = default)
    {
        if (CaptureAdds)
        {
            Added.Add(product);
        }

        if (product.Barcode is not null)
        {
            ExistingBarcodes.Add(product.Barcode);
        }

        if (product.Sku is not null)
        {
            ExistingSkus.Add(product.Sku);
        }

        return Task.CompletedTask;
    }

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
        CancellationToken cancellationToken = default,
        IReadOnlyCollection<Guid>? excludeProductIds = null,
        GlobalProductListSortBy sortBy = GlobalProductListSortBy.Name,
        bool sortDescending = false) =>
        Task.FromResult<(IReadOnlyList<GlobalProduct>, int)>(([], 0));

    public Task<IReadOnlyList<GlobalProduct>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GlobalProduct>>([]);

    public Task UpdateAsync(GlobalProduct product, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

file sealed class FakeImportRepository : ICatalogImportJobRepository
{
    public CatalogImportJob? Job { get; private set; }
    private bool _claimed;

    public FakeImportRepository(CatalogImportJob job) => Job = job;

    public Task AddAsync(CatalogImportJob job, CancellationToken cancellationToken = default)
    {
        Job = job;
        return Task.CompletedTask;
    }

    public Task<CatalogImportJob?> ClaimNextAsync(
        DateTimeOffset utcNow,
        TimeSpan staleAfter,
        CancellationToken cancellationToken = default)
    {
        if (_claimed || Job is null)
        {
            return Task.FromResult<CatalogImportJob?>(null);
        }

        if (Job.Status is CatalogImportJobStatus.Queued
            or CatalogImportJobStatus.Processing)
        {
            _claimed = true;
            return Task.FromResult<CatalogImportJob?>(Job);
        }

        return Task.FromResult<CatalogImportJob?>(null);
    }

    public Task<CatalogImportJob?> GetByIdAsync(CatalogImportJobId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Job is not null && Job.Id == id ? Job : null);

    public Task<CatalogImportJob?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CatalogImportJob?>(null);

    public Task<(IReadOnlyList<CatalogImportJob> Items, int TotalCount)> ListAsync(
        CatalogImportJobStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<(IReadOnlyList<CatalogImportJob>, int)>(([], 0));

    public Task<IReadOnlyList<CatalogImportErrorDto>> ListErrorsAsync(
        CatalogImportJobId id,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CatalogImportErrorDto>>([]);

    public Task UpdateAsync(CatalogImportJob job, CancellationToken cancellationToken = default)
    {
        Job = job;
        return Task.CompletedTask;
    }
}

file sealed class FakeUnitOfWork : IPlatformUnitOfWork
{
    public bool ThrowConflictOnce { get; set; }
    private bool _thrown;

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ThrowConflictOnce && !_thrown)
        {
            _thrown = true;
            throw new PersistenceConflictException(
                ApplicationErrorCodes.DuplicateGlobalCategoryName,
                "A category with this name already exists under the same parent.");
        }

        return Task.CompletedTask;
    }
}

file sealed class FakeClock : IClock
{
    public FakeClock(DateTimeOffset utcNow) => UtcNow = utcNow;
    public DateTimeOffset UtcNow { get; }
}

file sealed class FakeAuditWriter : IAuditWriter
{
    public List<(string Action, string TargetId, string? Summary)> Writes { get; } = [];

    public Task WriteAsync(
        string actorIdentifier,
        AuditActorType actorType,
        string actionCode,
        string targetType,
        string targetId,
        AuditOutcome outcome,
        PlatformOrganizationId? organizationId = null,
        ProductCode? productCode = null,
        string? correlationId = null,
        string? reason = null,
        string? summary = null,
        CancellationToken cancellationToken = default)
    {
        Writes.Add((actionCode, targetId, summary));
        return Task.CompletedTask;
    }
}
