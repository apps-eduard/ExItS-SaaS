using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.OperationalSetup;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>Typed POS operational setup API client. Online-only for P17-WP02.</summary>
public interface IPosOperationalSetupClient
{
    Task<ApiResult<PosOperationalSetupDto>> GetAsync(CancellationToken ct = default);

    Task<ApiResult<PosOperationalSetupDto>> CompleteAsync(
        CompleteOperationalSetupRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosOperationalSetupDto>> UpdateAsync(
        UpdateOperationalSetupRequest request,
        CancellationToken ct = default);

    Task<ApiResult<List<OrganizationCashDenominationDto>>> ListCashDenominationsAsync(
        CancellationToken ct = default);

    Task<ApiResult<List<OrganizationCashDenominationDto>>> ReplaceCashDenominationsAsync(
        ReplaceCashDenominationsRequest request,
        CancellationToken ct = default);
}
