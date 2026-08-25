using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;

namespace ExItS.PinoyBusinessPOS.Application.Payments;

public interface IWriteOffRepository
{
    Task<WriteOff?> GetByIdAsync(
        PosOrganizationId organizationId,
        WriteOffId writeOffId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<WriteOff> Items, int TotalCount)> ListByCustomerAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<WriteOff> Items, int TotalCount)> ListCreatedSinceAsync(
        PosOrganizationId organizationId,
        DateTimeOffset? sinceUtc,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<decimal> SumActiveAmountAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, decimal>> SumActiveAmountsByOrganizationAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveAsync(
        PosOrganizationId organizationId,
        POSCustomerId customerId,
        CancellationToken cancellationToken = default);

    Task AddAsync(WriteOff writeOff, CancellationToken cancellationToken = default);

    Task UpdateAsync(WriteOff writeOff, CancellationToken cancellationToken = default);
}
