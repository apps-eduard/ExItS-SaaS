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
    Task<ApiResult<BuyerSupplierProductLinkDto>> LinkProductAsync(Guid relationshipId,LinkProductRequest request,CancellationToken ct=default);
    Task<ApiResult<IReadOnlyList<BuyerSupplierProductLinkDto>>> ListLinksAsync(Guid relationshipId,CancellationToken ct=default);
    Task<ApiResult<LinkedProductsDeltaDto>> SyncLinksAsync(Guid relationshipId,long sinceVersion,CancellationToken ct=default);
    Task<ApiResult<IReadOnlyList<ConnectedPurchaseOrderDto>>> ListIncomingOrdersAsync(CancellationToken ct=default);
    Task<ApiResult<ConnectedPurchaseOrderDto>> AcceptIncomingAsync(Guid orderId,CancellationToken ct=default);
    Task<ApiResult<ConnectedPurchaseOrderDto>> DeclineIncomingAsync(Guid orderId,CancellationToken ct=default);
    Task<ApiResult<ConnectedPoDraftReviewDto>> RevalidateDraftAsync(Guid relationshipId,RevalidateConnectedPoDraftRequest request,CancellationToken ct=default);
}
