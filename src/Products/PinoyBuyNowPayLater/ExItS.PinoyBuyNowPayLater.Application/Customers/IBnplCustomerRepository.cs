using ExItS.PinoyBuyNowPayLater.Domain.Customers;

namespace ExItS.PinoyBuyNowPayLater.Application.Customers;

public interface IBnplCustomerRepository
{
    Task<BnplCustomer?> GetByIdAsync(
        Guid organizationId,
        BnplCustomerId customerId,
        CancellationToken cancellationToken = default);

    Task<BnplCustomer?> FindByLinkedPersonalPublicUserIdAsync(
        Guid organizationId,
        string linkedPersonalPublicUserId,
        CancellationToken cancellationToken = default);

    Task<BnplCustomer?> FindByLinkedCommerceCustomerIdAsync(
        Guid organizationId,
        Guid linkedCommerceCustomerId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<BnplCustomer> Items, int TotalCount)> SearchAsync(
        Guid organizationId,
        string? search,
        BnplCustomerStatus? status,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task AddAsync(BnplCustomer customer, CancellationToken cancellationToken = default);

    Task UpdateAsync(BnplCustomer customer, CancellationToken cancellationToken = default);
}

public interface IBnplUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
