using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Application.Statements;

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

    Task<ApiResult<PosCustomerDetailDto>> CreateAsync(
        CreatePosCustomerRequest request,
        PosMutationIdempotencyHeaders? idempotency = null,
        CancellationToken ct = default);

    Task<ApiResult<PosCustomerDetailDto>> UpdateAsync(
        Guid customerId,
        UpdatePosCustomerRequest request,
        PosMutationIdempotencyHeaders? idempotency = null,
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
        PosMutationIdempotencyHeaders? idempotency = null,
        CancellationToken ct = default);

    Task<ApiResult<PosCreditEntryDto>> ReverseCreditEntryAsync(
        Guid customerId,
        Guid entryId,
        ReversePosCreditEntryRequest request,
        PosMutationIdempotencyHeaders? idempotency = null,
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
        PosMutationIdempotencyHeaders? idempotency = null,
        CancellationToken ct = default);

    Task<ApiResult<PosRepaymentDto>> GetRepaymentAsync(Guid repaymentId, CancellationToken ct = default);

    Task<ApiResult<PosRepaymentDto>> ReverseRepaymentAsync(
        Guid repaymentId,
        ReversePosRepaymentRequest request,
        PosMutationIdempotencyHeaders? idempotency = null,
        CancellationToken ct = default);

    Task<ApiResult<PosCreditEntryDto>> SetCreditDueDateAsync(
        Guid creditEntryId,
        SetPosCreditDueDateRequest request,
        PosMutationIdempotencyHeaders? idempotency = null,
        CancellationToken ct = default);

    Task<ApiResult<PosCreditEntryDto>> ClearCreditDueDateAsync(
        Guid creditEntryId,
        ClearPosCreditDueDateRequest request,
        PosMutationIdempotencyHeaders? idempotency = null,
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

    Task<ApiResult<PosCustomerStatementDto>> GetStatementAsync(
        Guid customerId,
        DateOnly periodStart,
        DateOnly periodEnd,
        string? organizationDisplayName = null,
        string? currencyCode = null,
        string? culture = null,
        CancellationToken ct = default);

    Task<ApiResult<PosRepaymentReceiptDto>> GetRepaymentReceiptAsync(
        Guid repaymentId,
        string? organizationDisplayName = null,
        string? currencyCode = null,
        string? culture = null,
        CancellationToken ct = default);

    Task<ApiResult<PosCustomerSyncPageResult>> SyncCustomersAsync(
        DateTimeOffset? sinceUtc = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default);

    Task<ApiResult<PosCreditSyncPageResult>> SyncCreditEntriesAsync(
        DateTimeOffset? sinceUtc = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default);

    Task<ApiResult<PosRepaymentSyncPageResult>> SyncRepaymentsAsync(
        DateTimeOffset? sinceUtc = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken ct = default);
}
