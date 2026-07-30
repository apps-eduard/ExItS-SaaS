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
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);
}
