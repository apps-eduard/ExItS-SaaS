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

internal sealed class BuyerSupplierProductLinkRepository(PosDbContext db) : IBuyerSupplierProductLinkRepository
{
    public async Task<BuyerSupplierProductLink?> GetAsync(BuyerSupplierProductLinkId id,CancellationToken ct=default)
    {var r=await db.BuyerSupplierProductLinks.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==id.Value,ct);return r is null?null:ConnectedSupplierEntityMapper.ToDomain(r);}
    public async Task<BuyerSupplierProductLink?> FindAsync(ConnectedSupplierRelationshipId relationshipId,CatalogProductId buyerProductId,CancellationToken ct=default)
    {var r=await db.BuyerSupplierProductLinks.AsNoTracking().SingleOrDefaultAsync(x=>x.RelationshipId==relationshipId.Value&&x.BuyerProductId==buyerProductId.Value&&x.IsActive,ct);return r is null?null:ConnectedSupplierEntityMapper.ToDomain(r);}
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
    public async Task UpdateAsync(ConnectedPurchaseOrder x,CancellationToken ct=default){var r=await db.ConnectedPurchaseOrders.SingleAsync(y=>y.Id==x.Id.Value,ct);ConnectedSupplierEntityMapper.Apply(x,r);}
}
