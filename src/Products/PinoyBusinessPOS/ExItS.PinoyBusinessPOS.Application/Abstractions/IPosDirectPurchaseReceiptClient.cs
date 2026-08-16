using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

public interface IPosDirectPurchaseReceiptClient
{
    Task<ApiResult<PagedResult<DirectPurchaseReceiptListItemDto>>> ListAsync(
        string? fromPurchaseDate = null,
        string? toPurchaseDate = null,
        Guid? supplierId = null,
        string? sourceSearch = null,
        string? referenceNumber = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<DirectPurchaseReceiptDto>> GetAsync(Guid receiptId, CancellationToken ct = default);

    Task<ApiResult<DirectPurchaseReceiptDto>> CreateAsync(
        CreateDirectPurchaseReceiptRequest request,
        CancellationToken ct = default);
}
