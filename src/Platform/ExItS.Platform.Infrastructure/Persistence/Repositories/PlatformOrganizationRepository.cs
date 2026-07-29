using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Infrastructure.Persistence.Organizations;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class PlatformOrganizationRepository : IPlatformOrganizationRepository
{
    private readonly PlatformDbContext _db;

    public PlatformOrganizationRepository(PlatformDbContext db) => _db = db;

    public async Task<PlatformOrganization?> GetByIdAsync(
        PlatformOrganizationId id,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : OrganizationEntityMapper.ToDomain(record);
    }

    public async Task<PlatformOrganization?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var record = await _db.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Slug == slug, cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : OrganizationEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<PlatformOrganization> Items, int TotalCount)> ListAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Organizations.AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderBy(o => o.DisplayName)
            .ThenBy(o => o.Slug)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(OrganizationEntityMapper.ToDomain).ToList(), totalCount);
    }

    public Task AddAsync(PlatformOrganization organization, CancellationToken cancellationToken = default)
    {
        _db.Organizations.Add(OrganizationEntityMapper.ToRecord(organization));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(PlatformOrganization organization, CancellationToken cancellationToken = default)
    {
        var record = await _db.Organizations
            .FirstOrDefaultAsync(o => o.Id == organization.Id.Value, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.OrganizationNotFound,
                "Platform Organization was not found.");
        }

        OrganizationEntityMapper.ApplyToRecord(organization, record);
    }
}
