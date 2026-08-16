using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

public interface IPosConnectedSupplierClient
{
    Task<ApiResult<ConnectedSupplierRelationshipDto>> RequestConnectionAsync(RequestConnectionRequest request,CancellationToken ct=default);
    Task<ApiResult<ConnectedSupplierRelationshipDto>> ApproveAsync(Guid relationshipId,RespondConnectionRequest request,CancellationToken ct=default);
    Task<ApiResult<ConnectedSupplierRelationshipDto>> DeclineAsync(Guid relationshipId,RespondConnectionRequest request,CancellationToken ct=default);
    Task<ApiResult<ConnectedSupplierRelationshipDto>> DisconnectAsync(Guid relationshipId,CancellationToken ct=default);
    Task<ApiResult<IReadOnlyList<ConnectedSupplierRelationshipDto>>> ListRelationshipsAsync(string view="buyer",CancellationToken ct=default);
    Task<ApiResult<PagedResult<SupplierProductExposureDto>>> SearchCatalogAsync(Guid relationshipId,string? query=null,string? category=null,int page=1,int pageSize=25,CancellationToken ct=default);
    Task<ApiResult<SupplierProductExposureDto>> ExposeProductAsync(Guid relationshipId,ExposeProductRequest request,CancellationToken ct=default);
    Task<ApiResult<IReadOnlyList<SupplierProductExposureDto>>> ListExposuresAsync(CancellationToken ct=default);
    Task<ApiResult<IReadOnlyList<ConnectedBuyerProductShareDto>>> ListBuyerProductSharesAsync(Guid relationshipId,CancellationToken ct=default);
    Task<ApiResult<BuyerProductShareQueryResultDto>> QueryBuyerProductSharesAsync(Guid relationshipId,string? query=null,string? category=null,string? shareFilter=null,int page=1,int pageSize=25,CancellationToken ct=default);
    Task<ApiResult<IReadOnlyList<ConnectedBuyerProductShareDto>>> ListEligibleProductsForSharingAsync(Guid relationshipId,CancellationToken ct=default);
    Task<ApiResult<IReadOnlyList<ConnectedBuyerProductShareDto>>> SetBuyerProductSharesAsync(Guid relationshipId,SetBuyerProductSharesRequest request,CancellationToken ct=default);
    Task<ApiResult<IReadOnlyList<ConnectedBuyerProductShareDto>>> ConfirmBuyerProductSharingAsync(Guid relationshipId,ConfirmBuyerProductSharingRequest request,CancellationToken ct=default);
    Task<ApiResult<BulkBuyerProductShareMutationResultDto>> BulkMutateBuyerProductSharesAsync(Guid relationshipId,BulkBuyerProductShareMutationRequest request,CancellationToken ct=default);
    Task<ApiResult<BulkBuyerPricingPreviewDto>> PreviewBuyerProductPricingAsync(Guid relationshipId,BulkBuyerPricingRequest request,CancellationToken ct=default);
    Task<ApiResult<BulkBuyerProductShareMutationResultDto>> ApplyBuyerProductPricingAsync(Guid relationshipId,BulkBuyerPricingRequest request,CancellationToken ct=default);
    Task<ApiResult<BuyerSupplierProductLinkDto>> LinkProductAsync(Guid relationshipId,LinkProductRequest request,CancellationToken ct=default);
    Task<ApiResult<CreateBuyerProductAndLinkResultDto>> CreateBuyerProductAndLinkAsync(Guid relationshipId,CreateBuyerProductAndLinkRequest request,CancellationToken ct=default);
    Task<ApiResult<SuggestBuyerProductMatchesResultDto>> SuggestBuyerProductMatchesAsync(Guid relationshipId,Guid exposureId,CancellationToken ct=default);
    Task<ApiResult<IReadOnlyList<BuyerSupplierProductLinkDto>>> ListLinksAsync(Guid relationshipId,CancellationToken ct=default);
    Task<ApiResult<LinkedProductsDeltaDto>> SyncLinksAsync(Guid relationshipId,long sinceVersion,CancellationToken ct=default);
    Task<ApiResult<IReadOnlyList<ConnectedPurchaseOrderDto>>> ListIncomingOrdersAsync(string? status=null,CancellationToken ct=default);
    Task<ApiResult<ConnectedPurchaseOrderDto>> GetIncomingOrderAsync(Guid orderId,CancellationToken ct=default);
    Task<ApiResult<ConnectedPurchaseOrderDto>> AcceptIncomingAsync(Guid orderId,CancellationToken ct=default);
    Task<ApiResult<ConnectedPurchaseOrderDto>> DeclineIncomingAsync(Guid orderId,DeclineIncomingOrderRequest? request=null,CancellationToken ct=default);
    Task<ApiResult<ConnectedPurchaseOrderDto>> PrepareIncomingAsync(Guid orderId,CancellationToken ct=default);
    Task<ApiResult<ConnectedPurchaseOrderDto>> FulfillIncomingAsync(Guid orderId,CancellationToken ct=default);
    Task<ApiResult<ConnectedPoDraftReviewDto>> RevalidateDraftAsync(Guid relationshipId,RevalidateConnectedPoDraftRequest request,CancellationToken ct=default);
}
