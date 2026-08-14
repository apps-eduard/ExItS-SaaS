using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

public static class ConnectedSupplierErrorCodes
{
    public const string NotFound = "pos.connected_supplier.not_found";
    public const string DuplicateRelationship = "pos.connected_supplier.relationship.duplicate";
    public const string RelationshipInactive = "pos.connected_supplier.relationship.inactive";
    public const string ExposureNotFound = "pos.connected_supplier.exposure.not_found";
    public const string LinkNotFound = "pos.connected_supplier.link.not_found";
    public const string IncomingOrderNotFound = "pos.connected_supplier.incoming_order.not_found";
    public const string OrganizationMismatch = "pos.connected_supplier.organization_mismatch";
}

public sealed record ConnectedSupplierRelationshipDto(Guid RelationshipId, Guid BuyerOrganizationId, Guid SupplierOrganizationId,
    string Status, DateTimeOffset RequestedAtUtc, Guid? RequestedByUserId, DateTimeOffset? RespondedAtUtc,
    Guid? RespondedByUserId, DateTimeOffset? DisconnectedAtUtc, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);
public sealed record RequestConnectionRequest(Guid SupplierOrganizationId, Guid? RequestedByUserId = null);
public sealed record RespondConnectionRequest(Guid? RespondedByUserId = null);
public sealed record SupplierProductExposureDto(Guid ExposureId, Guid SupplierOrganizationId, Guid ProductId, string? SkuSnapshot,
    string NameSnapshot, string? CategoryNameSnapshot, string UnitOfMeasureCode, decimal SupplierOrderPrice,
    bool IsOrderable, bool IsExposed, long SyncVersion, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);
public sealed record ExposeProductRequest(Guid ProductId, decimal SupplierOrderPrice, bool IsOrderable = true);
public sealed record UpdateExposureRequest(decimal SupplierOrderPrice, bool IsOrderable, bool IsExposed);
public sealed record BuyerSupplierProductLinkDto(Guid LinkId, Guid RelationshipId, Guid BuyerOrganizationId,
    Guid SupplierOrganizationId, Guid BuyerProductId, Guid SupplierProductId, string? SupplierSkuSnapshot,
    string SupplierNameSnapshot, string UnitOfMeasureCode, decimal LastKnownOrderPrice, bool IsActive,
    long SyncVersion, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc);
public sealed record LinkProductRequest(Guid BuyerProductId, Guid ExposureId);
public sealed record LinkedProductsDeltaDto(IReadOnlyList<BuyerSupplierProductLinkDto> Changed, IReadOnlyList<Guid> RemovedIds, long Cursor);
public sealed record ConnectedPurchaseOrderLineDto(Guid ProductId, string NameSnapshot, string? SkuSnapshot,
    decimal Qty, decimal UnitPriceSnapshot, decimal LineTotal, string UnitOfMeasureCode);
public sealed record ConnectedPurchaseOrderDto(Guid ConnectedPurchaseOrderId, Guid RelationshipId, Guid BuyerOrganizationId,
    Guid SupplierOrganizationId, Guid BuyerPurchaseOrderId, string? BuyerPoNumber, DateOnly OrderDate, string? Notes,
    string Status, decimal TotalAmount, DateTimeOffset CreatedAtUtc, DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? AcceptedAtUtc, DateTimeOffset? DeclinedAtUtc, IReadOnlyList<ConnectedPurchaseOrderLineDto> Lines);
public sealed record DraftReviewLineRequest(Guid SupplierProductId, decimal UnitPriceSnapshot);
public sealed record RevalidateConnectedPoDraftRequest(IReadOnlyList<DraftReviewLineRequest> Lines);
public enum ConnectedPoDraftReviewStatus { Unchanged, PriceChanged, Unavailable, RelationshipInactive }
public sealed record ConnectedPoDraftReviewItem(Guid SupplierProductId, ConnectedPoDraftReviewStatus Status,
    decimal SubmittedPrice, decimal? CurrentPrice);
public sealed record ConnectedPoDraftReviewDto(ConnectedPoDraftReviewStatus OverallStatus, IReadOnlyList<ConnectedPoDraftReviewItem> Items);

public static class ConnectedSupplierMapper
{
    public static ConnectedSupplierRelationshipDto Map(ConnectedSupplierRelationship x) => new(x.Id.Value,
        x.BuyerOrganizationId.Value,x.SupplierOrganizationId.Value,x.Status.ToString(),x.RequestedAtUtc,x.RequestedByUserId,
        x.RespondedAtUtc,x.RespondedByUserId,x.DisconnectedAtUtc,x.CreatedAtUtc,x.UpdatedAtUtc);
    public static SupplierProductExposureDto Map(SupplierProductExposure x) => new(x.Id.Value,x.SupplierOrganizationId.Value,
        x.ProductId.Value,x.SkuSnapshot,x.NameSnapshot,x.CategoryNameSnapshot,x.UnitOfMeasureCode,x.SupplierOrderPrice,
        x.IsOrderable,x.IsExposed,x.SyncVersion,x.CreatedAtUtc,x.UpdatedAtUtc);
    public static BuyerSupplierProductLinkDto Map(BuyerSupplierProductLink x) => new(x.Id.Value,x.RelationshipId.Value,
        x.BuyerOrganizationId.Value,x.SupplierOrganizationId.Value,x.BuyerProductId.Value,x.SupplierProductId.Value,
        x.SupplierSkuSnapshot,x.SupplierNameSnapshot,x.UnitOfMeasureCode,x.LastKnownOrderPrice,x.IsActive,x.SyncVersion,x.CreatedAtUtc,x.UpdatedAtUtc);
    public static ConnectedPurchaseOrderDto Map(ConnectedPurchaseOrder x) => new(x.Id.Value,x.RelationshipId.Value,
        x.BuyerOrganizationId.Value,x.SupplierOrganizationId.Value,x.BuyerPurchaseOrderId.Value,x.BuyerPoNumber,x.OrderDate,
        x.Notes,x.Status.ToString(),x.TotalAmount,x.CreatedAtUtc,x.UpdatedAtUtc,x.AcceptedAtUtc,x.DeclinedAtUtc,
        x.Lines.Select(l=>new ConnectedPurchaseOrderLineDto(l.ProductId.Value,l.NameSnapshot,l.SkuSnapshot,l.Qty,
            l.UnitPriceSnapshot,l.LineTotal,l.UnitOfMeasureCode)).ToList());
}

internal static class ConnectedSupplierUseCaseGuard
{
    public static ApplicationResult Access(IPosCommercialAccessAccessor access, UtangCapability capability) =>
        CommercialAccessGuard.Require(access, capability);
    public static bool BelongsTo(ConnectedSupplierRelationship r, PosOrganizationId org) =>
        r.BuyerOrganizationId == org || r.SupplierOrganizationId == org;
    public static ApplicationResult<T> Failure<T>(string code,string message) => ApplicationResult<T>.Failure(code,message);
}

public sealed class RequestConnection
{
    private readonly IConnectedSupplierRelationshipRepository _relationships; private readonly IPosUnitOfWork _uow;
    private readonly IPosCommercialAccessAccessor _access; private readonly TimeProvider _clock;
    public RequestConnection(IConnectedSupplierRelationshipRepository relationships,IPosUnitOfWork uow,IPosCommercialAccessAccessor access,TimeProvider? clock=null)
    { _relationships=relationships;_uow=uow;_access=access;_clock=clock??TimeProvider.System; }
    public async Task<ApplicationResult<ConnectedSupplierRelationshipDto>> ExecuteAsync(Guid organizationId,RequestConnectionRequest request,CancellationToken ct=default)
    {
        var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ManageSuppliers);
        if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(gate.ErrorCode!,gate.ErrorMessage!);
        try {
            var buyer=PosOrganizationId.From(organizationId);var supplier=PosOrganizationId.From(request.SupplierOrganizationId);
            if(await _relationships.FindOpenAsync(buyer,supplier,ct) is not null)
                return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(ConnectedSupplierErrorCodes.DuplicateRelationship,"A pending or active relationship already exists.");
            var relationship=ConnectedSupplierRelationship.Request(buyer,supplier,_clock.GetUtcNow(),request.RequestedByUserId);
            await _relationships.AddAsync(relationship,ct);await _uow.SaveChangesAsync(ct);
            return ApplicationResult<ConnectedSupplierRelationshipDto>.Success(ConnectedSupplierMapper.Map(relationship));
        } catch(DomainException ex){return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(ex.ErrorCode,ex.Message);}
    }
}

public sealed class RespondConnection
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;private readonly IPosUnitOfWork _uow;
    private readonly IPosCommercialAccessAccessor _access;private readonly TimeProvider _clock;
    public RespondConnection(IConnectedSupplierRelationshipRepository r,IPosUnitOfWork u,IPosCommercialAccessAccessor a,TimeProvider? c=null)
    {_relationships=r;_uow=u;_access=a;_clock=c??TimeProvider.System;}
    public async Task<ApplicationResult<ConnectedSupplierRelationshipDto>> ExecuteAsync(Guid orgId,Guid relationshipId,bool approve,RespondConnectionRequest request,CancellationToken ct=default)
    {
        var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ManageSuppliers);
        if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(gate.ErrorCode!,gate.ErrorMessage!);
        var r=await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(relationshipId),ct);
        var org=PosOrganizationId.From(orgId);
        if(r is null||r.SupplierOrganizationId!=org)return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(ConnectedSupplierErrorCodes.NotFound,"Relationship was not found.");
        try {if(approve)r.Approve(_clock.GetUtcNow(),request.RespondedByUserId);else r.Decline(_clock.GetUtcNow(),request.RespondedByUserId);
            await _relationships.UpdateAsync(r,ct);await _uow.SaveChangesAsync(ct);return ApplicationResult<ConnectedSupplierRelationshipDto>.Success(ConnectedSupplierMapper.Map(r));
        }catch(DomainException ex){return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(ex.ErrorCode,ex.Message);}
    }
}

public sealed class DisconnectConnectedSupplier
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;private readonly IPosUnitOfWork _uow;
    private readonly IPosCommercialAccessAccessor _access;private readonly TimeProvider _clock;
    public DisconnectConnectedSupplier(IConnectedSupplierRelationshipRepository r,IPosUnitOfWork u,IPosCommercialAccessAccessor a,TimeProvider? c=null)
    {_relationships=r;_uow=u;_access=a;_clock=c??TimeProvider.System;}
    public async Task<ApplicationResult<ConnectedSupplierRelationshipDto>> ExecuteAsync(Guid orgId,Guid id,CancellationToken ct=default)
    {
        var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ManageSuppliers);
        if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(gate.ErrorCode!,gate.ErrorMessage!);
        var r=await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(id),ct);var org=PosOrganizationId.From(orgId);
        if(r is null||!ConnectedSupplierUseCaseGuard.BelongsTo(r,org))return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(ConnectedSupplierErrorCodes.NotFound,"Relationship was not found.");
        try {r.Disconnect(_clock.GetUtcNow());await _relationships.UpdateAsync(r,ct);await _uow.SaveChangesAsync(ct);
            return ApplicationResult<ConnectedSupplierRelationshipDto>.Success(ConnectedSupplierMapper.Map(r));}
        catch(DomainException ex){return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierRelationshipDto>(ex.ErrorCode,ex.Message);}
    }
}

public sealed class ListRelationships
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;private readonly IPosCommercialAccessAccessor _access;
    public ListRelationships(IConnectedSupplierRelationshipRepository r,IPosCommercialAccessAccessor a){_relationships=r;_access=a;}
    public async Task<ApplicationResult<IReadOnlyList<ConnectedSupplierRelationshipDto>>> ExecuteAsync(Guid orgId,bool supplierView,CancellationToken ct=default)
    {var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ViewSuppliers);
     if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<ConnectedSupplierRelationshipDto>>(gate.ErrorCode!,gate.ErrorMessage!);
     var items=await _relationships.ListAsync(PosOrganizationId.From(orgId),supplierView,ct);
     return ApplicationResult<IReadOnlyList<ConnectedSupplierRelationshipDto>>.Success(items.Select(ConnectedSupplierMapper.Map).ToList());}
}

public sealed class ExposeProduct
{
    private readonly ISupplierProductExposureRepository _exposures;private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _uow;private readonly IPosCommercialAccessAccessor _access;private readonly TimeProvider _clock;
    public ExposeProduct(ISupplierProductExposureRepository e,ICatalogProductRepository p,IPosUnitOfWork u,IPosCommercialAccessAccessor a,TimeProvider? c=null)
    {_exposures=e;_products=p;_uow=u;_access=a;_clock=c??TimeProvider.System;}
    public async Task<ApplicationResult<SupplierProductExposureDto>> ExecuteAsync(Guid orgId,ExposeProductRequest request,CancellationToken ct=default)
    {
        var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ManageSuppliers);
        if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<SupplierProductExposureDto>(gate.ErrorCode!,gate.ErrorMessage!);
        var org=PosOrganizationId.From(orgId);var product=await _products.GetByIdAsync(org,CatalogProductId.From(request.ProductId),ct);
        if(product is null)return ConnectedSupplierUseCaseGuard.Failure<SupplierProductExposureDto>(ApplicationErrorCodes.ProductNotFound,"Product was not found.");
        try {var existing=await _exposures.GetByProductAsync(org,product.Id,ct);
            if(existing is null){existing=SupplierProductExposure.Expose(org,product.Id,product.Name,product.UnitOfMeasure.ToString(),
                request.SupplierOrderPrice,_clock.GetUtcNow(),product.Sku);if(!request.IsOrderable)existing.MarkNotOrderable(_clock.GetUtcNow());
                await _exposures.AddAsync(existing,ct);}
            else {existing.UpdateOffer(product.Name,product.UnitOfMeasure.ToString(),request.SupplierOrderPrice,request.IsOrderable,_clock.GetUtcNow(),product.Sku);
                await _exposures.UpdateAsync(existing,ct);}
            await _uow.SaveChangesAsync(ct);return ApplicationResult<SupplierProductExposureDto>.Success(ConnectedSupplierMapper.Map(existing));
        }catch(DomainException ex){return ConnectedSupplierUseCaseGuard.Failure<SupplierProductExposureDto>(ex.ErrorCode,ex.Message);}
    }
}

public sealed class UpdateExposure
{
    private readonly ISupplierProductExposureRepository _exposures;private readonly IPosUnitOfWork _uow;
    private readonly IPosCommercialAccessAccessor _access;private readonly TimeProvider _clock;
    public UpdateExposure(ISupplierProductExposureRepository e,IPosUnitOfWork u,IPosCommercialAccessAccessor a,TimeProvider? c=null)
    {_exposures=e;_uow=u;_access=a;_clock=c??TimeProvider.System;}
    public async Task<ApplicationResult<SupplierProductExposureDto>> ExecuteAsync(Guid orgId,Guid id,UpdateExposureRequest request,CancellationToken ct=default)
    {var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ManageSuppliers);
     if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<SupplierProductExposureDto>(gate.ErrorCode!,gate.ErrorMessage!);
     var e=await _exposures.GetAsync(SupplierProductExposureId.From(id),ct);var org=PosOrganizationId.From(orgId);
     if(e is null||e.SupplierOrganizationId!=org)return ConnectedSupplierUseCaseGuard.Failure<SupplierProductExposureDto>(ConnectedSupplierErrorCodes.ExposureNotFound,"Exposure was not found.");
     try {if(!request.IsExposed)e.Deactivate(_clock.GetUtcNow());else e.UpdateOffer(e.NameSnapshot,e.UnitOfMeasureCode,request.SupplierOrderPrice,request.IsOrderable,_clock.GetUtcNow(),e.SkuSnapshot,e.CategoryNameSnapshot);
       await _exposures.UpdateAsync(e,ct);await _uow.SaveChangesAsync(ct);return ApplicationResult<SupplierProductExposureDto>.Success(ConnectedSupplierMapper.Map(e));}
     catch(DomainException ex){return ConnectedSupplierUseCaseGuard.Failure<SupplierProductExposureDto>(ex.ErrorCode,ex.Message);}}
}

public sealed class ListExposures
{
    private readonly ISupplierProductExposureRepository _exposures;private readonly IPosCommercialAccessAccessor _access;
    public ListExposures(ISupplierProductExposureRepository e,IPosCommercialAccessAccessor a){_exposures=e;_access=a;}
    public async Task<ApplicationResult<IReadOnlyList<SupplierProductExposureDto>>> ExecuteAsync(Guid orgId,CancellationToken ct=default)
    {var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ViewSuppliers);
     if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<SupplierProductExposureDto>>(gate.ErrorCode!,gate.ErrorMessage!);
     var x=await _exposures.ListAsync(PosOrganizationId.From(orgId),ct);return ApplicationResult<IReadOnlyList<SupplierProductExposureDto>>.Success(x.Select(ConnectedSupplierMapper.Map).ToList());}
}

public sealed class SearchExposedCatalog
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;private readonly ISupplierProductExposureRepository _exposures;
    private readonly IPosCommercialAccessAccessor _access;
    public SearchExposedCatalog(IConnectedSupplierRelationshipRepository r,ISupplierProductExposureRepository e,IPosCommercialAccessAccessor a)
    {_relationships=r;_exposures=e;_access=a;}
    public async Task<ApplicationResult<PagedResult<SupplierProductExposureDto>>> ExecuteAsync(Guid orgId,Guid relationshipId,string? query,string? category,int? page,int? pageSize,CancellationToken ct=default)
    {var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ViewPurchasing);
     if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<PagedResult<SupplierProductExposureDto>>(gate.ErrorCode!,gate.ErrorMessage!);
     var r=await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(relationshipId),ct);var buyer=PosOrganizationId.From(orgId);
     if(r is null||r.BuyerOrganizationId!=buyer)return ConnectedSupplierUseCaseGuard.Failure<PagedResult<SupplierProductExposureDto>>(ConnectedSupplierErrorCodes.NotFound,"Relationship was not found.");
     if(r.Status!=ConnectedSupplierRelationshipStatus.Active)return ConnectedSupplierUseCaseGuard.Failure<PagedResult<SupplierProductExposureDto>>(ConnectedSupplierErrorCodes.RelationshipInactive,"Relationship is not active.");
     var p=Math.Max(page??1,1);var size=Math.Clamp(pageSize??25,1,50);var (items,total)=await _exposures.SearchAsync(r.SupplierOrganizationId,query,category,(p-1)*size,size,ct);
     return ApplicationResult<PagedResult<SupplierProductExposureDto>>.Success(new(items.Select(ConnectedSupplierMapper.Map).ToList(),total,p,size));}
}

public sealed class LinkProduct
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;private readonly ISupplierProductExposureRepository _exposures;
    private readonly IBuyerSupplierProductLinkRepository _links;private readonly ICatalogProductRepository _products;
    private readonly IPosUnitOfWork _uow;private readonly IPosCommercialAccessAccessor _access;private readonly TimeProvider _clock;
    public LinkProduct(IConnectedSupplierRelationshipRepository r,ISupplierProductExposureRepository e,IBuyerSupplierProductLinkRepository l,
        ICatalogProductRepository p,IPosUnitOfWork u,IPosCommercialAccessAccessor a,TimeProvider? c=null)
    {_relationships=r;_exposures=e;_links=l;_products=p;_uow=u;_access=a;_clock=c??TimeProvider.System;}
    public async Task<ApplicationResult<BuyerSupplierProductLinkDto>> ExecuteAsync(Guid orgId,Guid relationshipId,LinkProductRequest request,CancellationToken ct=default)
    {var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ManagePurchasing);
     if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<BuyerSupplierProductLinkDto>(gate.ErrorCode!,gate.ErrorMessage!);
     var buyer=PosOrganizationId.From(orgId);var r=await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(relationshipId),ct);
     if(r is null||r.BuyerOrganizationId!=buyer||r.Status!=ConnectedSupplierRelationshipStatus.Active)return ConnectedSupplierUseCaseGuard.Failure<BuyerSupplierProductLinkDto>(ConnectedSupplierErrorCodes.NotFound,"Active relationship was not found.");
     var product=await _products.GetByIdAsync(buyer,CatalogProductId.From(request.BuyerProductId),ct);
     var exposure=await _exposures.GetAsync(SupplierProductExposureId.From(request.ExposureId),ct);
     if(product is null||exposure is null||exposure.SupplierOrganizationId!=r.SupplierOrganizationId||!exposure.IsExposed)
       return ConnectedSupplierUseCaseGuard.Failure<BuyerSupplierProductLinkDto>(ConnectedSupplierErrorCodes.ExposureNotFound,"Exposure was not found.");
     var existing=await _links.FindAsync(r.Id,product.Id,ct);if(existing is not null)return ApplicationResult<BuyerSupplierProductLinkDto>.Success(ConnectedSupplierMapper.Map(existing));
     var link=BuyerSupplierProductLink.Create(r.Id,buyer,r.SupplierOrganizationId,product.Id,exposure,_clock.GetUtcNow());
     await _links.AddAsync(link,ct);await _uow.SaveChangesAsync(ct);return ApplicationResult<BuyerSupplierProductLinkDto>.Success(ConnectedSupplierMapper.Map(link));}
}

public sealed class UnlinkProduct
{
    private readonly IBuyerSupplierProductLinkRepository _links;private readonly IPosUnitOfWork _uow;private readonly IPosCommercialAccessAccessor _access;private readonly TimeProvider _clock;
    public UnlinkProduct(IBuyerSupplierProductLinkRepository l,IPosUnitOfWork u,IPosCommercialAccessAccessor a,TimeProvider? c=null){_links=l;_uow=u;_access=a;_clock=c??TimeProvider.System;}
    public async Task<ApplicationResult<BuyerSupplierProductLinkDto>> ExecuteAsync(Guid orgId,Guid id,CancellationToken ct=default)
    {var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ManagePurchasing);if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<BuyerSupplierProductLinkDto>(gate.ErrorCode!,gate.ErrorMessage!);
     var link=await _links.GetAsync(BuyerSupplierProductLinkId.From(id),ct);if(link is null||link.BuyerOrganizationId!=PosOrganizationId.From(orgId))return ConnectedSupplierUseCaseGuard.Failure<BuyerSupplierProductLinkDto>(ConnectedSupplierErrorCodes.LinkNotFound,"Link was not found.");
     link.Unlink(_clock.GetUtcNow());await _links.UpdateAsync(link,ct);await _uow.SaveChangesAsync(ct);return ApplicationResult<BuyerSupplierProductLinkDto>.Success(ConnectedSupplierMapper.Map(link));}
}

public sealed class ListLinks
{
    private readonly IBuyerSupplierProductLinkRepository _links;private readonly IPosCommercialAccessAccessor _access;
    public ListLinks(IBuyerSupplierProductLinkRepository l,IPosCommercialAccessAccessor a){_links=l;_access=a;}
    public async Task<ApplicationResult<IReadOnlyList<BuyerSupplierProductLinkDto>>> ExecuteAsync(Guid orgId,Guid relationshipId,CancellationToken ct=default)
    {var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ViewPurchasing);if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<BuyerSupplierProductLinkDto>>(gate.ErrorCode!,gate.ErrorMessage!);
     var x=await _links.ListAsync(ConnectedSupplierRelationshipId.From(relationshipId),PosOrganizationId.From(orgId),ct);return ApplicationResult<IReadOnlyList<BuyerSupplierProductLinkDto>>.Success(x.Select(ConnectedSupplierMapper.Map).ToList());}
}

public sealed class SyncLinkedProductsDelta
{
    private readonly IBuyerSupplierProductLinkRepository _links;private readonly IPosCommercialAccessAccessor _access;
    public SyncLinkedProductsDelta(IBuyerSupplierProductLinkRepository l,IPosCommercialAccessAccessor a){_links=l;_access=a;}
    public async Task<ApplicationResult<LinkedProductsDeltaDto>> ExecuteAsync(Guid orgId,Guid relationshipId,long sinceVersion,CancellationToken ct=default)
    {var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ViewPurchasing);if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<LinkedProductsDeltaDto>(gate.ErrorCode!,gate.ErrorMessage!);
     var x=await _links.DeltaAsync(ConnectedSupplierRelationshipId.From(relationshipId),PosOrganizationId.From(orgId),Math.Max(0,sinceVersion),ct);
     var changed=x.Where(i=>i.IsActive).Select(ConnectedSupplierMapper.Map).ToList();var removed=x.Where(i=>!i.IsActive).Select(i=>i.Id.Value).ToList();
     return ApplicationResult<LinkedProductsDeltaDto>.Success(new(changed,removed,x.Count==0?sinceVersion:x.Max(i=>i.SyncVersion)));}
}

public sealed class SupplierIncomingOrderQuery
{
    private readonly IConnectedPurchaseOrderRepository _orders;private readonly IPosCommercialAccessAccessor _access;
    public SupplierIncomingOrderQuery(IConnectedPurchaseOrderRepository o,IPosCommercialAccessAccessor a){_orders=o;_access=a;}
    public async Task<ApplicationResult<IReadOnlyList<ConnectedPurchaseOrderDto>>> ExecuteAsync(Guid orgId,CancellationToken ct=default)
    {var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ViewPurchasing);if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<IReadOnlyList<ConnectedPurchaseOrderDto>>(gate.ErrorCode!,gate.ErrorMessage!);
     var x=await _orders.ListIncomingAsync(PosOrganizationId.From(orgId),ct);return ApplicationResult<IReadOnlyList<ConnectedPurchaseOrderDto>>.Success(x.Select(ConnectedSupplierMapper.Map).ToList());}
}

public sealed class RespondIncomingOrder
{
    private readonly IConnectedPurchaseOrderRepository _orders;private readonly IPosUnitOfWork _uow;private readonly IPosCommercialAccessAccessor _access;private readonly TimeProvider _clock;
    public RespondIncomingOrder(IConnectedPurchaseOrderRepository o,IPosUnitOfWork u,IPosCommercialAccessAccessor a,TimeProvider? c=null){_orders=o;_uow=u;_access=a;_clock=c??TimeProvider.System;}
    public async Task<ApplicationResult<ConnectedPurchaseOrderDto>> ExecuteAsync(Guid orgId,Guid id,bool accept,CancellationToken ct=default)
    {var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ManagePurchasing);if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<ConnectedPurchaseOrderDto>(gate.ErrorCode!,gate.ErrorMessage!);
     var o=await _orders.GetAsync(ConnectedPurchaseOrderId.From(id),ct);if(o is null||o.SupplierOrganizationId!=PosOrganizationId.From(orgId))return ConnectedSupplierUseCaseGuard.Failure<ConnectedPurchaseOrderDto>(ConnectedSupplierErrorCodes.IncomingOrderNotFound,"Incoming order was not found.");
     try {if(accept)o.Accept(_clock.GetUtcNow());else o.Decline(_clock.GetUtcNow());await _orders.UpdateAsync(o,ct);await _uow.SaveChangesAsync(ct);
       return ApplicationResult<ConnectedPurchaseOrderDto>.Success(ConnectedSupplierMapper.Map(o));}
     catch(DomainException ex){return ConnectedSupplierUseCaseGuard.Failure<ConnectedPurchaseOrderDto>(ex.ErrorCode,ex.Message);}}
}
public sealed class AcceptIncoming(RespondIncomingOrder inner)
{ public Task<ApplicationResult<ConnectedPurchaseOrderDto>> ExecuteAsync(Guid orgId,Guid id,CancellationToken ct=default)=>inner.ExecuteAsync(orgId,id,true,ct); }
public sealed class DeclineIncoming(RespondIncomingOrder inner)
{ public Task<ApplicationResult<ConnectedPurchaseOrderDto>> ExecuteAsync(Guid orgId,Guid id,CancellationToken ct=default)=>inner.ExecuteAsync(orgId,id,false,ct); }

public sealed class RevalidateConnectedPoDraft
{
    private readonly IConnectedSupplierRelationshipRepository _relationships;private readonly ISupplierProductExposureRepository _exposures;private readonly IPosCommercialAccessAccessor _access;
    public RevalidateConnectedPoDraft(IConnectedSupplierRelationshipRepository r,ISupplierProductExposureRepository e,IPosCommercialAccessAccessor a){_relationships=r;_exposures=e;_access=a;}
    public async Task<ApplicationResult<ConnectedPoDraftReviewDto>> ExecuteAsync(Guid orgId,Guid relationshipId,RevalidateConnectedPoDraftRequest request,CancellationToken ct=default)
    {var gate=ConnectedSupplierUseCaseGuard.Access(_access,UtangCapability.ViewPurchasing);if(!gate.IsSuccess)return ConnectedSupplierUseCaseGuard.Failure<ConnectedPoDraftReviewDto>(gate.ErrorCode!,gate.ErrorMessage!);
     var r=await _relationships.GetAsync(ConnectedSupplierRelationshipId.From(relationshipId),ct);if(r is null||r.BuyerOrganizationId!=PosOrganizationId.From(orgId))return ConnectedSupplierUseCaseGuard.Failure<ConnectedPoDraftReviewDto>(ConnectedSupplierErrorCodes.NotFound,"Relationship was not found.");
     if(r.Status!=ConnectedSupplierRelationshipStatus.Active)return ApplicationResult<ConnectedPoDraftReviewDto>.Success(new(ConnectedPoDraftReviewStatus.RelationshipInactive,
       request.Lines.Select(l=>new ConnectedPoDraftReviewItem(l.SupplierProductId,ConnectedPoDraftReviewStatus.RelationshipInactive,l.UnitPriceSnapshot,null)).ToList()));
     var items=new List<ConnectedPoDraftReviewItem>();foreach(var line in request.Lines){var e=await _exposures.GetByProductAsync(r.SupplierOrganizationId,CatalogProductId.From(line.SupplierProductId),ct);
       var status=e is null||!e.IsExposed||!e.IsOrderable?ConnectedPoDraftReviewStatus.Unavailable:e.SupplierOrderPrice!=Domain.Sales.SaleMoney.RoundMoney(line.UnitPriceSnapshot)?ConnectedPoDraftReviewStatus.PriceChanged:ConnectedPoDraftReviewStatus.Unchanged;
       items.Add(new(line.SupplierProductId,status,line.UnitPriceSnapshot,e?.SupplierOrderPrice));}
     var overall=items.Any(i=>i.Status==ConnectedPoDraftReviewStatus.Unavailable)?ConnectedPoDraftReviewStatus.Unavailable:
       items.Any(i=>i.Status==ConnectedPoDraftReviewStatus.PriceChanged)?ConnectedPoDraftReviewStatus.PriceChanged:ConnectedPoDraftReviewStatus.Unchanged;
     return ApplicationResult<ConnectedPoDraftReviewDto>.Success(new(overall,items));}
}
