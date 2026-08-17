using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Purchasing;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>Typed POS purchasing client. Online-only for P10-WP02.</summary>
public interface IPosPurchaseOrderClient
{
    Task<ApiResult<PagedResult<PosPurchaseOrderDto>>> ListAsync(
        string? status = null,
        Guid? supplierId = null,
        string? poNumber = null,
        string? fromOrderDate = null,
        string? toOrderDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<PosPurchaseOrderDto>> GetAsync(Guid purchaseOrderId, CancellationToken ct = default);

    Task<ApiResult<PosPurchaseOrderDto>> CreateAsync(CreatePurchaseOrderRequest request, CancellationToken ct = default);

    Task<ApiResult<PosPurchaseOrderDto>> UpdateAsync(
        Guid purchaseOrderId,
        UpdatePurchaseOrderRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosPurchaseOrderDto>> SubmitAsync(Guid purchaseOrderId, CancellationToken ct = default);

    Task<ApiResult<PosPurchaseOrderDto>> CancelAsync(Guid purchaseOrderId, CancellationToken ct = default);

    Task<ApiResult<PosPurchaseOrderDto>> AcceptConnectedChangesAsync(Guid purchaseOrderId, CancellationToken ct = default);

    Task<ApiResult<PosGoodsReceiptDto>> ReceiveAsync(
        Guid purchaseOrderId,
        ReceivePurchaseOrderRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosGoodsReceiptDto>> GetGoodsReceiptAsync(Guid goodsReceiptId, CancellationToken ct = default);
}
