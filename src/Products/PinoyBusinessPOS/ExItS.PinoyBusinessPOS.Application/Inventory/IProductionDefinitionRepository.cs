using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.Application.Inventory;

public interface IProductionDefinitionRepository
{
    Task<ProductionDefinition?> GetByIdAsync(
        PosOrganizationId organizationId,
        ProductionDefinitionId definitionId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<ProductionDefinition> Items, int TotalCount)> ListAsync(
        PosOrganizationId organizationId,
        ProductionDefinitionFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    /// <summary>All definitions for org (cycle validation). Bounded by organization scope.</summary>
    Task<IReadOnlyList<ProductionDefinition>> ListAllForCycleValidationAsync(
        PosOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(ProductionDefinition definition, CancellationToken cancellationToken = default);

    Task UpdateAsync(ProductionDefinition definition, CancellationToken cancellationToken = default);
}

public sealed record ProductionDefinitionFilter(
    string? Search = null,
    Guid? OutputProductId = null,
    bool? IsActive = null);
