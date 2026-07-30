using ExItS.Platform.Application.Audit;
using ExItS.Platform.Domain.Audit;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class AuditRecordRepository : IAuditRecordRepository
{
    private readonly PlatformDbContext _db;

    public AuditRecordRepository(PlatformDbContext db) => _db = db;

    public async Task<AuditRecord?> GetByIdAsync(AuditRecordId id, CancellationToken cancellationToken = default)
    {
        var record = await _db.AuditRecords.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : AuthorizationAuditEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<AuditRecord> Items, int TotalCount)> QueryAsync(
        AuditRecordFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.AuditRecords.AsNoTracking().AsQueryable();

        if (filter.OccurredFromUtc is not null)
        {
            var from = filter.OccurredFromUtc.Value;
            query = query.Where(a => a.OccurredAtUtc >= from);
        }

        if (filter.OccurredToUtc is not null)
        {
            var to = filter.OccurredToUtc.Value;
            query = query.Where(a => a.OccurredAtUtc <= to);
        }

        if (!string.IsNullOrWhiteSpace(filter.ActorIdentifier))
        {
            var actor = filter.ActorIdentifier.Trim();
            query = query.Where(a => a.ActorIdentifier == actor);
        }

        if (filter.ActorType is not null)
        {
            var actorType = filter.ActorType.Value.ToString();
            query = query.Where(a => a.ActorType == actorType);
        }

        if (!string.IsNullOrWhiteSpace(filter.ActionCode))
        {
            var action = filter.ActionCode.Trim();
            query = query.Where(a => a.ActionCode == action);
        }

        if (!string.IsNullOrWhiteSpace(filter.TargetType))
        {
            var targetType = filter.TargetType.Trim();
            query = query.Where(a => a.TargetType == targetType);
        }

        if (!string.IsNullOrWhiteSpace(filter.TargetId))
        {
            var targetId = filter.TargetId.Trim();
            query = query.Where(a => a.TargetId == targetId);
        }

        if (filter.OrganizationId is not null)
        {
            var orgId = filter.OrganizationId.Value;
            query = query.Where(a => a.OrganizationId == orgId);
        }

        if (filter.ProductCode is not null)
        {
            var productCode = filter.ProductCode.Value;
            query = query.Where(a => a.ProductCode == productCode);
        }

        if (filter.Outcome is not null)
        {
            var outcome = filter.Outcome.Value.ToString();
            query = query.Where(a => a.Outcome == outcome);
        }

        if (!string.IsNullOrWhiteSpace(filter.CorrelationId))
        {
            var correlationId = filter.CorrelationId.Trim();
            query = query.Where(a => a.CorrelationId == correlationId);
        }

        var total = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(a => a.OccurredAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return (records.Select(AuthorizationAuditEntityMapper.ToDomain).ToList(), total);
    }

    public Task AddAsync(AuditRecord record, CancellationToken cancellationToken = default)
    {
        _db.AuditRecords.Add(AuthorizationAuditEntityMapper.ToRecord(record));
        return Task.CompletedTask;
    }
}
