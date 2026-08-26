using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
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

    public async Task<PlatformOrganization?> GetByPublicOrganizationIdAsync(
        string publicOrganizationId,
        CancellationToken cancellationToken = default)
    {
        var normalized = PublicOrganizationIdRules.Normalize(publicOrganizationId);
        var record = await _db.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.PublicOrganizationId == normalized, cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : OrganizationEntityMapper.ToDomain(record);
    }

    public Task<(IReadOnlyList<PlatformOrganization> Items, int TotalCount)> ListAsync(
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        ListAsync(null, null, OrganizationListSortBy.DisplayName, false, skip, take, productCode: null, cancellationToken);

    public async Task<(IReadOnlyList<PlatformOrganization> Items, int TotalCount)> ListAsync(
        OrganizationStatus? status,
        string? search,
        OrganizationListSortBy sortBy,
        bool sortDescending,
        int skip,
        int take,
        ProductCode? productCode = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Organizations.AsNoTracking();

        if (status is not null)
        {
            var statusText = status.Value.ToString();
            query = query.Where(o => o.Status == statusText);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(o =>
                o.DisplayName.ToLower().Contains(term)
                || o.Slug.ToLower().Contains(term)
                || (o.LegalName != null && o.LegalName.ToLower().Contains(term))
                || (o.ContactEmail != null && o.ContactEmail.ToLower().Contains(term)));
        }

        if (productCode is not null)
        {
            var code = productCode.Value;
            query = query.Where(o =>
                _db.Subscriptions.Any(s => s.OrganizationId == o.Id && s.ProductCode == code));
        }

        query = ApplySort(query, sortBy, sortDescending);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
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

    private static IQueryable<PlatformOrganizationRecord> ApplySort(
        IQueryable<PlatformOrganizationRecord> query,
        OrganizationListSortBy sortBy,
        bool descending) =>
        (sortBy, descending) switch
        {
            (OrganizationListSortBy.Slug, false) => query.OrderBy(o => o.Slug).ThenBy(o => o.DisplayName),
            (OrganizationListSortBy.Slug, true) => query.OrderByDescending(o => o.Slug).ThenBy(o => o.DisplayName),
            (OrganizationListSortBy.Status, false) => query.OrderBy(o => o.Status).ThenBy(o => o.DisplayName),
            (OrganizationListSortBy.Status, true) => query.OrderByDescending(o => o.Status).ThenBy(o => o.DisplayName),
            (OrganizationListSortBy.CreatedAtUtc, false) => query.OrderBy(o => o.CreatedAtUtc).ThenBy(o => o.DisplayName),
            (OrganizationListSortBy.CreatedAtUtc, true) => query.OrderByDescending(o => o.CreatedAtUtc).ThenBy(o => o.DisplayName),
            (OrganizationListSortBy.UpdatedAtUtc, false) => query.OrderBy(o => o.UpdatedAtUtc).ThenBy(o => o.DisplayName),
            (OrganizationListSortBy.UpdatedAtUtc, true) => query.OrderByDescending(o => o.UpdatedAtUtc).ThenBy(o => o.DisplayName),
            (_, true) => query.OrderByDescending(o => o.DisplayName).ThenBy(o => o.Slug),
            _ => query.OrderBy(o => o.DisplayName).ThenBy(o => o.Slug)
        };
}
