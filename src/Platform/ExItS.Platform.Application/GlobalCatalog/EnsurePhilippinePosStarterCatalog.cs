using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.GlobalCatalog;
using Microsoft.Extensions.Logging;

namespace ExItS.Platform.Application.GlobalCatalog;

public sealed record EnsurePhilippinePosStarterCatalogResultDto(
    int BusinessTypesAdded,
    int BusinessTypesUpdated,
    int CategoriesAdded,
    int CategoriesUpdated,
    int ProductsAdded,
    int ProductsUpdated,
    int TemplatesAdded,
    int TemplatesUpdated,
    int TemplateLinksAdded);

/// <summary>
/// WP10A: idempotent Philippine POS Business Types, global categories/products, and starter templates.
/// Safe to re-run in Development/Local Validation. Does not bypass merchant entitlement filtering.
/// </summary>
public sealed class EnsurePhilippinePosStarterCatalog
{
    private readonly IBusinessTypeRepository _businessTypes;
    private readonly IGlobalCategoryRepository _categories;
    private readonly IGlobalProductRepository _products;
    private readonly ICatalogTemplateRepository _templates;
    private readonly IPlatformUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly ILogger<EnsurePhilippinePosStarterCatalog> _logger;

    public EnsurePhilippinePosStarterCatalog(
        IBusinessTypeRepository businessTypes,
        IGlobalCategoryRepository categories,
        IGlobalProductRepository products,
        ICatalogTemplateRepository templates,
        IPlatformUnitOfWork uow,
        IClock clock,
        ILogger<EnsurePhilippinePosStarterCatalog> logger)
    {
        _businessTypes = businessTypes;
        _categories = categories;
        _products = products;
        _templates = templates;
        _uow = uow;
        _clock = clock;
        _logger = logger;
    }

    public async Task<EnsurePhilippinePosStarterCatalogResultDto> ExecuteAsync(CancellationToken ct = default)
    {
        var now = _clock.UtcNow;
        var btAdded = 0;
        var btUpdated = 0;
        var catAdded = 0;
        var catUpdated = 0;
        var prodAdded = 0;
        var prodUpdated = 0;
        var tmplAdded = 0;
        var tmplUpdated = 0;
        var linksAdded = 0;

        var btByCode = new Dictionary<string, BusinessType>(StringComparer.OrdinalIgnoreCase);
        foreach (var seed in PhilippineBusinessTypeSeeds.All)
        {
            var existing = await _businessTypes.GetByCodeAsync(seed.Code, ct).ConfigureAwait(false);
            if (existing is null)
            {
                var created = BusinessType.Create(
                    seed.Code,
                    seed.Name,
                    now,
                    description: $"Philippine Pinoy Business POS default type ({seed.Code}).",
                    sortOrder: seed.SortOrder,
                    id: BusinessTypeId.From(seed.Id));
                await _businessTypes.AddAsync(created, ct).ConfigureAwait(false);
                btByCode[seed.Code] = created;
                btAdded++;
                continue;
            }

            var touched = false;
            if (!string.Equals(existing.Name, seed.Name, StringComparison.Ordinal))
            {
                existing.Rename(seed.Name, now);
                touched = true;
            }

            if (existing.SortOrder != seed.SortOrder)
            {
                existing.SetSortOrder(seed.SortOrder, now);
                touched = true;
            }

            if (existing.Status != BusinessTypeStatus.Active)
            {
                existing.SetStatus(BusinessTypeStatus.Active, now);
                touched = true;
            }

            if (touched)
            {
                await _businessTypes.UpdateAsync(existing, ct).ConfigureAwait(false);
                btUpdated++;
            }

            btByCode[seed.Code] = existing;
        }

        if (btAdded > 0 || btUpdated > 0)
        {
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        var categoryByName = new Dictionary<string, GlobalCategory>(StringComparer.OrdinalIgnoreCase);
        foreach (var def in PhilippinePosStarterCatalogData.Categories)
        {
            var btIds = ResolveBtIds(def.BusinessTypeCodes, btByCode);
            var normalized = def.Name.Trim().ToUpperInvariant();
            var matches = await _categories.FindByNormalizedNameAsync(normalized, ct).ConfigureAwait(false);
            var existing = matches.FirstOrDefault(c => c.ParentId is null);
            if (existing is null)
            {
                var created = GlobalCategory.Create(
                    def.Name,
                    now,
                    sortOrder: def.SortOrder,
                    businessTypeIds: btIds);
                await _categories.AddAsync(created, ct).ConfigureAwait(false);
                categoryByName[def.Name] = created;
                catAdded++;
                continue;
            }

            var before = existing.BusinessTypeIds.Select(i => i.Value).OrderBy(v => v).ToArray();
            existing.AssignBusinessTypes(btIds, now);
            if (existing.SortOrder != def.SortOrder)
            {
                existing.SetSortOrder(def.SortOrder, now);
            }

            if (existing.Status != GlobalCategoryStatus.Active)
            {
                existing.SetStatus(GlobalCategoryStatus.Active, now);
            }

            var after = existing.BusinessTypeIds.Select(i => i.Value).OrderBy(v => v).ToArray();
            if (!before.SequenceEqual(after) || existing.SortOrder != def.SortOrder)
            {
                await _categories.UpdateAsync(existing, ct).ConfigureAwait(false);
                catUpdated++;
            }

            categoryByName[def.Name] = existing;
        }

        if (catAdded > 0 || catUpdated > 0)
        {
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        var productBySku = new Dictionary<string, GlobalProduct>(StringComparer.OrdinalIgnoreCase);
        foreach (var def in PhilippinePosStarterCatalogData.Products)
        {
            if (!categoryByName.TryGetValue(def.CategoryName, out var category))
            {
                throw new InvalidOperationException($"Starter catalog category '{def.CategoryName}' was not ensured.");
            }

            var btIds = ResolveBtIds(def.BusinessTypeCodes, btByCode);
            var existing = await FindProductBySkuAsync(def.Sku, ct).ConfigureAwait(false);
            if (existing is null)
            {
                var created = GlobalProduct.Create(
                    def.Name,
                    def.Unit,
                    def.Sku,
                    barcode: null,
                    def.Brand,
                    category.Id,
                    now,
                    def.CostPrice,
                    def.SellingPrice,
                    businessTypeIds: btIds,
                    sellingMode: def.SellingMode);
                created.SetStatus(GlobalProductStatus.Active, now);
                await _products.AddAsync(created, ct).ConfigureAwait(false);
                productBySku[def.Sku] = created;
                prodAdded++;
                continue;
            }

            var touched = false;
            if (!string.Equals(existing.Name, def.Name, StringComparison.Ordinal)
                || existing.Unit != def.Unit
                || existing.SellingMode != def.SellingMode
                || existing.GlobalCategoryId != category.Id
                || existing.CostPrice != def.CostPrice
                || existing.SellingPrice != def.SellingPrice
                || !string.Equals(existing.Brand, def.Brand, StringComparison.Ordinal))
            {
                existing.Update(
                    def.Name,
                    def.Unit,
                    def.Sku,
                    existing.Barcode,
                    def.Brand,
                    category.Id,
                    now,
                    def.CostPrice,
                    def.SellingPrice,
                    sellingMode: def.SellingMode);
                touched = true;
            }

            var before = existing.BusinessTypeIds.Select(i => i.Value).OrderBy(v => v).ToArray();
            existing.AssignBusinessTypes(btIds, now);
            var after = existing.BusinessTypeIds.Select(i => i.Value).OrderBy(v => v).ToArray();
            if (!before.SequenceEqual(after))
            {
                touched = true;
            }

            if (existing.Status != GlobalProductStatus.Active)
            {
                existing.SetStatus(GlobalProductStatus.Active, now);
                touched = true;
            }

            if (touched)
            {
                await _products.UpdateAsync(existing, ct).ConfigureAwait(false);
                prodUpdated++;
            }

            productBySku[def.Sku] = existing;
        }

        if (prodAdded > 0 || prodUpdated > 0)
        {
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        foreach (var def in PhilippinePosStarterCatalogData.Templates)
        {
            if (!btByCode.TryGetValue(def.PrimaryBusinessTypeCode, out var primaryBt))
            {
                throw new InvalidOperationException($"Primary Business Type '{def.PrimaryBusinessTypeCode}' missing for template '{def.Slug}'.");
            }

            var existing = await FindTemplateBySlugAsync(def.Slug, ct).ConfigureAwait(false);
            CatalogTemplate template;
            var isNew = existing is null;
            var metaUpdated = false;
            if (isNew)
            {
                template = CatalogTemplate.Create(
                    def.Name,
                    primaryBt.Id,
                    now,
                    slug: def.Slug,
                    description: def.Description,
                    defaultBatchSize: Math.Clamp(def.ProductSkus.Length, 1, 50),
                    selectionMode: SelectionMode.Curated);
                tmplAdded++;
            }
            else
            {
                template = existing!;
                if (!string.Equals(template.Name, def.Name, StringComparison.Ordinal)
                    || !string.Equals(template.Description, def.Description, StringComparison.Ordinal)
                    || template.PrimaryBusinessTypeId != primaryBt.Id
                    || template.DefaultBatchSize != Math.Clamp(def.ProductSkus.Length, 1, 50))
                {
                    template.Update(
                        def.Name,
                        primaryBt.Id,
                        now,
                        description: def.Description,
                        defaultBatchSize: Math.Clamp(def.ProductSkus.Length, 1, 50),
                        selectionMode: SelectionMode.Curated);
                    metaUpdated = true;
                    tmplUpdated++;
                }
            }

            var sort = 0;
            var linkTouched = false;
            foreach (var sku in def.ProductSkus)
            {
                if (!productBySku.TryGetValue(sku, out var product))
                {
                    product = await FindProductBySkuAsync(sku, ct).ConfigureAwait(false)
                        ?? throw new InvalidOperationException($"Template '{def.Slug}' references missing SKU '{sku}'.");
                    productBySku[sku] = product;
                }

                if (template.TryAssignProduct(product.Id, now, isFeatured: sort < 5, isFirstBatch: true, sortOrder: sort))
                {
                    linksAdded++;
                    linkTouched = true;
                }

                sort++;
            }

            if (template.Status != CatalogTemplateStatus.Published)
            {
                template.Publish(now);
                linkTouched = true;
            }

            if (isNew)
            {
                await _templates.AddAsync(template, ct).ConfigureAwait(false);
            }
            else if (linkTouched || metaUpdated)
            {
                await _templates.UpdateAsync(template, ct).ConfigureAwait(false);
            }
        }

        if (tmplAdded > 0 || tmplUpdated > 0 || linksAdded > 0)
        {
            await _uow.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        _logger.LogInformation(
            "EnsurePhilippinePosStarterCatalog finished (BT +{BtAdd}/~{BtUp}, Cat +{CatAdd}/~{CatUp}, Prod +{ProdAdd}/~{ProdUp}, Tmpl +{TmplAdd}/~{TmplUp}, Links +{Links}).",
            btAdded,
            btUpdated,
            catAdded,
            catUpdated,
            prodAdded,
            prodUpdated,
            tmplAdded,
            tmplUpdated,
            linksAdded);

        return new EnsurePhilippinePosStarterCatalogResultDto(
            btAdded,
            btUpdated,
            catAdded,
            catUpdated,
            prodAdded,
            prodUpdated,
            tmplAdded,
            tmplUpdated,
            linksAdded);
    }

    private static IReadOnlyList<BusinessTypeId> ResolveBtIds(
        IEnumerable<string> codes,
        IReadOnlyDictionary<string, BusinessType> btByCode) =>
        codes.Select(code =>
            {
                if (!btByCode.TryGetValue(code, out var bt))
                {
                    throw new InvalidOperationException($"Business Type code '{code}' was not ensured.");
                }

                return bt.Id;
            })
            .Distinct()
            .ToList();

    private async Task<GlobalProduct?> FindProductBySkuAsync(string sku, CancellationToken ct)
    {
        var (items, _) = await _products
            .ListAsync(
                status: null,
                categoryId: null,
                businessTypeId: null,
                businessTypeCode: null,
                search: null,
                barcode: null,
                sku: sku,
                skip: 0,
                take: 5,
                cancellationToken: ct)
            .ConfigureAwait(false);
        var normalized = GlobalCatalogRules.NormalizeSku(sku);
        return items.FirstOrDefault(p => string.Equals(p.Sku, normalized, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<CatalogTemplate?> FindTemplateBySlugAsync(string slug, CancellationToken ct)
    {
        var (items, _) = await _templates
            .ListAsync(
                status: null,
                primaryBusinessTypeId: null,
                primaryBusinessTypeCode: null,
                search: slug,
                skip: 0,
                take: 20,
                cancellationToken: ct)
            .ConfigureAwait(false);
        return items.FirstOrDefault(t => string.Equals(t.Slug, slug, StringComparison.OrdinalIgnoreCase));
    }
}
