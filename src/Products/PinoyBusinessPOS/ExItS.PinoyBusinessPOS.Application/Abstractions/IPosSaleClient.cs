using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>
/// Typed POS sales API client. Online-only for P8-WP02 — offline calls fail fast and no sale is
/// ever queued or cached locally.
/// </summary>
public interface IPosSaleClient
{
    Task<ApiResult<PosSalePagedResult>> ListSalesAsync(
        string? status = null,
        string? paymentMethod = null,
        DateOnly? fromDateUtc = null,
        DateOnly? toDateUtc = null,
        string? saleNumber = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<PosSaleDto>> GetSaleAsync(Guid saleId, CancellationToken ct = default);

    Task<ApiResult<PosSaleDto>> CheckoutAsync(CheckoutSaleRequest request, CancellationToken ct = default);

    Task<ApiResult<PosSaleDto>> VoidSaleAsync(
        Guid saleId,
        VoidSaleRequest request,
        CancellationToken ct = default);
}
