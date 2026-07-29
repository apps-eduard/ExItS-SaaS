using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class FeatureDefinitionRepository : IFeatureDefinitionRepository
{
    private readonly PlatformDbContext _db;

    public FeatureDefinitionRepository(PlatformDbContext db) => _db = db;

    public async Task<FeatureDefinition?> GetByProductAndCodeAsync(
        ProductCode productCode,
        FeatureCode featureCode,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.FeatureDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                f => f.ProductCode == productCode.Value && f.FeatureCode == featureCode.Value,
                cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : CatalogEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<FeatureDefinition>> ListByProductAsync(
        ProductCode productCode,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.FeatureDefinitions
            .AsNoTracking()
            .Where(f => f.ProductCode == productCode.Value)
            .OrderBy(f => f.FeatureCode)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records.Select(CatalogEntityMapper.ToDomain).ToList();
    }

    public Task AddAsync(FeatureDefinition feature, CancellationToken cancellationToken = default)
    {
        _db.FeatureDefinitions.Add(CatalogEntityMapper.ToRecord(feature));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(FeatureDefinition feature, CancellationToken cancellationToken = default)
    {
        var record = await _db.FeatureDefinitions
            .FirstOrDefaultAsync(
                f => f.ProductCode == feature.ProductCode.Value && f.FeatureCode == feature.Code.Value,
                cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            return;
        }

        CatalogEntityMapper.ApplyToRecord(feature, record);
    }
}
