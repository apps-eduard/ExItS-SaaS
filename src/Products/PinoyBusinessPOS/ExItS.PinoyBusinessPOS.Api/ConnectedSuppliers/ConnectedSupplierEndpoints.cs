using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

namespace ExItS.PinoyBusinessPOS.Api.ConnectedSuppliers;

internal static class ConnectedSupplierEndpoints
{
    public static IEndpointRouteBuilder MapConnectedSupplierEndpoints(this IEndpointRouteBuilder app)
    {
        var group=app.MapGroup("/api/v1/pos/connected-suppliers");
        group.MapPost("/relationships/request",async(HttpRequest req,RequestConnectionRequest body,RequestConnection use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ManageSuppliers,out var org,out var problem))return problem!;
         return PosApiResults.FromResult(await use.ExecuteAsync(org,body,ct),x=>Results.Created($"/api/v1/pos/connected-suppliers/relationships/{x.RelationshipId:D}",x));});
        group.MapPost("/relationships/{id:guid}/approve",async(HttpRequest req,Guid id,RespondConnectionRequest body,RespondConnection use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ManageSuppliers,out var org,out var problem))return problem!;return PosApiResults.FromResult(await use.ExecuteAsync(org,id,true,body,ct),Results.Ok);});
        group.MapPost("/relationships/{id:guid}/decline",async(HttpRequest req,Guid id,RespondConnectionRequest body,RespondConnection use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ManageSuppliers,out var org,out var problem))return problem!;return PosApiResults.FromResult(await use.ExecuteAsync(org,id,false,body,ct),Results.Ok);});
        group.MapPost("/relationships/{id:guid}/disconnect",async(HttpRequest req,Guid id,DisconnectConnectedSupplier use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ManageSuppliers,out var org,out var problem))return problem!;return PosApiResults.FromResult(await use.ExecuteAsync(org,id,ct),Results.Ok);});
        group.MapGet("/relationships",async(HttpRequest req,string? view,ListRelationships use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ViewSuppliers,out var org,out var problem))return problem!;
         return PosApiResults.FromResult(await use.ExecuteAsync(org,string.Equals(view,"supplier",StringComparison.OrdinalIgnoreCase),ct),Results.Ok);});
        group.MapGet("/relationships/{id:guid}/catalog",async(HttpRequest req,Guid id,string? query,string? category,int? page,int? pageSize,SearchExposedCatalog use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ViewPurchasing,out var org,out var problem))return problem!;return PosApiResults.FromResult(await use.ExecuteAsync(org,id,query,category,page,pageSize,ct),Results.Ok);});
        group.MapPost("/exposures",async(HttpRequest req,ExposeProductRequest body,ExposeProduct use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ManageSuppliers,out var org,out var problem))return problem!;
         return PosApiResults.FromResult(await use.ExecuteAsync(org,body,ct),Results.Ok);});
        group.MapPost("/relationships/{id:guid}/exposures",(Guid id)=>
            PosApiResults.Problem("pos.connected_supplier.route_deprecated",
                $"Use PUT /api/v1/pos/connected-suppliers/relationships/{id:D}/buyer-product-shares for buyer-specific sharing.",400));
        group.MapGet("/exposures",async(HttpRequest req,ListExposures use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ViewSuppliers,out var org,out var problem))return problem!;return PosApiResults.FromResult(await use.ExecuteAsync(org,ct),Results.Ok);});
        group.MapPut("/exposures/{id:guid}",async(HttpRequest req,Guid id,UpdateExposureRequest body,UpdateExposure use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ManageSuppliers,out var org,out var problem))return problem!;return PosApiResults.FromResult(await use.ExecuteAsync(org,id,body,ct),Results.Ok);});
        group.MapGet("/relationships/{id:guid}/buyer-product-shares",async(HttpRequest req,Guid id,string? query,string? category,string? shareFilter,int? page,int? pageSize,QueryBuyerProductShares queryUse,ListBuyerProductShares legacy,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ViewSuppliers,out var org,out var problem))return problem!;
         if(page is not null||pageSize is not null||!string.IsNullOrWhiteSpace(query)||!string.IsNullOrWhiteSpace(category)||!string.IsNullOrWhiteSpace(shareFilter))
             return PosApiResults.FromResult(await queryUse.ExecuteAsync(org,id,query,category,shareFilter,page,pageSize,ct),Results.Ok);
         return PosApiResults.FromResult(await legacy.ExecuteAsync(org,id,ct),Results.Ok);});
        group.MapGet("/relationships/{id:guid}/eligible-products",async(HttpRequest req,Guid id,ListEligibleProductsForSharing use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ViewSuppliers,out var org,out var problem))return problem!;return PosApiResults.FromResult(await use.ExecuteAsync(org,id,ct),Results.Ok);});
        group.MapPut("/relationships/{id:guid}/buyer-product-shares",async(HttpRequest req,Guid id,SetBuyerProductSharesRequest body,SetBuyerProductShares use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ManageSuppliers,out var org,out var problem))return problem!;return PosApiResults.FromResult(await use.ExecuteAsync(org,id,body.Products??[],ct),Results.Ok);});
        group.MapPost("/relationships/{id:guid}/buyer-product-shares/confirm",async(HttpRequest req,Guid id,ConfirmBuyerProductSharingRequest body,ConfirmBuyerProductSharing use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ManageSuppliers,out var org,out var problem))return problem!;return PosApiResults.FromResult(await use.ExecuteAsync(org,id,body,ct),Results.Ok);});
        group.MapPost("/relationships/{id:guid}/buyer-product-shares/bulk",async(HttpRequest req,Guid id,BulkBuyerProductShareMutationRequest body,BulkMutateBuyerProductShares use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ManageSuppliers,out var org,out var problem))return problem!;return PosApiResults.FromResult(await use.ExecuteAsync(org,id,body,ct),Results.Ok);});
        group.MapPost("/relationships/{id:guid}/buyer-product-shares/pricing/preview",async(HttpRequest req,Guid id,BulkBuyerPricingRequest body,PreviewBuyerProductPricing use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ViewSuppliers,out var org,out var problem))return problem!;return PosApiResults.FromResult(await use.ExecuteAsync(org,id,body,ct),Results.Ok);});
        group.MapPost("/relationships/{id:guid}/buyer-product-shares/pricing/apply",async(HttpRequest req,Guid id,BulkBuyerPricingRequest body,ApplyBuyerProductPricing use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ManageSuppliers,out var org,out var problem))return problem!;return PosApiResults.FromResult(await use.ExecuteAsync(org,id,body,ct),Results.Ok);});
        group.MapPost("/relationships/{id:guid}/links",async(HttpRequest req,Guid id,LinkProductRequest body,LinkProduct use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ManagePurchasing,out var org,out var problem))return problem!;return PosApiResults.FromResult(await use.ExecuteAsync(org,id,body,ct),Results.Ok);});
        group.MapGet("/relationships/{id:guid}/links",async(HttpRequest req,Guid id,ListLinks use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ViewPurchasing,out var org,out var problem))return problem!;return PosApiResults.FromResult(await use.ExecuteAsync(org,id,ct),Results.Ok);});
        group.MapDelete("/links/{id:guid}",async(HttpRequest req,Guid id,UnlinkProduct use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ManagePurchasing,out var org,out var problem))return problem!;return PosApiResults.FromResult(await use.ExecuteAsync(org,id,ct),Results.Ok);});
        group.MapGet("/relationships/{id:guid}/links/sync",async(HttpRequest req,Guid id,long? sinceVersion,SyncLinkedProductsDelta use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ViewPurchasing,out var org,out var problem))return problem!;return PosApiResults.FromResult(await use.ExecuteAsync(org,id,sinceVersion??0,ct),Results.Ok);});
        group.MapGet("/incoming-orders",async(HttpRequest req,SupplierIncomingOrderQuery use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ViewPurchasing,out var org,out var problem))return problem!;return PosApiResults.FromResult(await use.ExecuteAsync(org,ct),Results.Ok);});
        group.MapPost("/incoming-orders/{id:guid}/accept",async(HttpRequest req,Guid id,AcceptIncoming use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ManagePurchasing,out var org,out var problem))return problem!;return PosApiResults.FromResult(await use.ExecuteAsync(org,id,ct),Results.Ok);});
        group.MapPost("/incoming-orders/{id:guid}/decline",async(HttpRequest req,Guid id,DeclineIncoming use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ManagePurchasing,out var org,out var problem))return problem!;return PosApiResults.FromResult(await use.ExecuteAsync(org,id,ct),Results.Ok);});
        group.MapPost("/relationships/{id:guid}/revalidate-draft",async(HttpRequest req,Guid id,RevalidateConnectedPoDraftRequest body,RevalidateConnectedPoDraft use,IPosCommercialAccessAccessor access,CancellationToken ct)=>
        {if(!Authorize(req,access,UtangCapability.ViewPurchasing,out var org,out var problem))return problem!;return PosApiResults.FromResult(await use.ExecuteAsync(org,id,body,ct),Results.Ok);});
        return app;
    }
    private static bool Authorize(HttpRequest request,IPosCommercialAccessAccessor access,UtangCapability capability,out Guid organizationId,out IResult? problem)
    {if(!PosOrganizationScope.TryGetOrganizationId(request,out organizationId,out problem))return false;return PosCommercialScope.TryAuthorize(access,capability,out problem);}
}
