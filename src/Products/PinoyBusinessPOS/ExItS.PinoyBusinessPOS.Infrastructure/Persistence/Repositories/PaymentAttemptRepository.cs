using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class PaymentAttemptRepository : IPaymentAttemptRepository
{
    private static readonly string[] ActiveStatuses =
    [
        nameof(PaymentAttemptStatus.Created),
        nameof(PaymentAttemptStatus.Pending),
        nameof(PaymentAttemptStatus.RequiresCustomerAction),
        nameof(PaymentAttemptStatus.Processing),
        nameof(PaymentAttemptStatus.PendingManualVerification)
    ];

    private readonly PosDbContext _db;

    public PaymentAttemptRepository(PosDbContext db) => _db = db;

    public async Task<PaymentAttempt?> GetByIdAsync(
        PosOrganizationId organizationId,
        PaymentAttemptId id,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.PaymentAttempts
            .FirstOrDefaultAsync(
                e => e.Id == id.Value && e.OrganizationId == organizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : PaymentAttemptEntityMapper.ToDomain(record);
    }

    public async Task<PaymentAttempt?> GetByIdempotencyKeyAsync(
        PosOrganizationId organizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var key = idempotencyKey.Trim();
        var record = await _db.PaymentAttempts.AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.OrganizationId == organizationId.Value && e.IdempotencyKey == key,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : PaymentAttemptEntityMapper.ToDomain(record);
    }

    public async Task<PaymentAttempt?> GetByProviderReferenceAsync(
        string provider,
        string providerReference,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.PaymentAttempts
            .FirstOrDefaultAsync(
                e => e.Provider == provider && e.ProviderReference == providerReference,
                cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : PaymentAttemptEntityMapper.ToDomain(record);
    }

    public async Task<PaymentAttempt?> FindActiveForSaleAsync(
        PosOrganizationId organizationId,
        SaleId saleId,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.PaymentAttempts.AsNoTracking()
            .Where(e => e.OrganizationId == organizationId.Value
                        && e.SaleId == saleId.Value
                        && ActiveStatuses.Contains(e.Status))
            .OrderByDescending(e => e.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        return record is null ? null : PaymentAttemptEntityMapper.ToDomain(record);
    }

    public async Task<bool> ExistsExternalReferenceAsync(
        PosOrganizationId organizationId,
        string externalReference,
        PaymentAttemptId? excludingId = null,
        CancellationToken cancellationToken = default)
    {
        var trimmed = externalReference.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return false;
        }

        var query = _db.PaymentAttempts.AsNoTracking()
            .Where(e => e.OrganizationId == organizationId.Value && e.ExternalReference == trimmed);
        if (excludingId is not null)
        {
            query = query.Where(e => e.Id != excludingId.Value.Value);
        }

        return await query.AnyAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(PaymentAttempt attempt, CancellationToken cancellationToken = default)
    {
        await _db.PaymentAttempts.AddAsync(PaymentAttemptEntityMapper.ToRecord(attempt), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(PaymentAttempt attempt, CancellationToken cancellationToken = default)
    {
        var record = await _db.PaymentAttempts
            .FirstOrDefaultAsync(
                e => e.Id == attempt.Id.Value && e.OrganizationId == attempt.OrganizationId.Value,
                cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            throw new InvalidOperationException($"Payment attempt {attempt.Id.Value:D} was not found for update.");
        }

        PaymentAttemptEntityMapper.Apply(attempt, record);
    }
}
