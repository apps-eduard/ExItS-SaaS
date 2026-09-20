using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.ConnectedSuppliers;

internal sealed class ConnectedSupplierRelationshipRepository(PosDbContext db) : IConnectedSupplierRelationshipRepository
{
    private IQueryable<ConnectedSupplierRelationshipRecord> QueryRelationships() =>
        db.ConnectedSupplierRelationships.Include(x => x.CategoryDiscountOverrides);

    public async Task<ConnectedSupplierRelationship?> GetAsync(ConnectedSupplierRelationshipId id,CancellationToken ct=default)
    {var r=await QueryRelationships().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id.Value,ct);return r is null?null:ConnectedSupplierEntityMapper.ToDomain(r);}
    public async Task<ConnectedSupplierRelationship?> FindOpenAsync(PosOrganizationId buyer,PosOrganizationId supplier,CancellationToken ct=default)
    {var r=await QueryRelationships().AsNoTracking().SingleOrDefaultAsync(x=>x.BuyerOrganizationId==buyer.Value&&x.SupplierOrganizationId==supplier.Value&&(x.Status==0||x.Status==1),ct);return r is null?null:ConnectedSupplierEntityMapper.ToDomain(r);}
    public async Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(PosOrganizationId org,bool supplierView,CancellationToken ct=default)=>
        (await QueryRelationships().AsNoTracking().Where(x=>supplierView?x.SupplierOrganizationId==org.Value:x.BuyerOrganizationId==org.Value)
        .OrderByDescending(x=>x.UpdatedAtUtc).ToListAsync(ct)).Select(ConnectedSupplierEntityMapper.ToDomain).ToList();
    public Task AddAsync(ConnectedSupplierRelationship x,CancellationToken ct=default){db.ConnectedSupplierRelationships.Add(ConnectedSupplierEntityMapper.ToRecord(x));return Task.CompletedTask;}
    public async Task UpdateAsync(ConnectedSupplierRelationship x,CancellationToken ct=default){var r=await QueryRelationships().SingleAsync(y=>y.Id==x.Id.Value,ct);ConnectedSupplierEntityMapper.Apply(x,r);}
}

internal sealed class SupplierProductExposureRepository(PosDbContext db) : ISupplierProductExposureRepository
{
    public async Task<SupplierProductExposure?> GetAsync(SupplierProductExposureId id,CancellationToken ct=default)
    {var r=await db.SupplierProductExposures.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id.Value,ct);return r is null?null:ConnectedSupplierEntityMapper.ToDomain(r);}
    public async Task<SupplierProductExposure?> GetByProductAsync(PosOrganizationId supplier,CatalogProductId productId,CancellationToken ct=default)
    {var r=await db.SupplierProductExposures.AsNoTracking().SingleOrDefaultAsync(x=>x.SupplierOrganizationId==supplier.Value&&x.ProductId==productId.Value,ct);return r is null?null:ConnectedSupplierEntityMapper.ToDomain(r);}
    public async Task<IReadOnlyList<SupplierProductExposure>> ListAsync(PosOrganizationId supplier,CancellationToken ct=default)=>
        (await db.SupplierProductExposures.AsNoTracking().Where(x=>x.SupplierOrganizationId==supplier.Value).OrderBy(x=>x.NameSnapshot).ToListAsync(ct))
            .Select(x => ConnectedSupplierEntityMapper.ToDomain(x)).ToList();
    public async Task<(IReadOnlyList<SupplierProductExposure> Items,int Total)> SearchAsync(PosOrganizationId supplier,string? query,string? category,int skip,int take,CancellationToken ct=default)
    {var q=db.SupplierProductExposures.AsNoTracking().Where(x=>x.SupplierOrganizationId==supplier.Value&&x.IsExposed&&x.IsOrderable);
     if(!string.IsNullOrWhiteSpace(query)){var term=query.Trim().ToUpper();q=q.Where(x=>x.NameSnapshot.ToUpper().Contains(term)||(x.SkuSnapshot!=null&&x.SkuSnapshot.ToUpper().Contains(term)));}
     if(!string.IsNullOrWhiteSpace(category)){var term=category.Trim().ToUpper();q=q.Where(x=>x.CategoryNameSnapshot!=null&&x.CategoryNameSnapshot.ToUpper()==term);}
     var total=await q.CountAsync(ct);var rows=await q.OrderBy(x=>x.NameSnapshot).ThenBy(x=>x.Id).Skip(skip).Take(take).ToListAsync(ct);
     return(rows.Select(x => ConnectedSupplierEntityMapper.ToDomain(x)).ToList(),total);}
    public Task AddAsync(SupplierProductExposure x,CancellationToken ct=default){db.SupplierProductExposures.Add(ConnectedSupplierEntityMapper.ToRecord(x));return Task.CompletedTask;}
    public async Task UpdateAsync(SupplierProductExposure x,CancellationToken ct=default){var r=await db.SupplierProductExposures.SingleAsync(y=>y.Id==x.Id.Value,ct);ConnectedSupplierEntityMapper.Apply(x,r);}
}

internal sealed class ConnectedBuyerProductShareRepository(PosDbContext db) : IConnectedBuyerProductShareRepository
{
    public async Task<ConnectedBuyerProductShare?> GetAsync(ConnectedBuyerProductShareId id,CancellationToken ct=default)
    {
        var row=await db.ConnectedBuyerProductShares.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id.Value,ct);
        return row is null?null:ConnectedSupplierEntityMapper.ToDomain(row);
    }
    public async Task<ConnectedBuyerProductShare?> FindAsync(ConnectedSupplierRelationshipId relationshipId,CatalogProductId productId,CancellationToken ct=default)
    {
        var row=await db.ConnectedBuyerProductShares.AsNoTracking()
            .SingleOrDefaultAsync(x=>x.RelationshipId==relationshipId.Value&&x.SupplierProductId==productId.Value,ct);
        return row is null?null:ConnectedSupplierEntityMapper.ToDomain(row);
    }
    public async Task<IReadOnlyList<ConnectedBuyerProductShare>> ListAsync(ConnectedSupplierRelationshipId relationshipId,CancellationToken ct=default)=>
        (await db.ConnectedBuyerProductShares.AsNoTracking().Where(x=>x.RelationshipId==relationshipId.Value)
            .OrderBy(x=>x.SupplierProductId).ToListAsync(ct)).Select(ConnectedSupplierEntityMapper.ToDomain).ToList();
    public async Task<(IReadOnlyList<SupplierProductExposure> Exposures,IReadOnlyList<ConnectedBuyerProductShare> Shares,int Total)>
        SearchSharedCatalogAsync(
            ConnectedSupplierRelationshipId relationshipId,
            PosOrganizationId supplier,
            string? query,
            string? category,
            int skip,
            int take,
            CancellationToken ct = default,
            CatalogSharingMode catalogSharingMode = CatalogSharingMode.SelectedOnly)
    {
        if (catalogSharingMode == CatalogSharingMode.AllEligible)
        {
            var allEligible =
                from exposure in db.SupplierProductExposures.AsNoTracking()
                join account in db.InventoryAccounts.AsNoTracking()
                    on new { Org = exposure.SupplierOrganizationId, Pid = exposure.ProductId }
                    equals new { Org = account.OrganizationId, Pid = account.ProductId }
                join product in db.CatalogProducts.AsNoTracking()
                    on new { Org = exposure.SupplierOrganizationId, Pid = exposure.ProductId }
                    equals new { Org = product.OrganizationId, Pid = product.Id }
                join categoryRow in db.ProductCategories.AsNoTracking()
                    on product.CategoryId equals categoryRow.Id into categoryGroup
                from categoryRow in categoryGroup.DefaultIfEmpty()
                join share in db.ConnectedBuyerProductShares.AsNoTracking()
                        .Where(s => s.RelationshipId == relationshipId.Value)
                    on exposure.ProductId equals share.SupplierProductId into shareGroup
                from share in shareGroup.DefaultIfEmpty()
                where exposure.SupplierOrganizationId == supplier.Value
                      && exposure.IsExposed
                      && exposure.IsOrderable
                      && account.IsTracked
                      && (share == null || share.IsShared)
                      && (categoryRow == null || categoryRow.OrganizationId == supplier.Value)
                select new
                {
                    exposure,
                    share,
                    // Authoritative supplier catalog category; snapshot is fallback only.
                    CategoryName = categoryRow != null
                        ? categoryRow.Name
                        : exposure.CategoryNameSnapshot,
                };

            if (!string.IsNullOrWhiteSpace(query))
            {
                var term = query.Trim().ToUpper();
                allEligible = allEligible.Where(x => x.exposure.NameSnapshot.ToUpper().Contains(term)
                    || (x.exposure.SkuSnapshot != null && x.exposure.SkuSnapshot.ToUpper().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(category))
            {
                var term = category.Trim().ToUpper();
                allEligible = allEligible.Where(x =>
                    x.CategoryName != null && x.CategoryName.ToUpper() == term);
            }

            var allTotal = await allEligible.CountAsync(ct).ConfigureAwait(false);
            var allRows = await allEligible
                .OrderBy(x => x.exposure.NameSnapshot).ThenBy(x => x.exposure.Id)
                .Skip(skip).Take(take)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            // Parallel lists: share may be null for inherited AllEligible rows (empty Guid placeholder not used —
            // callers match by product id via dictionary with null-safe GetValueOrDefault).
            var exposures = allRows
                .Select(x => ConnectedSupplierEntityMapper.ToDomain(x.exposure, x.CategoryName))
                .ToList();
            var shares = allRows
                .Where(x => x.share is not null)
                .Select(x => ConnectedSupplierEntityMapper.ToDomain(x.share!))
                .ToList();
            return (exposures, shares, allTotal);
        }

        var q =
            from exposure in db.SupplierProductExposures.AsNoTracking()
            join account in db.InventoryAccounts.AsNoTracking()
                on new { Org = exposure.SupplierOrganizationId, Pid = exposure.ProductId }
                equals new { Org = account.OrganizationId, Pid = account.ProductId }
            join product in db.CatalogProducts.AsNoTracking()
                on new { Org = exposure.SupplierOrganizationId, Pid = exposure.ProductId }
                equals new { Org = product.OrganizationId, Pid = product.Id }
            join categoryRow in db.ProductCategories.AsNoTracking()
                on product.CategoryId equals categoryRow.Id into categoryGroup
            from categoryRow in categoryGroup.DefaultIfEmpty()
            join share in db.ConnectedBuyerProductShares.AsNoTracking()
                on exposure.ProductId equals share.SupplierProductId
            where exposure.SupplierOrganizationId == supplier.Value
                  && exposure.IsExposed
                  && exposure.IsOrderable
                  && account.IsTracked
                  && share.RelationshipId == relationshipId.Value
                  && share.IsShared
                  && (categoryRow == null || categoryRow.OrganizationId == supplier.Value)
            select new
            {
                exposure,
                share,
                CategoryName = categoryRow != null
                    ? categoryRow.Name
                    : exposure.CategoryNameSnapshot,
            };
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToUpper();
            q = q.Where(x => x.exposure.NameSnapshot.ToUpper().Contains(term)
                || (x.exposure.SkuSnapshot != null && x.exposure.SkuSnapshot.ToUpper().Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            var term = category.Trim().ToUpper();
            q = q.Where(x => x.CategoryName != null && x.CategoryName.ToUpper() == term);
        }
        var total = await q.CountAsync(ct);
        var rows = await q.OrderBy(x => x.exposure.NameSnapshot).ThenBy(x => x.exposure.Id).Skip(skip).Take(take).ToListAsync(ct);
        return (
            rows.Select(x => ConnectedSupplierEntityMapper.ToDomain(x.exposure, x.CategoryName)).ToList(),
            rows.Select(x => ConnectedSupplierEntityMapper.ToDomain(x.share)).ToList(),
            total);
    }

    public async Task<BuyerProductShareSearchPage> SearchForSupplierManagementAsync(
        ConnectedSupplierRelationshipId relationshipId,
        PosOrganizationId supplier,
        string? query,
        string? category,
        string? shareFilter,
        int skip,
        int take,
        bool idsOnly,
        CancellationToken ct = default,
        CatalogSharingMode catalogSharingMode = CatalogSharingMode.SelectedOnly)
    {
        var activeStatus = nameof(CatalogProductStatus.Active);
        var products = db.CatalogProducts.AsNoTracking()
            .Where(x => x.OrganizationId == supplier.Value && x.Status == activeStatus);
        var shares = db.ConnectedBuyerProductShares.AsNoTracking()
            .Where(x => x.RelationshipId == relationshipId.Value);
        var exposures = db.SupplierProductExposures.AsNoTracking()
            .Where(x => x.SupplierOrganizationId == supplier.Value);
        var categories = db.ProductCategories.AsNoTracking();

        // Canonical tracked predicate — same org+product keys as EligibleCount / buyer catalog.
        // Inline against DbSet (not a closed-over IQueryable) so EF translates EXISTS reliably
        // in both WHERE and SELECT projections.
        var joined =
            from product in products
            join categoryRow in categories on product.CategoryId equals categoryRow.Id into categoryGroup
            from categoryRow in categoryGroup.DefaultIfEmpty()
            join share in shares on product.Id equals share.SupplierProductId into shareGroup
            from share in shareGroup.DefaultIfEmpty()
            join exposure in exposures on product.Id equals exposure.ProductId into exposureGroup
            from exposure in exposureGroup.DefaultIfEmpty()
            select new
            {
                product,
                categoryRow,
                share,
                exposure,
                IsInventoryTracked = db.InventoryAccounts.Any(a =>
                    a.OrganizationId == supplier.Value
                    && a.ProductId == product.Id
                    && a.IsTracked)
            };

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim().ToUpper();
            joined = joined.Where(x =>
                x.product.Name.ToUpper().Contains(term)
                || (x.product.Sku != null && x.product.Sku.ToUpper().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            if (string.Equals(category.Trim(), "__uncategorized__", StringComparison.OrdinalIgnoreCase))
            {
                joined = joined.Where(x => x.product.CategoryId == null);
            }
            else
            {
                var term = category.Trim().ToUpper();
                joined = joined.Where(x => x.categoryRow != null && x.categoryRow.Name.ToUpper() == term);
            }
        }

        var filter = NormalizeShareFilter(shareFilter);
        if (catalogSharingMode == CatalogSharingMode.AllEligible)
        {
            // Shared = buyer-visible (eligible + not excluded). Not shared = explicit exclusion only.
            // Ineligible = sellable/blocked rows that fail eligibility (e.g. untracked).
            joined = filter switch
            {
                "shared" => joined.Where(x =>
                    !x.product.IsBlockedFromConnectedBuyers
                    && x.product.CanBeSold
                    && x.IsInventoryTracked
                    && (x.share == null || x.share.IsShared)),
                "notshared" => joined.Where(x => x.share != null && !x.share.IsShared),
                "ineligible" => joined.Where(x =>
                    x.product.IsBlockedFromConnectedBuyers
                    || !x.product.CanBeSold
                    || !x.IsInventoryTracked),
                "customprice" => joined.Where(x =>
                    x.share != null
                    && x.share.IsShared
                    && x.share.BuyerSpecificPoPrice != null
                    && x.IsInventoryTracked),
                "blocked" => joined.Where(x => x.product.IsBlockedFromConnectedBuyers),
                _ => joined.Where(x => x.product.CanBeSold || x.product.IsBlockedFromConnectedBuyers)
            };
        }
        else
        {
            joined = filter switch
            {
                "shared" => joined.Where(x =>
                    x.share != null
                    && x.share.IsShared
                    && x.product.CanBeSold
                    && !x.product.IsBlockedFromConnectedBuyers
                    && x.IsInventoryTracked),
                "notshared" => joined.Where(x =>
                    x.share == null
                    || !x.share.IsShared
                    || !x.IsInventoryTracked
                    || x.product.IsBlockedFromConnectedBuyers
                    || !x.product.CanBeSold),
                "ineligible" => joined.Where(x =>
                    x.product.IsBlockedFromConnectedBuyers
                    || !x.product.CanBeSold
                    || !x.IsInventoryTracked),
                "customprice" => joined.Where(x =>
                    x.share != null && x.share.IsShared && x.share.BuyerSpecificPoPrice != null),
                "blocked" => joined.Where(x => x.product.IsBlockedFromConnectedBuyers),
                _ => joined
            };
        }

        var eligibleCount = await (
                from product in products
                join account in db.InventoryAccounts.AsNoTracking()
                    on new { Org = product.OrganizationId, Pid = product.Id }
                    equals new { Org = account.OrganizationId, Pid = account.ProductId }
                where !product.IsBlockedFromConnectedBuyers
                      && product.CanBeSold
                      && account.IsTracked
                select product.Id)
            .CountAsync(ct)
            .ConfigureAwait(false);
        var excludedCount = await (
                from product in products
                join account in db.InventoryAccounts.AsNoTracking()
                    on new { Org = product.OrganizationId, Pid = product.Id }
                    equals new { Org = account.OrganizationId, Pid = account.ProductId }
                join share in shares on product.Id equals share.SupplierProductId
                where !product.IsBlockedFromConnectedBuyers
                      && product.CanBeSold
                      && account.IsTracked
                      && !share.IsShared
                select product.Id)
            .CountAsync(ct)
            .ConfigureAwait(false);
        var explicitSharedCount = await (
            from product in products
            join account in db.InventoryAccounts.AsNoTracking()
                on new { Org = product.OrganizationId, Pid = product.Id }
                equals new { Org = account.OrganizationId, Pid = account.ProductId }
            join share in shares on product.Id equals share.SupplierProductId
            where share.IsShared
                  && !product.IsBlockedFromConnectedBuyers
                  && product.CanBeSold
                  && account.IsTracked
            select product.Id).CountAsync(ct).ConfigureAwait(false);
        var sharedCount = catalogSharingMode == CatalogSharingMode.AllEligible
            ? Math.Max(0, eligibleCount - excludedCount)
            : explicitSharedCount;

        var matchingCount = await joined.CountAsync(ct).ConfigureAwait(false);

        var facetRows = await joined
            .GroupBy(x => x.categoryRow != null ? x.categoryRow.Name : null)
            .Select(g => new { Category = g.Key, Count = g.Count() })
            .OrderBy(x => x.Category == null || x.Category == string.Empty ? 1 : 0)
            .ThenBy(x => x.Category)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var facets = facetRows
            .Select(x => (string.IsNullOrWhiteSpace(x.Category) ? (string?)null : x.Category, x.Count))
            .ToList();

        if (idsOnly)
        {
            var ids = await joined
                .OrderBy(x => x.product.Name).ThenBy(x => x.product.Id)
                .Select(x => x.product.Id)
                .Take(BuyerProductShareBulkPricing.MaxSelectAllMatching + 1)
                .ToListAsync(ct)
                .ConfigureAwait(false);
            return new BuyerProductShareSearchPage([], ids, matchingCount, eligibleCount, sharedCount, facets);
        }

        // Project scalars + keys only — selecting full entities alongside EXISTS often drops the bool
        // when EF materializes the anonymous type (eligible count stays correct; row Tracking becomes false).
        var pageKeys = await joined
            .OrderBy(x => x.product.Name).ThenBy(x => x.product.Id)
            .Skip(skip).Take(take)
            .Select(x => new
            {
                ProductId = x.product.Id,
                CategoryName = x.categoryRow == null || string.IsNullOrWhiteSpace(x.categoryRow.Name)
                    ? null
                    : x.categoryRow.Name,
                ShareId = x.share != null ? (Guid?)x.share.Id : null,
                ExposureId = x.exposure != null ? (Guid?)x.exposure.Id : null,
                IsInventoryTracked = x.IsInventoryTracked
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var pageProductIds = pageKeys.Select(x => x.ProductId).ToList();
        // Authoritative tracked set for the page — identical keys to EligibleCount (org + product + IsTracked).
        var trackedIds = pageProductIds.Count == 0
            ? new HashSet<Guid>()
            : (await db.InventoryAccounts.AsNoTracking()
                    .Where(a =>
                        a.OrganizationId == supplier.Value
                        && a.IsTracked
                        && pageProductIds.Contains(a.ProductId))
                    .Select(a => a.ProductId)
                    .ToListAsync(ct)
                    .ConfigureAwait(false))
                .ToHashSet();

        var productRows = pageProductIds.Count == 0
            ? []
            : await db.CatalogProducts.AsNoTracking()
                .Where(p => pageProductIds.Contains(p.Id))
                .ToListAsync(ct)
                .ConfigureAwait(false);
        var productsById = productRows.ToDictionary(p => p.Id);

        var shareIds = pageKeys.Where(x => x.ShareId is not null).Select(x => x.ShareId!.Value).Distinct().ToList();
        var shareRows = shareIds.Count == 0
            ? []
            : await db.ConnectedBuyerProductShares.AsNoTracking()
                .Where(s => shareIds.Contains(s.Id))
                .ToListAsync(ct)
                .ConfigureAwait(false);
        var sharesById = shareRows.ToDictionary(s => s.Id);

        var exposureIds = pageKeys.Where(x => x.ExposureId is not null).Select(x => x.ExposureId!.Value).Distinct().ToList();
        var exposureRows = exposureIds.Count == 0
            ? []
            : await db.SupplierProductExposures.AsNoTracking()
                .Where(e => exposureIds.Contains(e.Id))
                .ToListAsync(ct)
                .ConfigureAwait(false);
        var exposuresById = exposureRows.ToDictionary(e => e.Id);

        var rows = new List<BuyerProductShareManagementRow>(pageKeys.Count);
        foreach (var key in pageKeys)
        {
            if (!productsById.TryGetValue(key.ProductId, out var productRecord))
            {
                continue;
            }

            var isTracked = trackedIds.Contains(key.ProductId);
            // Invariant: EligibleCount uses the same tracked set — never emit Eligible without Tracked.
            rows.Add(new BuyerProductShareManagementRow(
                CatalogEntityMapper.ToDomain(productRecord),
                key.ExposureId is Guid eid && exposuresById.TryGetValue(eid, out var exposureRecord)
                    ? ConnectedSupplierEntityMapper.ToDomain(exposureRecord)
                    : null,
                key.ShareId is Guid sid && sharesById.TryGetValue(sid, out var shareRecord)
                    ? ConnectedSupplierEntityMapper.ToDomain(shareRecord)
                    : null,
                key.CategoryName,
                isTracked));
        }

        return new BuyerProductShareSearchPage(
            rows,
            rows.Select(x => x.Product.Id.Value).ToList(),
            matchingCount,
            eligibleCount,
            sharedCount,
            facets);
    }

    private static string NormalizeShareFilter(string? shareFilter)
    {
        var raw = (shareFilter ?? "all").Trim().ToLowerInvariant()
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal);
        return raw switch
        {
            "shared" => "shared",
            "notshared" => "notshared",
            "ineligible" => "ineligible",
            "customprice" => "customprice",
            "blocked" => "blocked",
            _ => "all"
        };
    }

    public Task AddAsync(ConnectedBuyerProductShare x,CancellationToken ct=default)
    {db.ConnectedBuyerProductShares.Add(ConnectedSupplierEntityMapper.ToRecord(x));return Task.CompletedTask;}
    public async Task UpdateAsync(ConnectedBuyerProductShare x,CancellationToken ct=default)
    {var row=await db.ConnectedBuyerProductShares.SingleAsync(y=>y.Id==x.Id.Value,ct);ConnectedSupplierEntityMapper.Apply(x,row);}
    public async Task RemoveAsync(ConnectedBuyerProductShare x, CancellationToken ct = default)
    {
        var row = await db.ConnectedBuyerProductShares
            .SingleOrDefaultAsync(y => y.Id == x.Id.Value, ct)
            .ConfigureAwait(false);
        if (row is not null)
        {
            db.ConnectedBuyerProductShares.Remove(row);
        }
    }

    public async Task<IReadOnlyDictionary<Guid, BuyerRelationshipShareStats>> ListShareStatsByRelationshipsAsync(
        IReadOnlyList<Guid> relationshipIds,
        CancellationToken ct = default)
    {
        if (relationshipIds.Count == 0)
        {
            return new Dictionary<Guid, BuyerRelationshipShareStats>();
        }

        var idSet = relationshipIds.Distinct().ToList();
        var activeStatus = nameof(CatalogProductStatus.Active);

        // Eligible-only aggregates — exclusions on untracked/ineligible products must not
        // inflate ExcludedCount and falsely keep HasSharedCatalog / commerce readiness false
        // after eligible products are re-shared (buyer catalog already shows them).
        var rows = await (
                from share in db.ConnectedBuyerProductShares.AsNoTracking()
                join product in db.CatalogProducts.AsNoTracking()
                    on new { Org = share.SupplierOrganizationId, Pid = share.SupplierProductId }
                    equals new { Org = product.OrganizationId, Pid = product.Id }
                join account in db.InventoryAccounts.AsNoTracking()
                    on new { Org = product.OrganizationId, Pid = product.Id }
                    equals new { Org = account.OrganizationId, Pid = account.ProductId }
                where idSet.Contains(share.RelationshipId)
                      && product.Status == activeStatus
                      && !product.IsBlockedFromConnectedBuyers
                      && product.CanBeSold
                      && account.IsTracked
                group share by share.RelationshipId into g
                select new
                {
                    RelationshipId = g.Key,
                    ExplicitSharedCount = g.Count(x => x.IsShared),
                    ExcludedCount = g.Count(x => !x.IsShared),
                    OverrideCount = g.Count(x => x.IsShared && x.BuyerSpecificPoPrice != null),
                })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows.ToDictionary(
            x => x.RelationshipId,
            x => new BuyerRelationshipShareStats(x.ExplicitSharedCount, x.ExcludedCount, x.OverrideCount));
    }

    public async Task<int> CountEligibleSupplierProductsAsync(PosOrganizationId supplier, CancellationToken ct = default)
    {
        var activeStatus = nameof(CatalogProductStatus.Active);
        return await (
                from product in db.CatalogProducts.AsNoTracking()
                join account in db.InventoryAccounts.AsNoTracking()
                    on new { Org = product.OrganizationId, Pid = product.Id }
                    equals new { Org = account.OrganizationId, Pid = account.ProductId }
                where product.OrganizationId == supplier.Value
                      && product.Status == activeStatus
                      && !product.IsBlockedFromConnectedBuyers
                      && product.CanBeSold
                      && account.IsTracked
                select product.Id)
            .CountAsync(ct)
            .ConfigureAwait(false);
    }
}

internal sealed class BuyerSupplierProductLinkRepository(PosDbContext db) : IBuyerSupplierProductLinkRepository
{
    public async Task<BuyerSupplierProductLink?> GetAsync(BuyerSupplierProductLinkId id,CancellationToken ct=default)
    {var r=await db.BuyerSupplierProductLinks.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id.Value,ct);return r is null?null:ConnectedSupplierEntityMapper.ToDomain(r);}
    public async Task<BuyerSupplierProductLink?> FindAsync(ConnectedSupplierRelationshipId relationshipId,CatalogProductId buyerProductId,CancellationToken ct=default)
    {var r=await db.BuyerSupplierProductLinks.AsNoTracking().SingleOrDefaultAsync(x=>x.RelationshipId==relationshipId.Value&&x.BuyerProductId==buyerProductId.Value&&x.IsActive,ct);return r is null?null:ConnectedSupplierEntityMapper.ToDomain(r);}
    public async Task<BuyerSupplierProductLink?> FindBySupplierProductAsync(ConnectedSupplierRelationshipId relationshipId,CatalogProductId supplierProductId,CancellationToken ct=default)
    {var r=await db.BuyerSupplierProductLinks.AsNoTracking().SingleOrDefaultAsync(x=>x.RelationshipId==relationshipId.Value&&x.SupplierProductId==supplierProductId.Value&&x.IsActive,ct);return r is null?null:ConnectedSupplierEntityMapper.ToDomain(r);}
    public async Task<IReadOnlyList<BuyerSupplierProductLink>> ListAsync(ConnectedSupplierRelationshipId relationshipId,PosOrganizationId buyer,CancellationToken ct=default)=>
        (await db.BuyerSupplierProductLinks.AsNoTracking().Where(x=>x.RelationshipId==relationshipId.Value&&x.BuyerOrganizationId==buyer.Value).OrderBy(x=>x.SupplierNameSnapshot).ToListAsync(ct)).Select(ConnectedSupplierEntityMapper.ToDomain).ToList();
    public async Task<IReadOnlyList<BuyerSupplierProductLink>> DeltaAsync(ConnectedSupplierRelationshipId relationshipId,PosOrganizationId buyer,long sinceVersion,CancellationToken ct=default)=>
        (await db.BuyerSupplierProductLinks.AsNoTracking().Where(x=>x.RelationshipId==relationshipId.Value&&x.BuyerOrganizationId==buyer.Value&&x.SyncVersion>sinceVersion).OrderBy(x=>x.SyncVersion).ToListAsync(ct)).Select(ConnectedSupplierEntityMapper.ToDomain).ToList();
    public Task AddAsync(BuyerSupplierProductLink x,CancellationToken ct=default){db.BuyerSupplierProductLinks.Add(ConnectedSupplierEntityMapper.ToRecord(x));return Task.CompletedTask;}
    public async Task UpdateAsync(BuyerSupplierProductLink x,CancellationToken ct=default){var r=await db.BuyerSupplierProductLinks.SingleAsync(y=>y.Id==x.Id.Value,ct);ConnectedSupplierEntityMapper.Apply(x,r);}
}

internal sealed class ConnectedPurchaseOrderRepository(PosDbContext db) : IConnectedPurchaseOrderRepository
{
    private IQueryable<ConnectedPurchaseOrderRecord> Query()=>db.ConnectedPurchaseOrders.Include(x=>x.Lines);
    public async Task<ConnectedPurchaseOrder?> GetAsync(ConnectedPurchaseOrderId id,CancellationToken ct=default)
    {var r=await Query().AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id.Value,ct);return r is null?null:ConnectedSupplierEntityMapper.ToDomain(r);}
    public async Task<ConnectedPurchaseOrder?> GetByBuyerPurchaseOrderAsync(PurchaseOrderId id,CancellationToken ct=default)
    {var r=await Query().AsNoTracking().SingleOrDefaultAsync(x=>x.BuyerPurchaseOrderId==id.Value,ct);return r is null?null:ConnectedSupplierEntityMapper.ToDomain(r);}
    public async Task<IReadOnlyList<ConnectedPurchaseOrder>> ListIncomingAsync(PosOrganizationId supplier,CancellationToken ct=default)=>
        (await Query().AsNoTracking().Where(x=>x.SupplierOrganizationId==supplier.Value).OrderByDescending(x=>x.CreatedAtUtc).ToListAsync(ct)).Select(ConnectedSupplierEntityMapper.ToDomain).ToList();
    public async Task<IReadOnlyList<ConnectedPurchaseOrder>> ListBetweenOrganizationsAsync(
        PosOrganizationId supplierOrganizationId,
        PosOrganizationId buyerOrganizationId,
        CancellationToken ct = default) =>
        (await Query().AsNoTracking()
            .Where(x => x.SupplierOrganizationId == supplierOrganizationId.Value
                && x.BuyerOrganizationId == buyerOrganizationId.Value)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct))
        .Select(ConnectedSupplierEntityMapper.ToDomain)
        .ToList();
    public Task AddAsync(ConnectedPurchaseOrder x,CancellationToken ct=default){db.ConnectedPurchaseOrders.Add(ConnectedSupplierEntityMapper.ToRecord(x));return Task.CompletedTask;}
    public async Task UpdateAsync(ConnectedPurchaseOrder x,CancellationToken ct=default)
    {
        var r=await db.ConnectedPurchaseOrders
            .Include(y => y.Lines)
            .SingleAsync(y=>y.Id==x.Id.Value,ct);
        var dbStatus=(ConnectedPurchaseOrderStatus)r.Status;
        if(!ConnectedPoDisplayStatus.IsValidConnectedStatusTransition(dbStatus,x.Status))
        {
            throw new PersistenceConflictException(
                ConnectedSupplierDomainErrorCodes.InvalidTransition,
                "Connected purchase order status changed concurrently. Refresh and try again.");
        }

        ConnectedSupplierEntityMapper.Apply(x,r);
    }
}

internal sealed class ConnectedPoInventoryReservationRepository(PosDbContext db) : IConnectedPoInventoryReservationRepository
{
    public async Task<IReadOnlyList<ConnectedPoInventoryReservation>> ListActiveByOrderAsync(
        ConnectedPurchaseOrderId orderId,
        CancellationToken ct = default) =>
        (await db.ConnectedPoInventoryReservations.AsNoTracking()
            .Where(x => x.ConnectedPurchaseOrderId == orderId.Value && x.Status == (int)ConnectedPoReservationStatus.Active)
            .OrderBy(x => x.ProductId)
            .ToListAsync(ct))
        .Select(ConnectedSupplierEntityMapper.ToDomain)
        .ToList();

    public async Task<IReadOnlyList<ConnectedPoInventoryReservation>> ListByOrderAsync(
        ConnectedPurchaseOrderId orderId,
        CancellationToken ct = default) =>
        (await db.ConnectedPoInventoryReservations.AsNoTracking()
            .Where(x => x.ConnectedPurchaseOrderId == orderId.Value)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(ct))
        .Select(ConnectedSupplierEntityMapper.ToDomain)
        .ToList();

    public async Task<IReadOnlyList<ConnectedPoInventoryReservation>> ListByProductBranchAsync(
        PosOrganizationId organizationId,
        CatalogProductId productId,
        PosBranchId branchId,
        CancellationToken ct = default) =>
        (await db.ConnectedPoInventoryReservations.AsNoTracking()
            .Where(x =>
                x.OrganizationId == organizationId.Value
                && x.ProductId == productId.Value
                && x.BranchId == branchId.Value
                && x.Status == (int)ConnectedPoReservationStatus.Active
                && x.RemainingQuantity > 0m)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(ct))
        .Select(ConnectedSupplierEntityMapper.ToDomain)
        .ToList();

    public async Task<IReadOnlyDictionary<Guid, decimal>> SumExpiredStillActiveRemainingByProductAsync(
        PosOrganizationId organizationId,
        PosBranchId branchId,
        IReadOnlyCollection<CatalogProductId> productIds,
        DateTimeOffset utcNow,
        CancellationToken ct = default)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var ids = productIds.Select(p => p.Value).Distinct().ToList();
        var activeStatus = (int)ConnectedPoReservationStatus.Active;
        var rows = await db.ConnectedPoInventoryReservations.AsNoTracking()
            .Where(x =>
                x.OrganizationId == organizationId.Value
                && x.BranchId == branchId.Value
                && ids.Contains(x.ProductId)
                && x.Status == activeStatus
                && x.RemainingQuantity > 0m
                && x.ExpiresAtUtc != null
                && x.ExpiresAtUtc <= utcNow)
            .GroupBy(x => x.ProductId)
            .Select(g => new { ProductId = g.Key, Qty = g.Sum(x => x.RemainingQuantity) })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return rows.ToDictionary(x => x.ProductId, x => x.Qty);
    }

    public Task AddAsync(ConnectedPoInventoryReservation reservation, CancellationToken ct = default)
    {
        db.ConnectedPoInventoryReservations.Add(ConnectedSupplierEntityMapper.ToRecord(reservation));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(ConnectedPoInventoryReservation reservation, CancellationToken ct = default)
    {
        var row = await db.ConnectedPoInventoryReservations.SingleAsync(x => x.Id == reservation.Id.Value, ct);
        if (row.Version != reservation.Version - 1 && row.Version != reservation.Version)
        {
            // Allow same version (idempotent) or expected bump; reject stale concurrent writers.
            if (row.Version > reservation.Version)
            {
                throw new PersistenceConflictException(
                    ConnectedSupplierDomainErrorCodes.InvalidTransition,
                    "Connected PO inventory reservation changed concurrently. Refresh and try again.");
            }
        }

        ConnectedSupplierEntityMapper.Apply(reservation, row);
    }
}
