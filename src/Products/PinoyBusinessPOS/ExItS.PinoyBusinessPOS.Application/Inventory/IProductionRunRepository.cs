using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public interface IProductionRunRepository
{
    Task<ProductionRun?> GetByIdAsync(
        PosOrganizationId organizationId,
        ProductionRunId productionRunId,
        CancellationToken cancellationToken = default);

    Task<ProductionRun?> FindByIdempotencyKeyAsync(
        PosOrganizationId organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ProductionRun> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        ProductionRunFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(ProductionRun productionRun, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProductionRun productionRun, CancellationToken cancellationToken = default);

    Task<string> AllocateNextNumberAsync(
        PosOrganizationId organizationId,
        DateOnly businessDateUtc,
        CancellationToken cancellationToken = default);
}

public sealed record ProductionRunFilter(
    DateTimeOffset? FromProducedAtUtc = null,
    DateTimeOffset? ToProducedAtUtc = null,
    string? Status = null,
    Guid? BranchId = null,
    Guid? OutputProductId = null,
    Guid? ProductionDefinitionId = null,
    string? ReferenceNumber = null);
