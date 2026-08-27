using ExItS.PinoyBuyNowPayLater.Domain.Financing;

namespace ExItS.PinoyBuyNowPayLater.Application.Financing;

public interface IBnplFinancingApplicationRepository
{
    Task<BnplFinancingApplication?> GetByIdAsync(
        Guid organizationId,
        BnplFinancingApplicationId applicationId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<BnplFinancingApplication> Items, int TotalCount)> SearchAsync(
        Guid organizationId,
        Guid? branchId,
        Guid? customerId,
        BnplFinancingApplicationStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(BnplFinancingApplication application, CancellationToken cancellationToken = default);

    Task UpdateAsync(BnplFinancingApplication application, CancellationToken cancellationToken = default);
}
