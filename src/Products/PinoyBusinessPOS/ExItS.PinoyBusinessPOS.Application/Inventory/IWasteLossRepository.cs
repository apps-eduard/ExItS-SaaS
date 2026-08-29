using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public interface IWasteLossRepository
{
    Task<WasteLoss?> GetByIdAsync(
        PosOrganizationId organizationId,
        WasteLossId wasteLossId,
        CancellationToken cancellationToken = default);

    Task<WasteLoss?> FindByIdempotencyKeyAsync(
        PosOrganizationId organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<WasteLoss> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        WasteLossFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(WasteLoss wasteLoss, CancellationToken cancellationToken = default);

    Task UpdateAsync(WasteLoss wasteLoss, CancellationToken cancellationToken = default);

    Task<string> AllocateNextNumberAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        CancellationToken cancellationToken = default);
}
