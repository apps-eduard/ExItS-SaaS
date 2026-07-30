using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Suppliers;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>
/// Typed POS supplier API client. Online-only for P10-WP01 — no offline cache or queued mutations.
/// </summary>
public interface IPosSupplierClient
{
    Task<ApiResult<PagedResult<PosSupplierDto>>> ListAsync(
        string? supplierCode = null,
        string? name = null,
        string? contactPerson = null,
        string? email = null,
        string? mobile = null,
        string? taxOrRegistrationNumber = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<PosSupplierDto>> GetAsync(Guid supplierId, CancellationToken ct = default);

    Task<ApiResult<PosSupplierDto>> CreateAsync(CreateSupplierRequest request, CancellationToken ct = default);

    Task<ApiResult<PosSupplierDto>> UpdateAsync(
        Guid supplierId,
        UpdateSupplierRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosSupplierDto>> ActivateAsync(Guid supplierId, CancellationToken ct = default);

    Task<ApiResult<PosSupplierDto>> DeactivateAsync(Guid supplierId, CancellationToken ct = default);
}
