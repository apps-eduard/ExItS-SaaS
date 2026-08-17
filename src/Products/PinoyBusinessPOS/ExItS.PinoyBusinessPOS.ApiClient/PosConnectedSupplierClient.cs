using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

namespace ExItS.PinoyBusinessPOS.ApiClient;

public sealed class PosConnectedSupplierClient(HttpClient http,IConnectivityService? connectivity=null) : IPosConnectedSupplierClient
{
    private const string Path="/api/v1/pos/connected-suppliers";
    private static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase,PropertyNameCaseInsensitive=true};
    public Task<ApiResult<ConnectedSupplierRelationshipDto>> RequestConnectionAsync(RequestConnectionRequest r,CancellationToken ct=default)=>Send<ConnectedSupplierRelationshipDto>(HttpMethod.Post,$"{Path}/relationships/request",r,ct);
    public Task<ApiResult<ConnectedSupplierRelationshipDto>> ApproveAsync(Guid id,RespondConnectionRequest r,CancellationToken ct=default)=>Send<ConnectedSupplierRelationshipDto>(HttpMethod.Post,$"{Path}/relationships/{id:D}/approve",r,ct);
    public Task<ApiResult<ConnectedSupplierRelationshipDto>> DeclineAsync(Guid id,RespondConnectionRequest r,CancellationToken ct=default)=>Send<ConnectedSupplierRelationshipDto>(HttpMethod.Post,$"{Path}/relationships/{id:D}/decline",r,ct);
    public Task<ApiResult<ConnectedSupplierRelationshipDto>> DisconnectAsync(Guid id,CancellationToken ct=default)=>Send<ConnectedSupplierRelationshipDto>(HttpMethod.Post,$"{Path}/relationships/{id:D}/disconnect",null,ct);
    public Task<ApiResult<IReadOnlyList<ConnectedSupplierRelationshipDto>>> ListRelationshipsAsync(string view="buyer",CancellationToken ct=default)=>Send<IReadOnlyList<ConnectedSupplierRelationshipDto>>(HttpMethod.Get,$"{Path}/relationships?view={Uri.EscapeDataString(view)}",null,ct);
    public Task<ApiResult<PagedResult<SupplierProductExposureDto>>> SearchCatalogAsync(Guid id,string? query=null,string? category=null,int page=1,int pageSize=25,CancellationToken ct=default)=>
        Send<PagedResult<SupplierProductExposureDto>>(HttpMethod.Get,$"{Path}/relationships/{id:D}/catalog?page={page}&pageSize={pageSize}&query={Uri.EscapeDataString(query??"")}&category={Uri.EscapeDataString(category??"")}",null,ct);
    public Task<ApiResult<SupplierProductExposureDto>> ExposeProductAsync(Guid id,ExposeProductRequest r,CancellationToken ct=default)=>Send<SupplierProductExposureDto>(HttpMethod.Post,$"{Path}/exposures",r,ct);
    public Task<ApiResult<IReadOnlyList<SupplierProductExposureDto>>> ListExposuresAsync(CancellationToken ct=default)=>Send<IReadOnlyList<SupplierProductExposureDto>>(HttpMethod.Get,$"{Path}/exposures",null,ct);
    public Task<ApiResult<IReadOnlyList<ConnectedBuyerProductShareDto>>> ListBuyerProductSharesAsync(Guid id,CancellationToken ct=default)=>Send<IReadOnlyList<ConnectedBuyerProductShareDto>>(HttpMethod.Get,$"{Path}/relationships/{id:D}/buyer-product-shares",null,ct);
    public Task<ApiResult<BuyerProductShareQueryResultDto>> QueryBuyerProductSharesAsync(Guid id,string? query=null,string? category=null,string? shareFilter=null,int page=1,int pageSize=25,CancellationToken ct=default)=>
        Send<BuyerProductShareQueryResultDto>(HttpMethod.Get,$"{Path}/relationships/{id:D}/buyer-product-shares?page={page}&pageSize={pageSize}&query={Uri.EscapeDataString(query??"")}&category={Uri.EscapeDataString(category??"")}&shareFilter={Uri.EscapeDataString(shareFilter??"")}",null,ct);
    public Task<ApiResult<IReadOnlyList<ConnectedBuyerProductShareDto>>> ListEligibleProductsForSharingAsync(Guid id,CancellationToken ct=default)=>Send<IReadOnlyList<ConnectedBuyerProductShareDto>>(HttpMethod.Get,$"{Path}/relationships/{id:D}/eligible-products",null,ct);
    public Task<ApiResult<IReadOnlyList<ConnectedBuyerProductShareDto>>> SetBuyerProductSharesAsync(Guid id,SetBuyerProductSharesRequest r,CancellationToken ct=default)=>Send<IReadOnlyList<ConnectedBuyerProductShareDto>>(HttpMethod.Put,$"{Path}/relationships/{id:D}/buyer-product-shares",r,ct);
    public Task<ApiResult<IReadOnlyList<ConnectedBuyerProductShareDto>>> ConfirmBuyerProductSharingAsync(Guid id,ConfirmBuyerProductSharingRequest r,CancellationToken ct=default)=>Send<IReadOnlyList<ConnectedBuyerProductShareDto>>(HttpMethod.Post,$"{Path}/relationships/{id:D}/buyer-product-shares/confirm",r,ct);
    public Task<ApiResult<BulkBuyerProductShareMutationResultDto>> BulkMutateBuyerProductSharesAsync(Guid id,BulkBuyerProductShareMutationRequest r,CancellationToken ct=default)=>Send<BulkBuyerProductShareMutationResultDto>(HttpMethod.Post,$"{Path}/relationships/{id:D}/buyer-product-shares/bulk",r,ct);
    public Task<ApiResult<BulkBuyerPricingPreviewDto>> PreviewBuyerProductPricingAsync(Guid id,BulkBuyerPricingRequest r,CancellationToken ct=default)=>Send<BulkBuyerPricingPreviewDto>(HttpMethod.Post,$"{Path}/relationships/{id:D}/buyer-product-shares/pricing/preview",r,ct);
    public Task<ApiResult<BulkBuyerProductShareMutationResultDto>> ApplyBuyerProductPricingAsync(Guid id,BulkBuyerPricingRequest r,CancellationToken ct=default)=>Send<BulkBuyerProductShareMutationResultDto>(HttpMethod.Post,$"{Path}/relationships/{id:D}/buyer-product-shares/pricing/apply",r,ct);
    public Task<ApiResult<BuyerSupplierProductLinkDto>> LinkProductAsync(Guid id,LinkProductRequest r,CancellationToken ct=default)=>Send<BuyerSupplierProductLinkDto>(HttpMethod.Post,$"{Path}/relationships/{id:D}/links",r,ct);
    public Task<ApiResult<CreateBuyerProductAndLinkResultDto>> CreateBuyerProductAndLinkAsync(Guid id,CreateBuyerProductAndLinkRequest r,CancellationToken ct=default)=>Send<CreateBuyerProductAndLinkResultDto>(HttpMethod.Post,$"{Path}/relationships/{id:D}/links/create-and-link",r,ct);
    public Task<ApiResult<SuggestBuyerProductMatchesResultDto>> SuggestBuyerProductMatchesAsync(Guid id,Guid exposureId,CancellationToken ct=default)=>Send<SuggestBuyerProductMatchesResultDto>(HttpMethod.Get,$"{Path}/relationships/{id:D}/catalog/{exposureId:D}/match-suggestions",null,ct);
    public Task<ApiResult<CatalogReadinessResultDto>> ClassifyCatalogReadinessAsync(Guid id,CancellationToken ct=default)=>Send<CatalogReadinessResultDto>(HttpMethod.Get,$"{Path}/relationships/{id:D}/catalog/readiness",null,ct);
    public Task<ApiResult<AutoLinkExactMatchesResultDto>> AutoLinkExactMatchesAsync(Guid id,CancellationToken ct=default)=>Send<AutoLinkExactMatchesResultDto>(HttpMethod.Post,$"{Path}/relationships/{id:D}/catalog/auto-link-exact",null,ct);
    public Task<ApiResult<IReadOnlyList<BuyerSupplierProductLinkDto>>> ListLinksAsync(Guid id,CancellationToken ct=default)=>Send<IReadOnlyList<BuyerSupplierProductLinkDto>>(HttpMethod.Get,$"{Path}/relationships/{id:D}/links",null,ct);
    public Task<ApiResult<LinkedProductsDeltaDto>> SyncLinksAsync(Guid id,long sinceVersion,CancellationToken ct=default)=>Send<LinkedProductsDeltaDto>(HttpMethod.Get,$"{Path}/relationships/{id:D}/links/sync?sinceVersion={sinceVersion}",null,ct);
    public Task<ApiResult<IReadOnlyList<ConnectedPurchaseOrderDto>>> ListIncomingOrdersAsync(string? status=null,CancellationToken ct=default)=>
        Send<IReadOnlyList<ConnectedPurchaseOrderDto>>(HttpMethod.Get,$"{Path}/incoming-orders{(string.IsNullOrWhiteSpace(status)?string.Empty:$"?status={Uri.EscapeDataString(status)}")}",null,ct);
    public Task<ApiResult<ConnectedPurchaseOrderDto>> GetIncomingOrderAsync(Guid id,CancellationToken ct=default)=>Send<ConnectedPurchaseOrderDto>(HttpMethod.Get,$"{Path}/incoming-orders/{id:D}",null,ct);
    public Task<ApiResult<ConnectedPurchaseOrderDto>> AcceptIncomingAsync(Guid id,CancellationToken ct=default)=>Send<ConnectedPurchaseOrderDto>(HttpMethod.Post,$"{Path}/incoming-orders/{id:D}/accept",null,ct);
    public Task<ApiResult<ConnectedPurchaseOrderDto>> DeclineIncomingAsync(Guid id,DeclineIncomingOrderRequest? request=null,CancellationToken ct=default)=>Send<ConnectedPurchaseOrderDto>(HttpMethod.Post,$"{Path}/incoming-orders/{id:D}/decline",request??new(),ct);
    public Task<ApiResult<ConnectedPurchaseOrderDto>> PrepareIncomingAsync(Guid id,CancellationToken ct=default)=>Send<ConnectedPurchaseOrderDto>(HttpMethod.Post,$"{Path}/incoming-orders/{id:D}/prepare",null,ct);
    public Task<ApiResult<ConnectedPurchaseOrderDto>> FulfillIncomingAsync(Guid id,CancellationToken ct=default)=>Send<ConnectedPurchaseOrderDto>(HttpMethod.Post,$"{Path}/incoming-orders/{id:D}/fulfill",null,ct);
    public Task<ApiResult<ConnectedPoDraftReviewDto>> RevalidateDraftAsync(Guid id,RevalidateConnectedPoDraftRequest r,CancellationToken ct=default)=>Send<ConnectedPoDraftReviewDto>(HttpMethod.Post,$"{Path}/relationships/{id:D}/revalidate-draft",r,ct);
    private async Task<ApiResult<T>> Send<T>(HttpMethod method,string path,object? body,CancellationToken ct)
    {
        if(connectivity is not null&&!await connectivity.IsConnectedAsync(ct))return new(){Status=ApiCallStatus.Offline,Error=new("Offline","No network connectivity detected.",null,null,null)};
        try{using var req=new HttpRequestMessage(method,path);if(body is not null)req.Content=JsonContent.Create(body,options:Json);
            using var response=await http.SendAsync(req,ct);var text=await response.Content.ReadAsStringAsync(ct);
            if(!response.IsSuccessStatusCode)return new(){Status=Classify(response.StatusCode),Error=Problem(text,(int)response.StatusCode)};
            var value=JsonSerializer.Deserialize<T>(text,Json);return value is null?new(){Status=ApiCallStatus.Failed,Error=new("Invalid response","The API returned no content.",null,null,null)}:ApiResult<T>.Success(value);}
        catch(OperationCanceledException)when(ct.IsCancellationRequested){return new(){Status=ApiCallStatus.Cancelled};}
        catch(HttpRequestException ex){return new(){Status=ApiCallStatus.Offline,Error=new("Network unavailable",ex.Message,null,null,null)};}
    }
    private static ApiError Problem(string text,int status){try{using var d=JsonDocument.Parse(text);var r=d.RootElement;
        return new(r.TryGetProperty("title",out var t)?t.GetString():null,r.TryGetProperty("detail",out var x)?x.GetString():null,
            r.TryGetProperty("errorCode",out var e)?e.GetString():null,null,status);}catch(JsonException){return new("Request failed",text,null,null,status);}}
    private static ApiCallStatus Classify(HttpStatusCode c)=>c switch{HttpStatusCode.NotFound=>ApiCallStatus.NotFound,HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity=>ApiCallStatus.Validation,
        HttpStatusCode.Conflict=>ApiCallStatus.Conflict,HttpStatusCode.Unauthorized=>ApiCallStatus.Unauthorized,HttpStatusCode.Forbidden=>ApiCallStatus.Forbidden,_ when(int)c>=500=>ApiCallStatus.Unavailable,_=>ApiCallStatus.Failed};
}
