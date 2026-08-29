using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.ConnectedSuppliers;

internal sealed class ConnectedSupplierRelationshipRepository(PosDbContext db) : IConnectedSupplierRelationshipRepository
{
    public async Task<ConnectedSupplierRelationship?> GetAsync(ConnectedSupplierRelationshipId id,CancellationToken ct=default)
    {var r=await db.ConnectedSupplierRelationships.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id.Value,ct);return r is null?null:ConnectedSupplierEntityMapper.ToDomain(r);}
    public async Task<ConnectedSupplierRelationship?> FindOpenAsync(PosOrganizationId buyer,PosOrganizationId supplier,CancellationToken ct=default)
    {var r=await db.ConnectedSupplierRelationships.AsNoTracking().SingleOrDefaultAsync(x=>x.BuyerOrganizationId==buyer.Value&&x.SupplierOrganizationId==supplier.Value&&(x.Status==0||x.Status==1),ct);return r is null?null:ConnectedSupplierEntityMapper.ToDomain(r);}
    public async Task<IReadOnlyList<ConnectedSupplierRelationship>> ListAsync(PosOrganizationId org,bool supplierView,CancellationToken ct=default)=>
        (await db.ConnectedSupplierRelationships.AsNoTracking().Where(x=>supplierView?x.SupplierOrganizationId==org.Value:x.BuyerOrganizationId==org.Value)
        .OrderByDescending(x=>x.UpdatedAtUtc).ToListAsync(ct)).Select(ConnectedSupplierEntityMapper.ToDomain).ToList();
    public Task AddAsync(ConnectedSupplierRelationship x,CancellationToken ct=default){db.ConnectedSupplierRelationships.Add(ConnectedSupplierEntityMapper.ToRecord(x));return Task.CompletedTask;}
    public async Task UpdateAsync(ConnectedSupplierRelationship x,CancellationToken ct=default){var r=await db.ConnectedSupplierRelationships.SingleAsync(y=>y.Id==x.Id.Value,ct);ConnectedSupplierEntityMapper.Apply(x,r);}
}

internal sealed class SupplierProductExposureRepository(PosDbContext db) : ISupplierProductExposureRepository
{
    public async Task<SupplierProductExposure?> GetAsync(SupplierProductExposureId id,CancellationToken ct=default)
    {var r=await db.SupplierProductExposures.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id.Value,ct);return r is null?null:ConnectedSupplierEntityMapper.ToDomain(r);}
    public async Task<SupplierProductExposure?> GetByProductAsync(PosOrganizationId supplier,CatalogProductId productId,CancellationToken ct=default)
    {var r=await db.SupplierProductExposures.AsNoTracking().SingleOrDefaultAsync(x=>x.SupplierOrganizationId==supplier.Value&&x.ProductId==productId.Value,ct);return r is null?null:ConnectedSupplierEntityMapper.ToDomain(r);}
    public async Task<IReadOnlyList<SupplierProductExposure>> ListAsync(PosOrganizationId supplier,CancellationToken ct=default)=>
        (await db.SupplierProductExposures.AsNoTracking().Where(x=>x.SupplierOrganizationId==supplier.Value).OrderBy(x=>x.NameSnapshot).ToListAsync(ct)).Select(ConnectedSupplierEntityMapper.ToDomain).ToList();
    public async Task<(IReadOnlyList<SupplierProductExposure> Items,int Total)> SearchAsync(PosOrganizationId supplier,string? query,string? category,int skip,int take,CancellationToken ct=default)
    {var q=db.SupplierProductExposures.AsNoTracking().Where(x=>x.SupplierOrganizationId==supplier.Value&&x.IsExposed&&x.IsOrderable);
     if(!string.IsNullOrWhiteSpace(query)){var term=query.Trim().ToUpper();q=q.Where(x=>x.NameSnapshot.ToUpper().Contains(term)||(x.SkuSnapshot!=null&&x.SkuSnapshot.ToUpper().Contains(term)));}
     if(!string.IsNullOrWhiteSpace(category)){var term=category.Trim().ToUpper();q=q.Where(x=>x.CategoryNameSnapshot!=null&&x.CategoryNameSnapshot.ToUpper()==term);}
     var total=await q.CountAsync(ct);var rows=await q.OrderBy(x=>x.NameSnapshot).ThenBy(x=>x.Id).Skip(skip).Take(take).ToListAsync(ct);
     return(rows.Select(ConnectedSupplierEntityMapper.ToDomain).ToList(),total);}
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
                join share in db.ConnectedBuyerProductShares.AsNoTracking()
                        .Where(s => s.RelationshipId == relationshipId.Value)
                    on exposure.ProductId equals share.SupplierProductId into shareGroup
                from share in shareGroup.DefaultIfEmpty()
                where exposure.SupplierOrganizationId == supplier.Value
                      && exposure.IsExposed
                      && exposure.IsOrderable
                      && (share == null || share.IsShared)
                select new { exposure, share };

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
                    x.exposure.CategoryNameSnapshot != null && x.exposure.CategoryNameSnapshot.ToUpper() == term);
            }

            var allTotal = await allEligible.CountAsync(ct).ConfigureAwait(false);
            var allRows = await allEligible
                .OrderBy(x => x.exposure.NameSnapshot).ThenBy(x => x.exposure.Id)
                .Skip(skip).Take(take)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            // Parallel lists: share may be null for inherited AllEligible rows (empty Guid placeholder not used —
            // callers match by product id via dictionary with null-safe GetValueOrDefault).
            var exposures = allRows.Select(x => ConnectedSupplierEntityMapper.ToDomain(x.exposure)).ToList();
            var shares = allRows
                .Where(x => x.share is not null)
                .Select(x => ConnectedSupplierEntityMapper.ToDomain(x.share!))
                .ToList();
            return (exposures, shares, allTotal);
        }

        var q=from exposure in db.SupplierProductExposures.AsNoTracking()
              join share in db.ConnectedBuyerProductShares.AsNoTracking()
                on exposure.ProductId equals share.SupplierProductId
              where exposure.SupplierOrganizationId==supplier.Value&&exposure.IsExposed&&exposure.IsOrderable
                    &&share.RelationshipId==relationshipId.Value&&share.IsShared
              select new { exposure, share };
        if(!string.IsNullOrWhiteSpace(query))
        {
            var term=query.Trim().ToUpper();
            q=q.Where(x=>x.exposure.NameSnapshot.ToUpper().Contains(term)
                ||(x.exposure.SkuSnapshot!=null&&x.exposure.SkuSnapshot.ToUpper().Contains(term)));
        }
        if(!string.IsNullOrWhiteSpace(category))
        {
            var term=category.Trim().ToUpper();
            q=q.Where(x=>x.exposure.CategoryNameSnapshot!=null&&x.exposure.CategoryNameSnapshot.ToUpper()==term);
        }
        var total=await q.CountAsync(ct);
        var rows=await q.OrderBy(x=>x.exposure.NameSnapshot).ThenBy(x=>x.exposure.Id).Skip(skip).Take(take).ToListAsync(ct);
        return(rows.Select(x=>ConnectedSupplierEntityMapper.ToDomain(x.exposure)).ToList(),
            rows.Select(x=>ConnectedSupplierEntityMapper.ToDomain(x.share)).ToList(),total);
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
        CancellationToken ct = default)
    {
        var activeStatus = nameof(CatalogProductStatus.Active);
        var products = db.CatalogProducts.AsNoTracking()
            .Where(x => x.OrganizationId == supplier.Value && x.Status == activeStatus);
        var shares = db.ConnectedBuyerProductShares.AsNoTracking()
            .Where(x => x.RelationshipId == relationshipId.Value);
        var exposures = db.SupplierProductExposures.AsNoTracking()
            .Where(x => x.SupplierOrganizationId == supplier.Value);
        var categories = db.ProductCategories.AsNoTracking();

        var joined =
            from product in products
            join categoryRow in categories on product.CategoryId equals categoryRow.Id into categoryGroup
            from categoryRow in categoryGroup.DefaultIfEmpty()
            join share in shares on product.Id equals share.SupplierProductId into shareGroup
            from share in shareGroup.DefaultIfEmpty()
            join exposure in exposures on product.Id equals exposure.ProductId into exposureGroup
            from exposure in exposureGroup.DefaultIfEmpty()
            select new { product, categoryRow, share, exposure };

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
        joined = filter switch
        {
            "shared" => joined.Where(x => x.share != null && x.share.IsShared),
            "notshared" => joined.Where(x => x.share == null || !x.share.IsShared),
            "customprice" => joined.Where(x =>
                x.share != null && x.share.IsShared && x.share.BuyerSpecificPoPrice != null),
            "blocked" => joined.Where(x => x.product.IsBlockedFromConnectedBuyers),
            _ => joined
        };

        var eligibleCount = await products.CountAsync(x => !x.IsBlockedFromConnectedBuyers, ct)
            .ConfigureAwait(false);
        var sharedCount = await (
            from product in products
            join share in shares on product.Id equals share.SupplierProductId
            where share.IsShared
            select product.Id).CountAsync(ct).ConfigureAwait(false);

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

        var pageRows = await joined
            .OrderBy(x => x.product.Name).ThenBy(x => x.product.Id)
            .Skip(skip).Take(take)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var rows = pageRows.Select(x => new BuyerProductShareManagementRow(
            CatalogEntityMapper.ToDomain(x.product),
            x.exposure is null ? null : ConnectedSupplierEntityMapper.ToDomain(x.exposure),
            x.share is null ? null : ConnectedSupplierEntityMapper.ToDomain(x.share),
            x.categoryRow is null || string.IsNullOrWhiteSpace(x.categoryRow.Name)
                ? null
                : x.categoryRow.Name)).ToList();

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
            "customprice" => "customprice",
            "blocked" => "blocked",
            _ => "all"
        };
    }

    public Task AddAsync(ConnectedBuyerProductShare x,CancellationToken ct=default)
    {db.ConnectedBuyerProductShares.Add(ConnectedSupplierEntityMapper.ToRecord(x));return Task.CompletedTask;}
    public async Task UpdateAsync(ConnectedBuyerProductShare x,CancellationToken ct=default)
    {var row=await db.ConnectedBuyerProductShares.SingleAsync(y=>y.Id==x.Id.Value,ct);ConnectedSupplierEntityMapper.Apply(x,row);}
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
    public Task AddAsync(ConnectedPurchaseOrder x,CancellationToken ct=default){db.ConnectedPurchaseOrders.Add(ConnectedSupplierEntityMapper.ToRecord(x));return Task.CompletedTask;}
    public async Task UpdateAsync(ConnectedPurchaseOrder x,CancellationToken ct=default)
    {
        var r=await db.ConnectedPurchaseOrders.SingleAsync(y=>y.Id==x.Id.Value,ct);
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
