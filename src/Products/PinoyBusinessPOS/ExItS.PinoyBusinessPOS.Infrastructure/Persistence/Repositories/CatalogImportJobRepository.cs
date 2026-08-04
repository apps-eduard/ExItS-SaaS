using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Catalog;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class CatalogImportJobRepository : ICatalogImportJobRepository
{
    private readonly PosDbContext _db;

    public CatalogImportJobRepository(PosDbContext db) => _db = db;

    public async Task<CatalogImportJob?> GetByIdAsync(
        PosOrganizationId organizationId,
        CatalogImportJobId jobId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.CatalogImportJobs
            .AsNoTracking()
            .Include(j => j.Items)
            .FirstOrDefaultAsync(
                j => j.Id == jobId.Value && j.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : CatalogEntityMapper.ToDomain(record);
    }

    public async Task<CatalogImportJob?> FindByIdempotencyKeyAsync(
        PosOrganizationId organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.CatalogImportJobs
            .AsNoTracking()
            .Include(j => j.Items)
            .FirstOrDefaultAsync(
                j => j.OrganizationId == organizationId.Value && j.IdempotencyKey == idempotencyKey,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : CatalogEntityMapper.ToDomain(record);
    }

    public async Task<CatalogImportJob?> ClaimNextAsync(
        DateTimeOffset utcNow,
        TimeSpan staleAfter,
        CancellationToken cancellationToken = default)
    {
        var queued = PosCatalogImportJobStatus.Queued.ToString();
        var processing = PosCatalogImportJobStatus.Processing.ToString();
        var staleBefore = utcNow - staleAfter;

        var record = await _db.CatalogImportJobs
            .Include(j => j.Items)
            .Where(j => j.Status == queued
                        || (j.Status == processing
                            && (j.LastHeartbeatAtUtc == null || j.LastHeartbeatAtUtc < staleBefore)))
            .OrderBy(j => j.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : CatalogEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<CatalogImportItemResult> Items, int TotalCount)> ListItemsAsync(
        PosOrganizationId organizationId,
        CatalogImportJobId jobId,
        PosCatalogImportItemStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var jobExists = await _db.CatalogImportJobs.AsNoTracking()
            .AnyAsync(j => j.Id == jobId.Value && j.OrganizationId == organizationId.Value, cancellationToken)
            .ConfigureAwait(false);
        if (!jobExists)
        {
            return ([], 0);
        }

        var query = _db.CatalogImportItems.AsNoTracking()
            .Where(i => i.CatalogImportJobId == jobId.Value);

        if (status is not null)
        {
            var statusName = status.Value.ToString();
            query = query.Where(i => i.Status == statusName);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(CatalogEntityMapper.ToDomain).ToList(), total);
    }

    public Task AddAsync(CatalogImportJob job, CancellationToken cancellationToken = default)
    {
        var record = CatalogEntityMapper.ToRecord(job);
        foreach (var item in record.Items)
        {
            item.CatalogImportJobId = job.Id.Value;
        }

        _db.CatalogImportJobs.Add(record);
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(CatalogImportJob job, CancellationToken cancellationToken = default)
    {
        var record = await _db.CatalogImportJobs
            .Include(j => j.Items)
            .FirstOrDefaultAsync(j => j.Id == job.Id.Value, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.CatalogImportJobNotFound,
                "Import job was not found.");
        }

        if (record.OrganizationId != job.OrganizationId.Value)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.CatalogImportJobNotFound,
                "Import job was not found.");
        }

        CatalogEntityMapper.ApplyToRecord(job, record);
    }
}
