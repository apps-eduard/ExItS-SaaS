using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

public interface IPosCustomerClient
{
    Task<ApiResult<PosCustomerPagedResult>> ListAsync(
        string? search = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<PosCustomerDetailDto>> GetAsync(Guid customerId, CancellationToken ct = default);

    Task<ApiResult<PosCustomerDetailDto>> CreateAsync(CreatePosCustomerRequest request, CancellationToken ct = default);

    Task<ApiResult<PosCustomerDetailDto>> UpdateAsync(
        Guid customerId,
        UpdatePosCustomerRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosCustomerDetailDto>> DeactivateAsync(Guid customerId, CancellationToken ct = default);

    Task<ApiResult<PosCustomerDetailDto>> ReactivateAsync(Guid customerId, CancellationToken ct = default);
}
