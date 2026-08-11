using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.IntegrationTests.Support;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.Platform.IntegrationTests;

/// <summary>
/// WP04: merchant Global Catalog / Template discovery filtering by effective BT entitlement.
/// Uses repository/query services with entitled allowed-id sets (same filters as merchant endpoints).
/// </summary>
[Collection(PostgreSqlCollection.Name)]
public sealed class MerchantCatalogEntitlementFilteringTests(PostgreSqlFixture fixture)
{
    [Fact]
    public async Task Product_discovery_is_entitlement_scoped_with_correct_count_and_no_duplicates()
    {
        await using var provider = GlobalCatalogTestServices.Build(fixture.ConnectionString);
        var sari = await RequireBtId(provider, "SariSari");
        var veg = await RequireBtId(provider, "MiniGrocery");
        var pharmacy = await RequireBtId(provider, "Pharmacy");
        var bakery = await RequireBtId(provider, "Bakery");

        var sariOnly = await CreateProductAsync(provider, "WP04 Sari Only", ["SariSari"]);
        var vegOnly = await CreateProductAsync(provider, "WP04 Veg Only", ["MiniGrocery"]);
        var pharmacyOnly = await CreateProductAsync(provider, "WP04 Pharmacy Only", ["Pharmacy"]);
        var multiTag = await CreateProductAsync(provider, "WP04 Multi Tag", ["SariSari", "MiniGrocery"]);
        var bakeryOnly = await CreateProductAsync(provider, "WP04 Bakery Only", ["Bakery"]);

        using var scope = provider.CreateScope();
        var products = scope.ServiceProvider.GetRequiredService<GlobalProductQueryService>();

        var sariScoped = await products.ListAsync(
            GlobalProductStatus.Active,
            categoryId: null,
            businessTypeId: null,
            businessTypeCode: null,
            search: "WP04",
            barcode: null,
            sku: null,
            page: 1,
            pageSize: 50,
            allowedBusinessTypeIds: [sari]);
        Assert.Equal(2, sariScoped.TotalCount);
        Assert.Contains(sariScoped.Items, i => i.Id == sariOnly);
        Assert.Contains(sariScoped.Items, i => i.Id == multiTag);
        Assert.DoesNotContain(sariScoped.Items, i => i.Id == pharmacyOnly);
        Assert.DoesNotContain(sariScoped.Items, i => i.Id == vegOnly);
        Assert.Equal(sariScoped.Items.Select(i => i.Id).Distinct().Count(), sariScoped.Items.Count);

        var multiScoped = await products.ListAsync(
            GlobalProductStatus.Active,
            categoryId: null,
            businessTypeId: null,
            businessTypeCode: null,
            search: "WP04",
            barcode: null,
            sku: null,
            page: 1,
            pageSize: 50,
            allowedBusinessTypeIds: [sari, veg]);
        Assert.Equal(3, multiScoped.TotalCount);
        Assert.Contains(multiScoped.Items, i => i.Id == sariOnly);
        Assert.Contains(multiScoped.Items, i => i.Id == vegOnly);
        Assert.Contains(multiScoped.Items, i => i.Id == multiTag);
        Assert.DoesNotContain(multiScoped.Items, i => i.Id == pharmacyOnly);
        Assert.DoesNotContain(multiScoped.Items, i => i.Id == bakeryOnly);
        Assert.Equal(1, multiScoped.Items.Count(i => i.Id == multiTag));

        var narrowed = await products.ListAsync(
            GlobalProductStatus.Active,
            categoryId: null,
            businessTypeId: veg,
            businessTypeCode: null,
            search: "WP04",
            barcode: null,
            sku: null,
            page: 1,
            pageSize: 50,
            allowedBusinessTypeIds: null);
        Assert.Equal(2, narrowed.TotalCount);
        Assert.All(narrowed.Items, i => Assert.Contains(veg, i.BusinessTypeIds));

        var page = await products.ListAsync(
            GlobalProductStatus.Active,
            categoryId: null,
            businessTypeId: null,
            businessTypeCode: null,
            search: "WP04",
            barcode: null,
            sku: null,
            page: 1,
            pageSize: 1,
            allowedBusinessTypeIds: [sari, veg]);
        Assert.Equal(3, page.TotalCount);
        Assert.Single(page.Items);
    }

    [Fact]
    public async Task Category_discovery_hides_unrelated_and_dedupes_multi_bt()
    {
        await using var provider = GlobalCatalogTestServices.Build(fixture.ConnectionString);
        var sari = await RequireBtId(provider, "SariSari");
        var veg = await RequireBtId(provider, "MiniGrocery");

        var applicable = await CreateCategoryAsync(provider, "WP04 Cat Sari", ["SariSari"]);
        var multi = await CreateCategoryAsync(provider, "WP04 Cat Multi", ["SariSari", "MiniGrocery"]);
        var unrelated = await CreateCategoryAsync(provider, "WP04 Cat Pharmacy", ["Pharmacy"]);

        using var scope = provider.CreateScope();
        var categories = scope.ServiceProvider.GetRequiredService<GlobalCategoryQueryService>();

        var result = await categories.ListAsync(
            GlobalCategoryStatus.Active,
            parentId: null,
            businessTypeId: null,
            businessTypeCode: null,
            search: "WP04 Cat",
            page: 1,
            pageSize: 50,
            allowedBusinessTypeIds: [sari, veg]);

        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, i => i.Id == applicable);
        Assert.Contains(result.Items, i => i.Id == multi);
        Assert.DoesNotContain(result.Items, i => i.Id == unrelated);
        Assert.Equal(1, result.Items.Count(i => i.Id == multi));
    }

    [Fact]
    public async Task Template_list_and_product_filter_respect_entitlement()
    {
        await using var provider = GlobalCatalogTestServices.Build(fixture.ConnectionString);
        var sari = await RequireBtId(provider, "SariSari");
        var pharmacy = await RequireBtId(provider, "Pharmacy");

        var sariTemplate = await CreateTemplateAsync(provider, "WP04 Sari Tpl", "SariSari");
        var pharmacyTemplate = await CreateTemplateAsync(provider, "WP04 Pharmacy Tpl", "Pharmacy");

        var sariProduct = await CreateProductAsync(provider, "WP04 Tpl Sari Prod", ["SariSari"]);
        var pharmacyProduct = await CreateProductAsync(provider, "WP04 Tpl Pharmacy Prod", ["Pharmacy"]);
        var pharmacyTplProduct = await CreateProductAsync(provider, "WP04 Pharmacy Tpl Prod", ["Pharmacy"]);

        var afterSari = await AssignAsync(provider, sariTemplate.Id, sariProduct);
        Assert.True(afterSari.IsSuccess, afterSari.ErrorMessage);
        // Entitled template incorrectly containing an unentitled product link.
        var afterPharmacyLink = await AssignAsync(provider, sariTemplate.Id, pharmacyProduct);
        Assert.True(afterPharmacyLink.IsSuccess, afterPharmacyLink.ErrorMessage);
        var afterPharmacyTpl = await AssignAsync(provider, pharmacyTemplate.Id, pharmacyTplProduct);
        Assert.True(afterPharmacyTpl.IsSuccess, afterPharmacyTpl.ErrorMessage);

        var publishedSari = await PublishAsync(provider, sariTemplate.Id);
        Assert.True(publishedSari.IsSuccess, publishedSari.ErrorMessage);
        var publishedPharmacy = await PublishAsync(provider, pharmacyTemplate.Id);
        Assert.True(publishedPharmacy.IsSuccess, publishedPharmacy.ErrorMessage);

        using var scope = provider.CreateScope();
        var templates = scope.ServiceProvider.GetRequiredService<CatalogTemplateQueryService>();

        var listed = await templates.ListPublishedForMerchantsAsync(
            primaryBusinessTypeId: null,
            primaryBusinessTypeCode: null,
            search: "WP04",
            page: 1,
            pageSize: 50,
            allowedPrimaryBusinessTypeIds: [sari]);
        Assert.Contains(listed.Items, i => i.Id == sariTemplate.Id);
        Assert.DoesNotContain(listed.Items, i => i.Id == pharmacyTemplate.Id);

        var loaded = await templates.GetPublishedByIdAsync(sariTemplate.Id);
        Assert.NotNull(loaded);
        var filtered = await templates.ApplyMerchantProductEntitlementAsync(loaded!, [sari]);
        Assert.Contains(filtered.Products, p => p.GlobalProductId == sariProduct);
        Assert.DoesNotContain(filtered.Products, p => p.GlobalProductId == pharmacyProduct);
        Assert.Equal(filtered.Products.Count, filtered.ProductCount);

        var pharmacyDenied = await templates.ApplyMerchantProductEntitlementAsync(loaded!, [pharmacy]);
        // Pharmacy allowed set does not intersect Sari-tagged products on this template.
        Assert.DoesNotContain(pharmacyDenied.Products, p => p.GlobalProductId == sariProduct);
    }

    [Fact]
    public async Task Gate_rejects_unentitled_filter_and_omitted_filter_stays_scoped()
    {
        var allowed = Guid.NewGuid();
        var other = Guid.NewGuid();
        var scope = new MerchantCatalogEntitlementGate.DiscoveryScope(
            Unrestricted: false,
            OrganizationId: PlatformOrganizationId.New(),
            AllowedBusinessTypeIds: [allowed],
            Entitlement: null);
        var gate = new MerchantCatalogEntitlementGate(null!, null!, null!, new EmptyBusinessTypeRepo());

        var forged = await gate.ResolveListFilterAsync(scope, other, null);
        Assert.False(forged.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.BusinessTypeNotEntitled, forged.ErrorCode);

        var omitted = await gate.ResolveListFilterAsync(scope, null, null);
        Assert.True(omitted.IsSuccess);
        Assert.Null(omitted.Value.SingleBusinessTypeId);
        Assert.Equal([allowed], omitted.Value.AllowedBusinessTypeIds);

        var resourceOk = gate.EnsureResourceEntitled(scope, [BusinessTypeId.From(allowed), BusinessTypeId.From(other)]);
        Assert.True(resourceOk.IsSuccess);
        var resourceDenied = gate.EnsureResourceEntitled(scope, [BusinessTypeId.From(other)]);
        Assert.False(resourceDenied.IsSuccess);
    }

    private static async Task<Guid> RequireBtId(ServiceProvider provider, string code)
    {
        using var scope = provider.CreateScope();
        var bt = await scope.ServiceProvider.GetRequiredService<IBusinessTypeRepository>().GetByCodeAsync(code);
        Assert.NotNull(bt);
        return bt!.Id.Value;
    }

    private static async Task<Guid> CreateProductAsync(ServiceProvider provider, string name, string[] businessTypes)
    {
        using var scope = provider.CreateScope();
        var services = scope.ServiceProvider;
        var category = await services
            .GetRequiredService<CreateGlobalCategory>()
            .ExecuteAsync(new CreateGlobalCategoryRequest(
                $"Cat-{Guid.NewGuid():N}"[..20],
                BusinessTypes: businessTypes));
        Assert.True(category.IsSuccess, category.ErrorMessage);

        var created = await services
            .GetRequiredService<CreateGlobalProduct>()
            .ExecuteAsync(new CreateGlobalProductRequest(
                Name: name,
                Unit: "Piece",
                Sku: $"SKU-{Guid.NewGuid():N}"[..20],
                Barcode: null,
                Brand: "WP04",
                GlobalCategoryId: category.Value!.Id,
                CostPrice: 10m,
                SellingPrice: 15m,
                BusinessTypes: businessTypes));
        Assert.True(created.IsSuccess, created.ErrorMessage);

        var activated = await services
            .GetRequiredService<SetGlobalProductStatus>()
            .ExecuteAsync(created.Value!.Id, new SetGlobalProductStatusRequest("Active", created.Value.UpdatedAtUtc));
        Assert.True(activated.IsSuccess, activated.ErrorMessage);
        return activated.Value!.Id;
    }

    private static async Task<Guid> CreateCategoryAsync(ServiceProvider provider, string name, string[] businessTypes)
    {
        using var scope = provider.CreateScope();
        var created = await scope.ServiceProvider
            .GetRequiredService<CreateGlobalCategory>()
            .ExecuteAsync(new CreateGlobalCategoryRequest(name, BusinessTypes: businessTypes));
        Assert.True(created.IsSuccess, created.ErrorMessage);
        return created.Value!.Id;
    }

    private static async Task<CatalogTemplateDto> CreateTemplateAsync(
        ServiceProvider provider,
        string name,
        string primaryBusinessType)
    {
        using var scope = provider.CreateScope();
        var created = await scope.ServiceProvider
            .GetRequiredService<CreateCatalogTemplate>()
            .ExecuteAsync(new CreateCatalogTemplateRequest(
                Name: $"{name} {Guid.NewGuid():N}"[..Math.Min(64, name.Length + 9)],
                PrimaryBusinessType: primaryBusinessType,
                Slug: $"wp04-{Guid.NewGuid():N}",
                DefaultBatchSize: 50,
                SelectionMode: "Curated"));
        Assert.True(created.IsSuccess, created.ErrorMessage);
        return created.Value!;
    }

    private static async Task<ApplicationResult<CatalogTemplateDto>> PublishAsync(
        ServiceProvider provider,
        Guid templateId)
    {
        using var scope = provider.CreateScope();
        var current = await scope.ServiceProvider.GetRequiredService<CatalogTemplateQueryService>().GetByIdAsync(templateId);
        Assert.NotNull(current);
        return await scope.ServiceProvider
            .GetRequiredService<PublishCatalogTemplate>()
            .ExecuteAsync(templateId, new CatalogTemplateLifecycleRequest(current!.UpdatedAtUtc));
    }

    private static async Task<ApplicationResult<CatalogTemplateDto>> AssignAsync(
        ServiceProvider provider,
        Guid templateId,
        Guid productId)
    {
        using var scope = provider.CreateScope();
        var current = await scope.ServiceProvider.GetRequiredService<CatalogTemplateQueryService>().GetByIdAsync(templateId);
        Assert.NotNull(current);
        return await scope.ServiceProvider
            .GetRequiredService<AssignCatalogTemplateProduct>()
            .ExecuteAsync(templateId, new AssignCatalogTemplateProductRequest(
                productId,
                ExpectedUpdatedAtUtc: current!.UpdatedAtUtc));
    }

    private sealed class EmptyBusinessTypeRepo : IBusinessTypeRepository
    {
        public Task AddAsync(BusinessType businessType, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsWithCodeAsync(string code, BusinessTypeId? excludingId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> ExistsWithNameAsync(string name, BusinessTypeId? excludingId = null, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<BusinessType?> FindByNormalizedNameAsync(string normalizedName, CancellationToken cancellationToken = default) => Task.FromResult<BusinessType?>(null);
        public Task<BusinessType?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) => Task.FromResult<BusinessType?>(null);
        public Task<BusinessType?> GetByIdAsync(BusinessTypeId id, CancellationToken cancellationToken = default) => Task.FromResult<BusinessType?>(null);
        public Task<IReadOnlyList<BusinessType>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<BusinessType>>([]);
        public Task<bool> IsReferencedAsync(BusinessTypeId id, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<(IReadOnlyList<BusinessType> Items, int TotalCount)> ListAsync(BusinessTypeStatus? status, string? search, int skip, int take, CancellationToken cancellationToken = default, BusinessTypeListSortBy sortBy = BusinessTypeListSortBy.SortOrder, bool sortDescending = false) => Task.FromResult<(IReadOnlyList<BusinessType>, int)>(([], 0));
        public Task UpdateAsync(BusinessType businessType, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
