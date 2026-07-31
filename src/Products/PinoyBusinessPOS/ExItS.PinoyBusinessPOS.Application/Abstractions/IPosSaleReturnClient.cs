using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Returns;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>Typed POS sale return API client. Online-only.</summary>
public interface IPosSaleReturnClient
{
    Task<ApiResult<PosSaleReturnPagedResult>> ListReturnsAsync(
        Guid? saleId = null,
        string? returnNumber = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<PosSaleReturnDto>> GetReturnAsync(Guid returnId, CancellationToken ct = default);

    Task<ApiResult<PosRefundableSaleDto>> GetRefundableAsync(Guid saleId, CancellationToken ct = default);

    Task<ApiResult<PosSaleReturnDto>> CreateReturnAsync(
        CreateSaleReturnRequest request,
        CancellationToken ct = default);
}
