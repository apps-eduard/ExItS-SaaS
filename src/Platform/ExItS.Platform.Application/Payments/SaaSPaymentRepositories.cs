using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Payments;

public interface ISaaSPaymentRepository
{
    Task<SaaSPayment?> GetByIdAsync(SaaSPaymentId id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNormalizedReferenceAsync(
        SaaSPaymentMethod method,
        string normalizedReference,
        PlatformOrganizationId orgId,
        CancellationToken cancellationToken = default);

    Task AddAsync(SaaSPayment payment, CancellationToken cancellationToken = default);

    Task UpdateAsync(SaaSPayment payment, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<SaaSPayment> Items, int TotalCount)> ListByOrganizationAsync(
        PlatformOrganizationId orgId,
        SaaSPaymentStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<SaaSPayment> Items, int TotalCount)> ListByProductAsync(
        ProductCode productCode,
        SaaSPaymentStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<SaaSPayment> Items, int TotalCount)> ListByStatusAsync(
        SaaSPaymentStatus status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<SaaSPayment?> GetByNormalizedReferenceAsync(
        SaaSPaymentMethod method,
        string normalizedReference,
        PlatformOrganizationId orgId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<SaaSPayment> Items, int TotalCount)> ListBySubscriptionAsync(
        SubscriptionId subscriptionId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}
