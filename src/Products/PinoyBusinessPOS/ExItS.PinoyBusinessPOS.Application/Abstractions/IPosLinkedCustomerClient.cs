using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Statements;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>
/// Personal linked-customer statement projection against POS (Business Utang).
/// Distinct from staff <see cref="IPosCustomerClient"/> statement APIs.
/// </summary>
public interface IPosLinkedCustomerClient
{
    Task<ApiResult<LinkedCustomerStatementSummaryDto>> GetStatementAsync(
        Guid organizationId,
        Guid platformBusinessCustomerId,
        string? currency = null,
        CancellationToken ct = default);

    Task<ApiResult<LinkedCustomerRecentActivityPageDto>> GetRecentActivityAsync(
        Guid organizationId,
        Guid platformBusinessCustomerId,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default);

    Task<ApiResult<LinkedCustomerOpenDebtActivityPageDto>> GetOpenDebtActivityAsync(
        Guid organizationId,
        Guid platformBusinessCustomerId,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default);

    Task<ApiResult<LinkedCustomerRecentActivityPageDto>> GetOlderSettledActivityAsync(
        Guid organizationId,
        Guid platformBusinessCustomerId,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default);

    Task<ApiResult<LinkedCustomerSaleReceiptDto>> GetReceiptAsync(
        Guid organizationId,
        Guid platformBusinessCustomerId,
        Guid saleId,
        string? currency = null,
        CancellationToken ct = default);
}
