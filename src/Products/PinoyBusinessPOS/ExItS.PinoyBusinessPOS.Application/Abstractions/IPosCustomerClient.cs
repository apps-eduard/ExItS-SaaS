using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Payments;

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

    Task<ApiResult<PosCustomerCreditSummaryDto>> GetCreditSummaryAsync(Guid customerId, CancellationToken ct = default);

    Task<ApiResult<PosCreditEntryPagedResult>> ListCreditEntriesAsync(
        Guid customerId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<PosCreditEntryDto>> CreateCreditEntryAsync(
        Guid customerId,
        CreatePosCreditEntryRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosCreditEntryDto>> ReverseCreditEntryAsync(
        Guid customerId,
        Guid entryId,
        ReversePosCreditEntryRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosCustomerUtangSummaryDto>> GetUtangSummaryAsync(Guid customerId, CancellationToken ct = default);

    Task<ApiResult<PosLedgerPagedResult>> ListLedgerAsync(
        Guid customerId,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);

    Task<ApiResult<PosRepaymentPagedResult>> ListRepaymentsAsync(
        Guid customerId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<PosRepaymentDto>> CreateRepaymentAsync(
        Guid customerId,
        CreatePosRepaymentRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosRepaymentDto>> GetRepaymentAsync(Guid repaymentId, CancellationToken ct = default);

    Task<ApiResult<PosRepaymentDto>> ReverseRepaymentAsync(
        Guid repaymentId,
        ReversePosRepaymentRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosCreditEntryDto>> SetCreditDueDateAsync(
        Guid creditEntryId,
        SetPosCreditDueDateRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PosCreditEntryDto>> ClearCreditDueDateAsync(
        Guid creditEntryId,
        string reason,
        CancellationToken ct = default);

    Task<ApiResult<PosCreditDueDateHistoryPagedResult>> ListCreditDueDateHistoryAsync(
        Guid creditEntryId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<PosCustomerOverdueSummaryDto>> GetOverdueSummaryAsync(
        Guid customerId,
        CancellationToken ct = default);

    Task<ApiResult<PosAgedCreditPagedResult>> ListAgedCreditsAsync(
        Guid customerId,
        string? filter = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);

    Task<ApiResult<PosOverdueCustomerPagedResult>> ListOverdueCustomersAsync(
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    Task<ApiResult<PosAgedCreditPagedResult>> ListOverdueCreditsAsync(
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);
}
