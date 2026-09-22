using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.Domain.Abstractions;

public interface IConnectedPoReturnEligibilityBucketRepository
{
    Task AddAsync(
        ConnectedPoReturnEligibilityBucket bucket,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(
        IReadOnlyList<ConnectedPoReturnEligibilityBucket> buckets,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ConnectedPoReturnEligibilityBucket bucket,
        CancellationToken cancellationToken = default);

    Task AddAllocationAsync(
        ConnectedPoReturnAllocation allocation,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConnectedPoReturnEligibilityBucket>> ListByPurchaseOrderAsync(
        PosOrganizationId buyerOrganizationId,
        PurchaseOrderId purchaseOrderId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ConnectedPoReturnEligibilityBucket>> ListByPurchaseOrderLineAsync(
        PosOrganizationId buyerOrganizationId,
        PurchaseOrderLineId purchaseOrderLineId,
        CancellationToken cancellationToken = default);

    Task<ConnectedPoReturnEligibilityBucket?> GetByGoodsReceiptLineAsync(
        GoodsReceiptLineId goodsReceiptLineId,
        CancellationToken cancellationToken = default);
}
