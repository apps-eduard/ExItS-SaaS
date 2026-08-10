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

public sealed class CatalogImportTemplateAssignmentTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 10, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Global_only_import_does_not_link_template()
    {
        var item = Pending("Alpha", "SKU-A", "480100");
        var job = Validated([item]);
        job.Confirm(T0.AddMinutes(1));

        var templates = new TemplateAssignFakeTemplateRepository();
        var products = new TemplateAssignFakeProductRepo { CaptureAdds = true };
        var processor = CreateProcessor(job, products, templates);

        Assert.True(await processor.ExecuteOnceAsync());
        Assert.Equal(CatalogImportJobStatus.Completed, job.Status);
        Assert.Null(job.TargetTemplateId);
        Assert.Empty(templates.Updated);
        Assert.Single(products.Added);
    }

    [Fact]
    public async Task Import_plus_template_links_successful_products()
    {
        var template = CatalogTemplate.Create("Bakery", BusinessType.Bakery, T0);
        var templates = new TemplateAssignFakeTemplateRepository();
        templates.Seed(template);

        var item = Pending("Bread", "SKU-B", "480200");
        var job = Validated([item]);
        job.Confirm(T0.AddMinutes(1), template.Id.Value);

        var products = new TemplateAssignFakeProductRepo { CaptureAdds = true };
        var processor = CreateProcessor(job, products, templates);

        Assert.True(await processor.ExecuteOnceAsync());
        Assert.Equal(CatalogImportJobStatus.Completed, job.Status);
        Assert.Equal(template.Id.Value, job.TargetTemplateId);
        Assert.Single(products.Added);
        Assert.Single(templates.Updated);
        Assert.Equal(1, templates.Updated[0].ProductCount);
        Assert.Equal(products.Added[0].Id, templates.Updated[0].Products[0].GlobalProductId);
    }

    [Fact]
    public async Task Failed_rows_are_not_linked()
    {
        var template = CatalogTemplate.Create("Bakery", BusinessType.Bakery, T0);
        var templates = new TemplateAssignFakeTemplateRepository();
        templates.Seed(template);

        var good = Pending("Cake", "SKU-C", "480300");
        var bad = CatalogImportItem.CreatePending(
            3,
            "Bad",
            "NotAUnit",
            sku: "SKU-BAD",
            barcode: "480301",
            categoryName: "Pastry",
            sellingPrice: 12m,
            costPrice: 8m,
            searchTagsRaw: "brand:ImportBrand");
        var job = Validated([good, bad]);
        job.Confirm(T0.AddMinutes(1), template.Id.Value);

        var products = new TemplateAssignFakeProductRepo { CaptureAdds = true };
        var processor = CreateProcessor(job, products, templates);

        Assert.True(await processor.ExecuteOnceAsync());
        Assert.Equal(CatalogImportJobStatus.CompletedWithWarnings, job.Status);
        Assert.Equal(1, job.ImportedCount);
        Assert.Equal(1, job.FailedCount);
        Assert.Equal(1, templates.Updated[0].ProductCount);
        Assert.Equal(products.Added[0].Id, templates.Updated[0].Products[0].GlobalProductId);
    }

    [Fact]
    public async Task Skipped_existing_product_is_resolved_and_linked()
    {
        var template = CatalogTemplate.Create("Bakery", BusinessType.Bakery, T0);
        var templates = new TemplateAssignFakeTemplateRepository();
        templates.Seed(template);

        var existing = GlobalProduct.Create(
            "Existing Bun",
            ProductUnit.Piece,
            "SKU-EXIST",
            "480400",
            "BrandX",
            GlobalCategory.Create("Buns", T0).Id,
            T0,
            5m,
            10m);

        var products = new TemplateAssignFakeProductRepo { CaptureAdds = true };
        products.SeedExisting(existing);

        var item = Pending("Existing Bun", "SKU-EXIST", "480400");
        var job = Validated([item]);
        job.Confirm(T0.AddMinutes(1), template.Id.Value);

        var processor = CreateProcessor(job, products, templates);
        Assert.True(await processor.ExecuteOnceAsync());

        Assert.Equal(CatalogImportJobStatus.CompletedWithWarnings, job.Status);
        Assert.Equal(0, job.ImportedCount);
        Assert.Equal(1, job.SkippedCount);
        Assert.Equal(existing.Id.Value, job.Items[0].CreatedGlobalProductId);
        Assert.Equal(1, templates.Updated[0].ProductCount);
        Assert.Equal(existing.Id, templates.Updated[0].Products[0].GlobalProductId);
        Assert.Empty(products.Added);
    }

    [Fact]
    public async Task Duplicate_confirm_and_reprocess_remain_idempotent()
    {
        var template = CatalogTemplate.Create("Bakery", BusinessType.Bakery, T0);
        var templates = new TemplateAssignFakeTemplateRepository();
        templates.Seed(template);

        var item = Pending("Cookie", "SKU-D", "480500");
        var job = Validated([item]);
        job.Confirm(T0.AddMinutes(1), template.Id.Value);

        var products = new TemplateAssignFakeProductRepo { CaptureAdds = true };
        var imports = new TemplateAssignFakeImportRepo(job);
        var processor = new ProcessCatalogImportChunk(
            imports,
            products,
            new TemplateAssignFakeCategoryRepo(),
            new TemplateAssignFakeUow(),
            new TemplateAssignFixedClock(T0.AddMinutes(2)),
            new TemplateAssignNoopAudit(),
            templates);

        Assert.True(await processor.ExecuteOnceAsync());
        Assert.Equal(1, templates.Updated[0].ProductCount);
        Assert.Single(products.Added);

        var confirm = new ConfirmCatalogImport(
            imports,
            templates,
            new TemplateAssignFakeUow(),
            new TemplateAssignFixedClock(T0.AddMinutes(3)));
        var confirmed = await confirm.ExecuteAsync(
            job.Id.Value,
            new ConfirmCatalogImportRequest(TargetTemplateId: template.Id.Value));
        Assert.True(confirmed.IsSuccess);
        Assert.Equal(template.Id.Value, confirmed.Value!.TargetTemplateId);

        Assert.False(await processor.ExecuteOnceAsync());
        Assert.Equal(1, templates.Updated[0].ProductCount);
        Assert.Single(products.Added);
    }

    [Fact]
    public async Task Duplicate_template_membership_is_noop()
    {
        var template = CatalogTemplate.Create("Bakery", BusinessType.Bakery, T0);
        var productId = GlobalProductId.New();
        template.AssignProduct(productId, T0);
        var templates = new TemplateAssignFakeTemplateRepository();
        templates.Seed(template);

        Assert.False(template.TryAssignProduct(productId, T0.AddMinutes(1)));
        Assert.Equal(1, template.ProductCount);

        await templates.UpdateAsync(template);
        Assert.Equal(1, templates.Updated[0].ProductCount);
    }

    [Fact]
    public async Task Confirm_with_missing_template_fails()
    {
        var item = Pending("Pie", "SKU-E", "480600");
        var job = Validated([item]);
        var imports = new TemplateAssignFakeImportRepo(job);
        var confirm = new ConfirmCatalogImport(
            imports,
            new TemplateAssignFakeTemplateRepository(),
            new TemplateAssignFakeUow(),
            new TemplateAssignFixedClock(T0.AddMinutes(1)));

        var result = await confirm.ExecuteAsync(
            job.Id.Value,
            new ConfirmCatalogImportRequest(TargetTemplateId: Guid.NewGuid()));

        Assert.False(result.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.CatalogTemplateNotFound, result.ErrorCode);
        Assert.Equal(CatalogImportJobStatus.Validated, job.Status);
        Assert.Null(job.TargetTemplateId);
    }

    [Fact]
    public async Task Confirm_global_only_leaves_target_template_null()
    {
        var item = Pending("Tart", "SKU-F", "480700");
        var job = Validated([item]);
        var imports = new TemplateAssignFakeImportRepo(job);
        var confirm = new ConfirmCatalogImport(
            imports,
            new TemplateAssignFakeTemplateRepository(),
            new TemplateAssignFakeUow(),
            new TemplateAssignFixedClock(T0.AddMinutes(1)));

        var result = await confirm.ExecuteAsync(job.Id.Value, new ConfirmCatalogImportRequest());
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.TargetTemplateId);
        Assert.Equal(CatalogImportJobStatus.Queued, job.Status);
    }

    private static CatalogImportItem Pending(string name, string sku, string barcode) =>
        CatalogImportItem.CreatePending(
            2,
            name,
            "Piece",
            sku: sku,
            barcode: barcode,
            categoryName: "Pastry",
            sellingPrice: 12m,
            costPrice: 8m,
            searchTagsRaw: "brand:ImportBrand");

    private static CatalogImportJob Validated(IReadOnlyList<CatalogImportItem> items) =>
        CatalogImportJob.CreateValidated(
            "bakery.csv",
            CatalogImportFileFormat.Csv,
            128,
            new string('a', 64),
            "platform-user:admin",
            items,
            T0);

    private static ProcessCatalogImportChunk CreateProcessor(
        CatalogImportJob job,
        IGlobalProductRepository products,
        ICatalogTemplateRepository templates) =>
        new(
            new TemplateAssignFakeImportRepo(job),
            products,
            new TemplateAssignFakeCategoryRepo(),
            new TemplateAssignFakeUow(),
            new TemplateAssignFixedClock(T0.AddMinutes(2)),
            new TemplateAssignNoopAudit(),
            templates);
}

file sealed class TemplateAssignFakeTemplateRepository : ICatalogTemplateRepository
{
    private readonly Dictionary<Guid, CatalogTemplate> _byId = [];
    public List<CatalogTemplate> Updated { get; } = [];

    public void Seed(CatalogTemplate template) => _byId[template.Id.Value] = template;

    public Task<CatalogTemplate?> GetByIdAsync(CatalogTemplateId id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_byId.TryGetValue(id.Value, out var t) ? t : null);

    public Task<bool> ExistsWithSlugAsync(
        string slug,
        CatalogTemplateId? excludingId = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<(IReadOnlyList<CatalogTemplate> Items, int TotalCount)> ListAsync(
        CatalogTemplateStatus? status,
        BusinessType? primaryBusinessType,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default,
        CatalogTemplateListSortBy sortBy = CatalogTemplateListSortBy.Name,
        bool sortDescending = false) =>
        Task.FromResult<(IReadOnlyList<CatalogTemplate>, int)>((_byId.Values.ToList(), _byId.Count));

    public Task AddAsync(CatalogTemplate template, CancellationToken cancellationToken = default)
    {
        _byId[template.Id.Value] = template;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(CatalogTemplate template, CancellationToken cancellationToken = default)
    {
        _byId[template.Id.Value] = template;
        Updated.Add(template);
        return Task.CompletedTask;
    }
}

file sealed class TemplateAssignFakeProductRepo : IGlobalProductRepository
{
    public bool CaptureAdds { get; set; }
    public List<GlobalProduct> Added { get; } = [];
    public HashSet<string> ExistingBarcodes { get; } = new(StringComparer.OrdinalIgnoreCase);
    public HashSet<string> ExistingSkus { get; } = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<GlobalProduct> _existing = [];

    public void SeedExisting(GlobalProduct product)
    {
        _existing.Add(product);
        if (product.Barcode is not null)
        {
            ExistingBarcodes.Add(product.Barcode);
        }

        if (product.Sku is not null)
        {
            ExistingSkus.Add(product.Sku);
        }
    }

    public Task AddAsync(GlobalProduct product, CancellationToken cancellationToken = default)
    {
        if (CaptureAdds)
        {
            Added.Add(product);
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
        Task.FromResult(_existing.Concat(Added).FirstOrDefault(p => p.Id == id));

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
        bool sortDescending = false)
    {
        IEnumerable<GlobalProduct> query = _existing.Concat(Added);
        if (!string.IsNullOrWhiteSpace(barcode))
        {
            query = query.Where(p => string.Equals(p.Barcode, barcode, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(sku))
        {
            query = query.Where(p => string.Equals(p.Sku, sku, StringComparison.OrdinalIgnoreCase));
        }

        var matched = query.Skip(skip).Take(take).ToList();
        return Task.FromResult<(IReadOnlyList<GlobalProduct>, int)>((matched, matched.Count));
    }

    public Task<IReadOnlyList<GlobalProduct>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GlobalProduct>>(
            _existing.Concat(Added).Where(p => ids.Contains(p.Id.Value)).ToList());

    public Task UpdateAsync(GlobalProduct product, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

file sealed class TemplateAssignFakeCategoryRepo : IGlobalCategoryRepository
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

    public Task<IReadOnlyList<GlobalCategory>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<GlobalCategory>>([]);

    public Task<(IReadOnlyList<GlobalCategory> Items, int TotalCount)> ListAsync(
        GlobalCategoryStatus? status,
        GlobalCategoryId? parentId,
        BusinessType? businessType,
        string? search,
        int skip,
        int take,
        CancellationToken cancellationToken = default,
        GlobalCategoryListSortBy sortBy = GlobalCategoryListSortBy.SortOrder,
        bool sortDescending = false) =>
        Task.FromResult<(IReadOnlyList<GlobalCategory>, int)>(([], 0));

    public Task UpdateAsync(GlobalCategory category, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

file sealed class TemplateAssignFakeImportRepo : ICatalogImportJobRepository
{
    public CatalogImportJob Job { get; private set; }
    private bool _claimed;

    public TemplateAssignFakeImportRepo(CatalogImportJob job) => Job = job;

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
        if (_claimed || Job.Status is CatalogImportJobStatus.Completed or CatalogImportJobStatus.CompletedWithWarnings
            or CatalogImportJobStatus.Failed)
        {
            return Task.FromResult<CatalogImportJob?>(null);
        }

        if (Job.Status is CatalogImportJobStatus.Queued or CatalogImportJobStatus.Processing)
        {
            _claimed = true;
            return Task.FromResult<CatalogImportJob?>(Job);
        }

        return Task.FromResult<CatalogImportJob?>(null);
    }

    public Task<CatalogImportJob?> GetByIdAsync(CatalogImportJobId id, CancellationToken cancellationToken = default) =>
        Task.FromResult<CatalogImportJob?>(Job.Id == id ? Job : null);

    public Task<CatalogImportJob?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CatalogImportJob?>(null);

    public Task<(IReadOnlyList<CatalogImportJob> Items, int TotalCount)> ListAsync(
        CatalogImportJobStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<(IReadOnlyList<CatalogImportJob>, int)>(([Job], 1));

    public Task<IReadOnlyList<CatalogImportErrorDto>> ListErrorsAsync(
        CatalogImportJobId id,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CatalogImportErrorDto>>([]);

    public Task UpdateAsync(CatalogImportJob job, CancellationToken cancellationToken = default)
    {
        Job = job;
        if (job.Status is CatalogImportJobStatus.Queued or CatalogImportJobStatus.Processing)
        {
            _claimed = false;
        }

        return Task.CompletedTask;
    }
}

file sealed class TemplateAssignFakeUow : IPlatformUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}

file sealed class TemplateAssignFixedClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; } = utcNow;
}

file sealed class TemplateAssignNoopAudit : IAuditWriter
{
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
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
