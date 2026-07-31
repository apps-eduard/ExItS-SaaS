using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>Typed POS cashier shift client. Online-only for P10-WP04.</summary>
public interface IPosCashierShiftClient
{
    Task<ApiResult<PagedResult<PosCashierShiftDto>>> ListAsync(
        string? status = null,
        Guid? actorId = null,
        string? shiftNumber = null,
        string? fromBusinessDate = null,
        string? toBusinessDate = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<PosCashierShiftDto>> GetCurrentAsync(CancellationToken ct = default);

    Task<ApiResult<PosCashierShiftDto>> GetAsync(Guid shiftId, CancellationToken ct = default);

    Task<ApiResult<PosCashierShiftSummaryDto>> GetSummaryAsync(Guid shiftId, CancellationToken ct = default);

    Task<ApiResult<PosCashierShiftDto>> OpenAsync(OpenCashierShiftRequest request, CancellationToken ct = default);

    Task<ApiResult<PosCashierShiftDto>> CloseAsync(
        Guid shiftId,
        CloseCashierShiftRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosCashierShiftDto>> CancelAsync(Guid shiftId, CancellationToken ct = default);

    Task<ApiResult<PosCashierShiftMovementDto>> RecordMovementAsync(
        Guid shiftId,
        RecordCashierShiftMovementRequest request,
        CancellationToken ct = default);
}
