using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Registers;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>
/// Typed POS register API client. Online-only for P10-WP07 — no offline cache or queued mutations.
/// </summary>
public interface IPosRegisterClient
{
    Task<ApiResult<PagedResult<PosRegisterDto>>> ListAsync(
        string? registerCode = null,
        string? name = null,
        string? status = null,
        bool? hasOpenShift = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<IReadOnlyList<PosRegisterSummaryDto>>> ListAvailableForShiftAsync(
        CancellationToken ct = default);

    Task<ApiResult<PosRegisterDto>> GetAsync(Guid registerId, CancellationToken ct = default);

    Task<ApiResult<PosRegisterActivityDto>> GetActivityAsync(
        Guid registerId,
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        CancellationToken ct = default);

    Task<ApiResult<PosRegisterDto>> CreateAsync(CreateRegisterRequest request, CancellationToken ct = default);

    Task<ApiResult<PosRegisterDto>> UpdateAsync(
        Guid registerId,
        UpdateRegisterRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosRegisterDto>> ActivateAsync(Guid registerId, CancellationToken ct = default);

    Task<ApiResult<PosRegisterDto>> DeactivateAsync(Guid registerId, CancellationToken ct = default);
}
