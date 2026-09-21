using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.ConnectedSuppliers;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;

internal sealed class ConnectedPoReturnEligibilityBucketRepository(PosDbContext db)
    : IConnectedPoReturnEligibilityBucketRepository
{
    public async Task AddAsync(
        ConnectedPoReturnEligibilityBucket bucket,
        CancellationToken cancellationToken = default)
    {
        await db.ConnectedPoReturnEligibilityBuckets
            .AddAsync(ConnectedPoReturnEligibilityMapper.ToRecord(bucket), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task AddRangeAsync(
        IReadOnlyList<ConnectedPoReturnEligibilityBucket> buckets,
        CancellationToken cancellationToken = default)
    {
        if (buckets.Count == 0)
        {
            return;
        }

        await db.ConnectedPoReturnEligibilityBuckets
            .AddRangeAsync(buckets.Select(ConnectedPoReturnEligibilityMapper.ToRecord), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task UpdateAsync(
        ConnectedPoReturnEligibilityBucket bucket,
        CancellationToken cancellationToken = default)
    {
        var record = await db.ConnectedPoReturnEligibilityBuckets
            .FirstOrDefaultAsync(x => x.Id == bucket.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (record is null)
        {
            db.ConnectedPoReturnEligibilityBuckets.Update(ConnectedPoReturnEligibilityMapper.ToRecord(bucket));
            return;
        }

        ConnectedPoReturnEligibilityMapper.Apply(bucket, record);
    }

    public async Task AddAllocationAsync(
        ConnectedPoReturnAllocation allocation,
        CancellationToken cancellationToken = default)
    {
        await db.ConnectedPoReturnAllocations
            .AddAsync(ConnectedPoReturnEligibilityMapper.ToRecord(allocation), cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ConnectedPoReturnEligibilityBucket>> ListByPurchaseOrderAsync(
        PosOrganizationId buyerOrganizationId,
        PurchaseOrderId purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.ConnectedPoReturnEligibilityBuckets
            .AsNoTracking()
            .Where(x =>
                x.BuyerOrganizationId == buyerOrganizationId.Value
                && x.PurchaseOrderId == purchaseOrderId.Value)
            .OrderBy(x => x.ReceivedAtUtc)
            .ThenBy(x => x.GoodsReceiptLineId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(ConnectedPoReturnEligibilityMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<ConnectedPoReturnEligibilityBucket>> ListByPurchaseOrderLineAsync(
        PosOrganizationId buyerOrganizationId,
        PurchaseOrderLineId purchaseOrderLineId,
        CancellationToken cancellationToken = default)
    {
        var rows = await db.ConnectedPoReturnEligibilityBuckets
            .AsNoTracking()
            .Where(x =>
                x.BuyerOrganizationId == buyerOrganizationId.Value
                && x.PurchaseOrderLineId == purchaseOrderLineId.Value)
            .OrderBy(x => x.ReceivedAtUtc)
            .ThenBy(x => x.GoodsReceiptLineId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(ConnectedPoReturnEligibilityMapper.ToDomain).ToList();
    }

    public async Task<ConnectedPoReturnEligibilityBucket?> GetByGoodsReceiptLineAsync(
        GoodsReceiptLineId goodsReceiptLineId,
        CancellationToken cancellationToken = default)
    {
        var row = await db.ConnectedPoReturnEligibilityBuckets
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.GoodsReceiptLineId == goodsReceiptLineId.Value, cancellationToken)
            .ConfigureAwait(false);
        return row is null ? null : ConnectedPoReturnEligibilityMapper.ToDomain(row);
    }
}
