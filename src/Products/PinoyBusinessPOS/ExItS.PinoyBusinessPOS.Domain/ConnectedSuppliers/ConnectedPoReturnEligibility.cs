using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;

namespace ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

public sealed class ConnectedPoReturnEligibilityBucketId : IEquatable<ConnectedPoReturnEligibilityBucketId>
{
    public Guid Value { get; }

    private ConnectedPoReturnEligibilityBucketId(Guid value) => Value = value;

    public static ConnectedPoReturnEligibilityBucketId New() => new(Guid.NewGuid());

    public static ConnectedPoReturnEligibilityBucketId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReturnEligibilityBucketId,
                "Return eligibility bucket id cannot be empty.");
        }

        return new ConnectedPoReturnEligibilityBucketId(value);
    }

    public bool Equals(ConnectedPoReturnEligibilityBucketId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is ConnectedPoReturnEligibilityBucketId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");
}

public sealed class ConnectedPoReturnAllocationId : IEquatable<ConnectedPoReturnAllocationId>
{
    public Guid Value { get; }

    private ConnectedPoReturnAllocationId(Guid value) => Value = value;

    public static ConnectedPoReturnAllocationId New() => new(Guid.NewGuid());

    public static ConnectedPoReturnAllocationId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReturnAllocationId,
                "Return allocation id cannot be empty.");
        }

        return new ConnectedPoReturnAllocationId(value);
    }

    public bool Equals(ConnectedPoReturnAllocationId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is ConnectedPoReturnAllocationId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");
}

/// <summary>
/// Receipt-line eligibility lot for voluntary connected-PO returns.
/// One bucket per goods-receipt line with good QuantityReceived &gt; 0; policy is snapshotted at GRN.
/// </summary>
public sealed class ConnectedPoReturnEligibilityBucket
{
    private ConnectedPoReturnEligibilityBucket(
        ConnectedPoReturnEligibilityBucketId id,
        PosOrganizationId buyerOrganizationId,
        PosOrganizationId sellerOrganizationId,
        PurchaseOrderId purchaseOrderId,
        PurchaseOrderLineId purchaseOrderLineId,
        GoodsReceiptId goodsReceiptId,
        GoodsReceiptLineId goodsReceiptLineId,
        CatalogProductId? buyerProductId,
        CatalogProductId? supplierProductId,
        decimal quantityReceived,
        decimal quantityAllocated,
        DateTimeOffset receivedAtUtc,
        bool policyReturnsAllowed,
        int? policyReturnWindowDays,
        int policyReceivingIssueWindowDays,
        bool policyRequireReturnApproval,
        ConnectedPoReturnPolicySource policySource,
        DateTimeOffset? returnExpiresAtUtc,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        BuyerOrganizationId = buyerOrganizationId;
        SellerOrganizationId = sellerOrganizationId;
        PurchaseOrderId = purchaseOrderId;
        PurchaseOrderLineId = purchaseOrderLineId;
        GoodsReceiptId = goodsReceiptId;
        GoodsReceiptLineId = goodsReceiptLineId;
        BuyerProductId = buyerProductId;
        SupplierProductId = supplierProductId;
        QuantityReceived = quantityReceived;
        QuantityAllocated = quantityAllocated;
        ReceivedAtUtc = receivedAtUtc;
        PolicyReturnsAllowed = policyReturnsAllowed;
        PolicyReturnWindowDays = policyReturnWindowDays;
        PolicyReceivingIssueWindowDays = policyReceivingIssueWindowDays;
        PolicyRequireReturnApproval = policyRequireReturnApproval;
        PolicySource = policySource;
        ReturnExpiresAtUtc = returnExpiresAtUtc;
        CreatedAtUtc = createdAtUtc;
    }

    public ConnectedPoReturnEligibilityBucketId Id { get; }
    public PosOrganizationId BuyerOrganizationId { get; }
    public PosOrganizationId SellerOrganizationId { get; }
    public PurchaseOrderId PurchaseOrderId { get; }
    public PurchaseOrderLineId PurchaseOrderLineId { get; }
    public GoodsReceiptId GoodsReceiptId { get; }
    public GoodsReceiptLineId GoodsReceiptLineId { get; }
    public CatalogProductId? BuyerProductId { get; }
    public CatalogProductId? SupplierProductId { get; }
    public decimal QuantityReceived { get; }
    public decimal QuantityAllocated { get; private set; }
    public DateTimeOffset ReceivedAtUtc { get; }
    public bool PolicyReturnsAllowed { get; }
    public int? PolicyReturnWindowDays { get; }
    public int PolicyReceivingIssueWindowDays { get; }
    public bool PolicyRequireReturnApproval { get; }
    public ConnectedPoReturnPolicySource PolicySource { get; }
    public DateTimeOffset? ReturnExpiresAtUtc { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    public decimal RemainingQuantity => QuantityReceived - QuantityAllocated;

    public static ConnectedPoReturnEligibilityBucket CreateFromReceiptLine(
        PosOrganizationId buyerOrganizationId,
        PosOrganizationId sellerOrganizationId,
        PurchaseOrderId purchaseOrderId,
        GoodsReceipt goodsReceipt,
        GoodsReceiptLine line,
        CatalogProductId? buyerProductId,
        CatalogProductId? supplierProductId,
        EffectiveConnectedPoReturnPolicy policy,
        DateTimeOffset utcNow,
        ConnectedPoReturnEligibilityBucketId? id = null)
    {
        if (line.QuantityReceived <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReturnEligibilityBucket,
                "Eligibility buckets require good received quantity greater than zero.");
        }

        var expires = policy.ReturnsAllowed
            ? ConnectedPoReturnPolicyResolver.ComputeReturnExpiresAtUtc(
                goodsReceipt.ReceivedAtUtc,
                policy.ReturnWindowDays)
            : null;

        return new ConnectedPoReturnEligibilityBucket(
            id ?? ConnectedPoReturnEligibilityBucketId.New(),
            buyerOrganizationId,
            sellerOrganizationId,
            purchaseOrderId,
            line.PurchaseOrderLineId,
            goodsReceipt.Id,
            line.Id,
            buyerProductId,
            supplierProductId,
            line.QuantityReceived,
            quantityAllocated: 0m,
            goodsReceipt.ReceivedAtUtc,
            policy.ReturnsAllowed,
            policy.ReturnWindowDays,
            policy.ReceivingIssueWindowDays,
            policy.RequireReturnApproval,
            policy.Source,
            expires,
            utcNow);
    }

    public static ConnectedPoReturnEligibilityBucket Rehydrate(
        ConnectedPoReturnEligibilityBucketId id,
        PosOrganizationId buyerOrganizationId,
        PosOrganizationId sellerOrganizationId,
        PurchaseOrderId purchaseOrderId,
        PurchaseOrderLineId purchaseOrderLineId,
        GoodsReceiptId goodsReceiptId,
        GoodsReceiptLineId goodsReceiptLineId,
        CatalogProductId? buyerProductId,
        CatalogProductId? supplierProductId,
        decimal quantityReceived,
        decimal quantityAllocated,
        DateTimeOffset receivedAtUtc,
        bool policyReturnsAllowed,
        int? policyReturnWindowDays,
        int policyReceivingIssueWindowDays,
        bool policyRequireReturnApproval,
        ConnectedPoReturnPolicySource policySource,
        DateTimeOffset? returnExpiresAtUtc,
        DateTimeOffset createdAtUtc) =>
        new(
            id,
            buyerOrganizationId,
            sellerOrganizationId,
            purchaseOrderId,
            purchaseOrderLineId,
            goodsReceiptId,
            goodsReceiptLineId,
            buyerProductId,
            supplierProductId,
            quantityReceived,
            quantityAllocated,
            receivedAtUtc,
            policyReturnsAllowed,
            policyReturnWindowDays,
            policyReceivingIssueWindowDays,
            policyRequireReturnApproval,
            policySource,
            returnExpiresAtUtc,
            createdAtUtc);

    public bool IsVoluntarilyEligibleAt(DateTimeOffset utcNow)
    {
        if (!PolicyReturnsAllowed || RemainingQuantity <= 0m)
        {
            return false;
        }

        return ReturnExpiresAtUtc is null || utcNow <= ReturnExpiresAtUtc.Value;
    }

    public void Allocate(decimal quantity)
    {
        if (quantity <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReturnAllocation,
                "Allocation quantity must be positive.");
        }

        if (quantity > RemainingQuantity)
        {
            throw new DomainException(
                DomainErrorCodes.ConnectedPoReturnQuantityExceedsEligible,
                "Allocation quantity exceeds remaining eligible received quantity.");
        }

        QuantityAllocated += quantity;
    }
}

/// <summary>Links a return-batch line to the receipt eligibility bucket(s) that funded it.</summary>
public sealed class ConnectedPoReturnAllocation
{
    private ConnectedPoReturnAllocation(
        ConnectedPoReturnAllocationId id,
        ReturnBatchLineId returnBatchLineId,
        ConnectedPoReturnEligibilityBucketId eligibilityBucketId,
        decimal quantity,
        DateTimeOffset createdAtUtc)
    {
        Id = id;
        ReturnBatchLineId = returnBatchLineId;
        EligibilityBucketId = eligibilityBucketId;
        Quantity = quantity;
        CreatedAtUtc = createdAtUtc;
    }

    public ConnectedPoReturnAllocationId Id { get; }
    public ReturnBatchLineId ReturnBatchLineId { get; }
    public ConnectedPoReturnEligibilityBucketId EligibilityBucketId { get; }
    public decimal Quantity { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    public static ConnectedPoReturnAllocation Create(
        ReturnBatchLineId returnBatchLineId,
        ConnectedPoReturnEligibilityBucketId eligibilityBucketId,
        decimal quantity,
        DateTimeOffset utcNow,
        ConnectedPoReturnAllocationId? id = null)
    {
        if (quantity <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReturnAllocation,
                "Allocation quantity must be positive.");
        }

        return new ConnectedPoReturnAllocation(
            id ?? ConnectedPoReturnAllocationId.New(),
            returnBatchLineId,
            eligibilityBucketId,
            quantity,
            utcNow);
    }

    public static ConnectedPoReturnAllocation Rehydrate(
        ConnectedPoReturnAllocationId id,
        ReturnBatchLineId returnBatchLineId,
        ConnectedPoReturnEligibilityBucketId eligibilityBucketId,
        decimal quantity,
        DateTimeOffset createdAtUtc) =>
        new(id, returnBatchLineId, eligibilityBucketId, quantity, createdAtUtc);
}

/// <summary>FIFO voluntary return allocation across receipt eligibility buckets.</summary>
public static class ConnectedPoReturnBucketAllocator
{
    public readonly record struct AllocationSlice(
        ConnectedPoReturnEligibilityBucket Bucket,
        decimal Quantity);

    public static IReadOnlyList<AllocationSlice> AllocateFifo(
        IReadOnlyList<ConnectedPoReturnEligibilityBucket> buckets,
        decimal requestedQuantity,
        DateTimeOffset utcNow)
    {
        if (requestedQuantity <= 0m)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidConnectedPoReturnAllocation,
                "Return quantity must be positive.");
        }

        var remaining = requestedQuantity;
        var slices = new List<AllocationSlice>();
        foreach (var bucket in buckets
                     .Where(b => b.IsVoluntarilyEligibleAt(utcNow))
                     .OrderBy(b => b.ReceivedAtUtc)
                     .ThenBy(b => b.GoodsReceiptLineId.Value))
        {
            if (remaining <= 0m)
            {
                break;
            }

            var take = Math.Min(remaining, bucket.RemainingQuantity);
            if (take <= 0m)
            {
                continue;
            }

            slices.Add(new AllocationSlice(bucket, take));
            remaining -= take;
        }

        if (remaining > 0m)
        {
            throw new DomainException(
                DomainErrorCodes.ConnectedPoReturnQuantityExceedsEligible,
                "Requested return quantity exceeds currently eligible received quantity.");
        }

        return slices;
    }
}
