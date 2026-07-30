using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Payments;

public enum LedgerEntryType
{
    Credit = 0,
    Repayment = 1
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
    int TotalLedgerEntryCount);

public interface IUtangLedgerQuery
{
    Task<(IReadOnlyList<LedgerEntryDto> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
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
