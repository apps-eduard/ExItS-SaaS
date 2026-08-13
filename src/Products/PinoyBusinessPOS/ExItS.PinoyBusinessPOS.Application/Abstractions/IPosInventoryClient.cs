using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>
/// Typed POS inventory API client. Online-only for P8-WP04 — offline calls fail fast and no
/// inventory mutation is ever queued locally.
/// </summary>
public interface IPosInventoryClient
{
    Task<ApiResult<PosInventoryAccountPagedResult>> ListAsync(
        string? search = null,
        bool? tracked = null,
        bool? lowStock = null,
        string? productStatus = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<PosInventoryAccountPagedResult>> ListLowStockAsync(
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<PosInventoryAccountDto>> GetAsync(Guid productId, CancellationToken ct = default);

    Task<ApiResult<PosInventoryAccountDto>> EnableAsync(
        Guid productId,
        EnableInventoryTrackingRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosInventoryAccountDto>> DisableAsync(Guid productId, CancellationToken ct = default);

    Task<ApiResult<PosInventoryAccountDto>> AdjustAsync(
        Guid productId,
        AdjustInventoryRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosStockMovementPagedResult>> ListMovementsAsync(
        Guid productId,
        string? movementType = null,
        string? sourceType = null,
        string? fromDateUtc = null,
        string? toDateUtc = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<PosInventoryAccountDto>> SetReorderAsync(
        Guid productId,
        SetInventoryReorderRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosInventoryAccountPagedResult>> ListReorderSuggestionsAsync(
        string? search = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<PosInventoryReconciliationDto>> GetReconciliationAsync(
        Guid productId,
        CancellationToken ct = default);

    Task<ApiResult<PagedResult<PosStockCountDto>>> ListStockCountsAsync(
        string? status = null,
        string? countNumber = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<PosStockCountDto>> GetStockCountAsync(Guid stockCountId, CancellationToken ct = default);

    Task<ApiResult<PosStockCountDto>> CreateStockCountAsync(
        CreateStockCountRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosStockCountDto>> UpdateStockCountAsync(
        Guid stockCountId,
        UpdateStockCountRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosStockCountDto>> StartStockCountAsync(Guid stockCountId, CancellationToken ct = default);

    Task<ApiResult<PosStockCountDto>> CompleteStockCountAsync(Guid stockCountId, CancellationToken ct = default);

    Task<ApiResult<PosStockCountDto>> CancelStockCountAsync(Guid stockCountId, CancellationToken ct = default);

    Task<ApiResult<PagedResult<InventoryTransferListItemDto>>> ListTransfersAsync(
        string? status = null,
        string? transferNumber = null,
        string? direction = null,
        Guid? sourceBranchId = null,
        Guid? destinationBranchId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<InventoryTransferDto>> GetTransferAsync(Guid transferId, CancellationToken ct = default);

    Task<ApiResult<InventoryTransferDto>> CreateTransferAsync(
        CreateInventoryTransferRequest request,
        CancellationToken ct = default);

    Task<ApiResult<InventoryTransferDto>> DispatchTransferAsync(Guid transferId, CancellationToken ct = default);

    Task<ApiResult<InventoryTransferDto>> ReceiveTransferAsync(
        Guid transferId,
        ReceiveInventoryTransferRequest request,
        CancellationToken ct = default);

    Task<ApiResult<InventoryTransferDto>> CancelTransferAsync(Guid transferId, CancellationToken ct = default);
}
