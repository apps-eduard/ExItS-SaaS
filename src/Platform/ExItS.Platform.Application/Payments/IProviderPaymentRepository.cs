using ExItS.Platform.Domain.Payments;

namespace ExItS.Platform.Application.Payments;

public interface IProviderPaymentRepository
{
    Task<ProviderPayment?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
    Task AddAsync(ProviderPayment payment, CancellationToken cancellationToken = default);
    Task<int> GetNextSequenceAsync(CancellationToken cancellationToken = default);
}
