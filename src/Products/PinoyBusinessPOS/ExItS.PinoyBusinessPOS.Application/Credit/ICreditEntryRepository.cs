using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Credit;

public interface ICreditEntryRepository
{
    Task<CreditEntry?> GetByIdAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CreditEntryId entryId,
        CancellationToken cancellationToken = default);

    Task<CreditEntry?> GetByIdForOrganizationAsync(
        PosOrganizationId organizationId,
        CreditEntryId entryId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListByCustomerAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default,
        IReadOnlySet<Guid>? historyBranchIds = null);

    Task<(IReadOnlyList<CreditEntry> Items, int TotalCount)> ListCreatedSinceAsync(
        PosOrganizationId organizationId,
        DateTimeOffset? sinceUtc,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CreditEntry>> ListActiveByOrganizationAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Credits whose UTC calendar <c>CreatedAtUtc</c> falls in the inclusive range (all statuses).
    /// </summary>
    Task<IReadOnlyList<CreditEntry>> ListRecordedInRangeAsync(
        PosOrganizationId organizationId,
        DateOnly fromDateUtc,
        DateOnly toDateUtc,
        CancellationToken cancellationToken = default);

    Task<decimal> SumActiveAmountAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default,
        IReadOnlySet<Guid>? historyBranchIds = null);

    Task<int> CountActiveAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default,
        IReadOnlySet<Guid>? historyBranchIds = null);

    Task AddAsync(CreditEntry entry, CancellationToken cancellationToken = default);

    Task UpdateAsync(CreditEntry entry, CancellationToken cancellationToken = default);
}
