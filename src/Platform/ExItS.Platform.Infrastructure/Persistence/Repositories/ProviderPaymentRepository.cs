using ExItS.Platform.Application.Payments;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.Infrastructure.Persistence.Payments;
using Microsoft.EntityFrameworkCore;

namespace ExItS.Platform.Infrastructure.Persistence.Repositories;

internal sealed class ProviderPaymentRepository : IProviderPaymentRepository
{
    private readonly PlatformDbContext _db;

    public ProviderPaymentRepository(PlatformDbContext db) => _db = db;

    public async Task<ProviderPayment?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var record = await _db.ProviderPayments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, cancellationToken)
            .ConfigureAwait(false);

        return record is null ? null : ProviderPaymentEntityMapper.ToDomain(record);
    }

    public Task AddAsync(ProviderPayment payment, CancellationToken cancellationToken = default)
    {
        _db.ProviderPayments.Add(ProviderPaymentEntityMapper.ToRecord(payment));
        return Task.CompletedTask;
    }

    public async Task<int> GetNextSequenceAsync(CancellationToken cancellationToken = default)
    {
        var count = await _db.ProviderPayments.CountAsync(cancellationToken).ConfigureAwait(false);
        return count + 1;
    }
}
