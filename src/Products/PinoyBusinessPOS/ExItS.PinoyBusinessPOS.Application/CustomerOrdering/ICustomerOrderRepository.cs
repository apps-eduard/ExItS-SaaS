using ExItS.PinoyBusinessPOS.Domain.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.Application.CustomerOrdering;

public sealed record CustomerOrderFilter(
    CustomerOrderStatus? Status = null,
    CustomerOrderFulfillmentType? FulfillmentType = null,
    Guid? FulfillmentBranchId = null,
    string? OrderNumber = null,
    Guid? CustomerPlatformUserId = null,
    Guid? CustomerBuyerOrganizationId = null);

public interface ICustomerOrderRepository
{
    Task<CustomerOrder?> GetByIdAsync(
        PosOrganizationId sellerOrganizationId,
        CustomerOrderId orderId,
        CancellationToken cancellationToken = default);

    Task<CustomerOrder?> FindByIdempotencyKeyAsync(
        PosOrganizationId sellerOrganizationId,
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CustomerOrder> Items, int TotalCount)> ListAsync(
        PosOrganizationId sellerOrganizationId,
        CustomerOrderFilter filter,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<CustomerOrder> Items, int TotalCount)> ListForCustomerPartyAsync(
        CustomerPartyType partyType,
        Guid? platformUserId,
        Guid? buyerOrganizationId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<CustomerOrder?> GetForCustomerPartyAsync(
        CustomerOrderId orderId,
        CustomerPartyType partyType,
        Guid? platformUserId,
        Guid? buyerOrganizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Allocates the next org-scoped order number, builds the order, optionally runs
    /// <paramref name="afterCreated"/>, then persists in one transaction.
    /// </summary>
    Task<CustomerOrder> PlaceAsync(
        PosOrganizationId sellerOrganizationId,
        Func<string, CustomerOrder> createOrder,
        Func<CustomerOrder, CancellationToken, Task>? afterCreated = null,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(CustomerOrder order, CancellationToken cancellationToken = default);
}
