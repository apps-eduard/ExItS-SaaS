using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class TrialDefinitionRepository : ITrialDefinitionRepository
{
    private readonly PlatformDbContext _db;

    public TrialDefinitionRepository(PlatformDbContext db) => _db = db;

    public async Task<TrialDefinition?> GetByIdAsync(
        TrialDefinitionId id,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.TrialDefinitions
            .AsNoTracking()
            .Include(t => t.FeatureGrants)
            .FirstOrDefaultAsync(t => t.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : CatalogEntityMapper.ToDomain(record);
    }

    public async Task<IReadOnlyList<TrialDefinition>> ListByProductAsync(
        ProductCode productCode,
        CancellationToken cancellationToken = default)
    {
        var records = await _db.TrialDefinitions
            .AsNoTracking()
            .Include(t => t.FeatureGrants)
            .Where(t => t.ProductCode == productCode.Value)
            .OrderBy(t => t.DisplayName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records.Select(CatalogEntityMapper.ToDomain).ToList();
    }

    public Task AddAsync(TrialDefinition trial, CancellationToken cancellationToken = default)
    {
        _db.TrialDefinitions.Add(CatalogEntityMapper.ToRecord(trial));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(TrialDefinition trial, CancellationToken cancellationToken = default)
    {
        var record = await _db.TrialDefinitions
            .Include(t => t.FeatureGrants)
            .FirstOrDefaultAsync(t => t.Id == trial.Id.Value, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            return;
        }

        CatalogEntityMapper.ApplyToRecord(trial, record);
    }
}
