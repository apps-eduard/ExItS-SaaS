using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.Infrastructure.Persistence.Payments;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class SaaSPaymentRepository : ISaaSPaymentRepository
{
    private static readonly string[] TerminalStatuses =
    [
        nameof(SaaSPaymentStatus.Rejected),
        nameof(SaaSPaymentStatus.Voided)
    ];

    private readonly PlatformDbContext _db;

    public SaaSPaymentRepository(PlatformDbContext db) => _db = db;

    public async Task<SaaSPayment?> GetByIdAsync(SaaSPaymentId id, CancellationToken cancellationToken = default)
    {
        var record = await _db.SaaSPayments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id.Value, cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : SaaSPaymentEntityMapper.ToDomain(record);
    }

    public Task<bool> ExistsByNormalizedReferenceAsync(
        SaaSPaymentMethod method,
        string normalizedReference,
        PlatformOrganizationId orgId,
        CancellationToken cancellationToken = default) =>
        _db.SaaSPayments
            .AsNoTracking()
            .AnyAsync(
                p => p.Method == method.ToString()
                     && p.NormalizedReference == normalizedReference
                     && p.OrganizationId == orgId.Value
                     && !TerminalStatuses.Contains(p.Status),
                cancellationToken);

    public async Task<SaaSPayment?> GetByNormalizedReferenceAsync(
        SaaSPaymentMethod method,
        string normalizedReference,
        PlatformOrganizationId orgId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.SaaSPayments
            .AsNoTracking()
            .Where(p => p.Method == method.ToString()
                        && p.NormalizedReference == normalizedReference
                        && p.OrganizationId == orgId.Value)
            .OrderByDescending(p => p.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : SaaSPaymentEntityMapper.ToDomain(record);
    }

    public async Task<(IReadOnlyList<SaaSPayment> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId orgId,
        SaaSPaymentStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.SaaSPayments.AsNoTracking().Where(p => p.OrganizationId == orgId.Value);
        if (status is not null)
        {
            query = query.Where(p => p.Status == status.Value.ToString());
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(SaaSPaymentEntityMapper.ToDomain).ToList(), totalCount);
    }

    public async Task<(IReadOnlyList<SaaSPayment> Items, int TotalCount)> ListByProductAsync(
        ProductCode productCode,
        SaaSPaymentStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.SaaSPayments.AsNoTracking().Where(p => p.ProductCode == productCode.Value);
        if (status is not null)
        {
            query = query.Where(p => p.Status == status.Value.ToString());
        }

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(SaaSPaymentEntityMapper.ToDomain).ToList(), totalCount);
    }

    public async Task<(IReadOnlyList<SaaSPayment> Items, int TotalCount)> ListByStatusAsync(
        SaaSPaymentStatus status,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.SaaSPayments.AsNoTracking().Where(p => p.Status == status.ToString());

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(SaaSPaymentEntityMapper.ToDomain).ToList(), totalCount);
    }

    public async Task<(IReadOnlyList<SaaSPayment> Items, int TotalCount)> ListBySubscriptionAsync(
        SubscriptionId subscriptionId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _db.SaaSPayments.AsNoTracking().Where(p => p.SubscriptionId == subscriptionId.Value);

        var totalCount = await query.CountAsync(cancellationToken).ConfigureAwait(false);
        var records = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (records.Select(SaaSPaymentEntityMapper.ToDomain).ToList(), totalCount);
    }

    public async Task<IReadOnlyList<SaaSPayment>> FindByNormalizedReferenceAsync(
        string normalizedReference,
        SaaSPaymentMethod? method,
        CancellationToken cancellationToken = default)
    {
        var query = _db.SaaSPayments.AsNoTracking()
            .Where(p => p.NormalizedReference == normalizedReference);
        if (method is not null)
        {
            query = query.Where(p => p.Method == method.Value.ToString());
        }

        var records = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Take(10)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return records.Select(SaaSPaymentEntityMapper.ToDomain).ToList();
    }

    public Task AddAsync(SaaSPayment payment, CancellationToken cancellationToken = default)
    {
        _db.SaaSPayments.Add(SaaSPaymentEntityMapper.ToRecord(payment));
        return Task.CompletedTask;
    }

    public async Task UpdateAsync(SaaSPayment payment, CancellationToken cancellationToken = default)
    {
        var record = await _db.SaaSPayments
            .FirstOrDefaultAsync(p => p.Id == payment.Id.Value, cancellationToken)
            .ConfigureAwait(false);

        if (record is null)
        {
            throw new PersistenceConflictException(ApplicationErrorCodes.PaymentNotFound, "Payment was not found.");
        }

        SaaSPaymentEntityMapper.ApplyToRecord(payment, record);
    }
}
