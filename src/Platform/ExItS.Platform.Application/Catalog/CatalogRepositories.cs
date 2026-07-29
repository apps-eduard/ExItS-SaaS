using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Catalog;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(ProductId id, CancellationToken cancellationToken = default);
    Task<Product?> GetByCodeAsync(ProductCode code, CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);
    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
}

public interface IFeatureDefinitionRepository
{
    Task<FeatureDefinition?> GetByProductAndCodeAsync(
        ProductCode productCode,
        FeatureCode featureCode,
        CancellationToken cancellationToken = default);

    Task AddAsync(FeatureDefinition feature, CancellationToken cancellationToken = default);
}

public interface IPlanRepository
{
    Task<Plan?> GetByIdAsync(PlanId id, CancellationToken cancellationToken = default);
    Task<Plan?> GetByProductAndCodeAsync(ProductCode productCode, PlanCode planCode, CancellationToken cancellationToken = default);
    Task AddAsync(Plan plan, CancellationToken cancellationToken = default);
    Task UpdateAsync(Plan plan, CancellationToken cancellationToken = default);

    Task<PlanVersion?> GetVersionByIdAsync(PlanVersionId id, CancellationToken cancellationToken = default);
    Task<int> GetMaxVersionNumberAsync(PlanId planId, CancellationToken cancellationToken = default);
    Task AddVersionAsync(PlanVersion version, CancellationToken cancellationToken = default);
    Task UpdateVersionAsync(PlanVersion version, CancellationToken cancellationToken = default);
}

public interface ITrialDefinitionRepository
{
    Task<TrialDefinition?> GetByIdAsync(TrialDefinitionId id, CancellationToken cancellationToken = default);
    Task AddAsync(TrialDefinition trial, CancellationToken cancellationToken = default);
}
