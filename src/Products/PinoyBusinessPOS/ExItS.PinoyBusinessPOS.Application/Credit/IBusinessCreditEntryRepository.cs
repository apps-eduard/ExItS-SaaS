using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.Credit;

public interface IBusinessCreditEntryRepository
{
    Task<BusinessCreditEntry?> GetByIdAsync(
        PosOrganizationId sellerOrganizationId,
        BusinessCreditEntryId entryId,
        CancellationToken cancellationToken = default);

    Task AddAsync(BusinessCreditEntry entry, CancellationToken cancellationToken = default);

    Task UpdateAsync(BusinessCreditEntry entry, CancellationToken cancellationToken = default);

    Task<decimal> SumActiveAmountAsync(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, decimal>> SumActiveAmountsByBuyerIdsAsync(
        PosOrganizationId sellerOrganizationId,
        IReadOnlyCollection<Guid> buyerOrganizationIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// All business credit entries for the seller+buyer pair, oldest first (CreatedAtUtc, then Id).
    /// </summary>
    Task<IReadOnlyList<BusinessCreditEntry>> ListChronologicalForBuyerAsync(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Advisory lock shared with business credit policy mutations for the seller+buyer pair.
    /// Call inside an ambient transaction before authorizing NEW business Utang.
    /// </summary>
    Task AcquireBusinessCreditLockAsync(
        PosOrganizationId sellerOrganizationId,
        PosOrganizationId buyerOrganizationId,
        CancellationToken cancellationToken = default);
}
