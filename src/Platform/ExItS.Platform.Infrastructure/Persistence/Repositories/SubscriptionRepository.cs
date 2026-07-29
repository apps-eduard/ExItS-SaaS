using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.Infrastructure.Persistence.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class SubscriptionRepository : ISubscriptionRepository
{
    private static readonly string[] ActiveLikeStatuses = Enum.GetValues<SubscriptionStatus>()
        .Where(Subscription.IsActiveLike)
        .Select(s => s.ToString())
        .ToArray();

    private readonly PlatformDbContext _db;

    public SubscriptionRepository(PlatformDbContext db) => _db = db;

    public async Task<Subscription?> GetByIdAsync(SubscriptionId id, CancellationToken cancellationToken = default)
    {
        var record = await _db.Subscriptions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : SubscriptionEntityMapper.ToDomain(record);
    }

    public async Task<Subscription?> GetCurrentForOrganizationProductAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.OrganizationId == organizationId.Value
                        && s.ProductCode == productCode.Value
                        && ActiveLikeStatuses.Contains(s.Status))
            .OrderByDescending(s => s.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        record ??= await _db.Subscriptions
            .AsNoTracking()
            .Where(s => s.OrganizationId == organizationId.Value && s.ProductCode == productCode.Value)
            .OrderByDescending(s => s.UpdatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : SubscriptionEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId organizationId,
        SubscriptionStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Subscriptions.AsNoTracking().Where(s => s.OrganizationId == organizationId.Value);
        if (status is not null)
        {
            query = query.Where(s => s.Status == status.Value.ToString());
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(SubscriptionEntityMapper.ToDomain).ToList(), totalCount);
    }

    public async Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListByProductAsync(
        ProductCode productCode,
        SubscriptionStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Subscriptions.AsNoTracking().Where(s => s.ProductCode == productCode.Value);
        if (status is not null)
        {
            query = query.Where(s => s.Status == status.Value.ToString());
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(s => s.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(SubscriptionEntityMapper.ToDomain).ToList(), totalCount);
    }

    public async Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListExpiringTrialsAsync(
        DateTimeOffset asOfUtc,
        DateTimeOffset throughUtc,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Subscriptions.AsNoTracking().Where(s =>
            s.Status == SubscriptionStatus.Trialing.ToString()
            && s.TrialEndUtc != null
            && s.TrialEndUtc >= asOfUtc
            && s.TrialEndUtc <= throughUtc);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderBy(s => s.TrialEndUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(SubscriptionEntityMapper.ToDomain).ToList(), totalCount);
    }

    public async Task<(IReadOnlyList<Subscription> Items, int TotalCount)> ListByStatusAsync(
        SubscriptionStatus status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Subscriptions.AsNoTracking().Where(s => s.Status == status.ToString());

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(s => s.UpdatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(SubscriptionEntityMapper.ToDomain).ToList(), totalCount);
    }

    public Task<bool> ExistsActiveLikeAsync(
        PlatformOrganizationId organizationId,
        ProductCode productCode,
        CancellationToken cancellationToken = default) =>
        _db.Subscriptions
            .AsNoTracking()
            .AnyAsync(
                s => s.OrganizationId == organizationId.Value
                     && s.ProductCode == productCode.Value
                     && ActiveLikeStatuses.Contains(s.Status),
                cancellationToken);

    public Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        _db.Subscriptions.Add(SubscriptionEntityMapper.ToRecord(subscription));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(Subscription subscription, CancellationToken cancellationToken = default)
    {
        var record = await _db.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == subscription.Id.Value, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            throw new PersistenceConflictException(
                ApplicationErrorCodes.SubscriptionNotFound,
                "Subscription was not found.");
        }

        SubscriptionEntityMapper.ApplyToRecord(subscription, record);
    }
}
