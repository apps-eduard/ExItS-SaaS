using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Payments;

public enum LedgerEntryType
{
    Credit = 0,
    Repayment = 1,
    WriteOff = 2
}

public sealed record LedgerEntryDto(
    Guid EntryId,
    string EntryType,
    Guid OrganizationId,
    Guid CustomerId,
    decimal Amount,
    decimal SignedEffect,
    string? Remarks,
    string Status,
    DateTimeOffset RecordedAtUtc,
    Guid? RecordedBy,
    DateTimeOffset? ReversedAtUtc,
    string? ReversalReason,
    Guid? ReversedBy,
    decimal? RunningBalance);

public sealed record CustomerUtangSummaryDto(
    Guid CustomerId,
    Guid OrganizationId,
    decimal OutstandingAmount,
    decimal ActiveCreditTotal,
    decimal ActiveRepaymentTotal,
    int ActiveCreditCount,
    int ActiveRepaymentCount,
    int TotalLedgerEntryCount,
    decimal OverdueAmount = 0m,
    int OverdueCreditCount = 0,
    DateOnly? EarliestOverdueDate = null,
    DateOnly? NextUpcomingDueDate = null,
    int CreditsWithoutDueDateCount = 0,
    decimal ActiveWriteOffTotal = 0m,
    int ActiveWriteOffCount = 0);

public interface IUtangLedgerQuery
{
    Task<(IReadOnlyList<LedgerEntryDto> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default,
        IReadOnlySet<Guid>? historyBranchIds = null,
        bool hideOrgWideAdjustments = false);

    /// <summary>Full chronological ledger with running balances (no paging).</summary>
    Task<IReadOnlyList<LedgerEntryDto>> ListAllChronologicalAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default,
        IReadOnlySet<Guid>? historyBranchIds = null,
        bool hideOrgWideAdjustments = false);
}

public interface IOutstandingBalanceService
{
    Task<decimal> GetOutstandingAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default);

    Task<CustomerUtangSummaryDto> GetSummaryAsync(
        Guid organizationId,
        Guid customerId,
        CancellationToken cancellationToken = default);
}
