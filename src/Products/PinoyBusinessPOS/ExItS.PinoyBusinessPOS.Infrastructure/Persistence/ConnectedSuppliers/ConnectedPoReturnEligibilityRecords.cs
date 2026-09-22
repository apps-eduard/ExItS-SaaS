using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;
using ExItS.PinoyBusinessPOS.Domain.Returns;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.ConnectedSuppliers;

internal sealed class ConnectedPoReturnEligibilityBucketRecord
{
    public Guid Id { get; set; }
    public Guid BuyerOrganizationId { get; set; }
    public Guid SellerOrganizationId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public Guid GoodsReceiptLineId { get; set; }
    public Guid? BuyerProductId { get; set; }
    public Guid? SupplierProductId { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityAllocated { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }
    public bool PolicyReturnsAllowed { get; set; }
    public int? PolicyReturnWindowDays { get; set; }
    public int PolicyReceivingIssueWindowDays { get; set; }
    public bool PolicyRequireReturnApproval { get; set; }
    public short PolicySource { get; set; }
    public DateTimeOffset? ReturnExpiresAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

internal sealed class ConnectedPoReturnAllocationRecord
{
    public Guid Id { get; set; }
    public Guid ReturnBatchLineId { get; set; }
    public Guid EligibilityBucketId { get; set; }
    public decimal Quantity { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

internal static class ConnectedPoReturnEligibilityMapper
{
    public static ConnectedPoReturnEligibilityBucket ToDomain(ConnectedPoReturnEligibilityBucketRecord r) =>
        ConnectedPoReturnEligibilityBucket.Rehydrate(
            ConnectedPoReturnEligibilityBucketId.From(r.Id),
            PosOrganizationId.From(r.BuyerOrganizationId),
            PosOrganizationId.From(r.SellerOrganizationId),
            PurchaseOrderId.From(r.PurchaseOrderId),
            PurchaseOrderLineId.From(r.PurchaseOrderLineId),
            GoodsReceiptId.From(r.GoodsReceiptId),
            GoodsReceiptLineId.From(r.GoodsReceiptLineId),
            r.BuyerProductId is null ? null : CatalogProductId.From(r.BuyerProductId.Value),
            r.SupplierProductId is null ? null : CatalogProductId.From(r.SupplierProductId.Value),
            r.QuantityReceived,
            r.QuantityAllocated,
            r.ReceivedAtUtc,
            r.PolicyReturnsAllowed,
            r.PolicyReturnWindowDays,
            r.PolicyReceivingIssueWindowDays,
            r.PolicyRequireReturnApproval,
            (ConnectedPoReturnPolicySource)r.PolicySource,
            r.ReturnExpiresAtUtc,
            r.CreatedAtUtc);

    public static ConnectedPoReturnEligibilityBucketRecord ToRecord(ConnectedPoReturnEligibilityBucket x) =>
        new()
        {
            Id = x.Id.Value,
            BuyerOrganizationId = x.BuyerOrganizationId.Value,
            SellerOrganizationId = x.SellerOrganizationId.Value,
            PurchaseOrderId = x.PurchaseOrderId.Value,
            PurchaseOrderLineId = x.PurchaseOrderLineId.Value,
            GoodsReceiptId = x.GoodsReceiptId.Value,
            GoodsReceiptLineId = x.GoodsReceiptLineId.Value,
            BuyerProductId = x.BuyerProductId?.Value,
            SupplierProductId = x.SupplierProductId?.Value,
            QuantityReceived = x.QuantityReceived,
            QuantityAllocated = x.QuantityAllocated,
            ReceivedAtUtc = x.ReceivedAtUtc,
            PolicyReturnsAllowed = x.PolicyReturnsAllowed,
            PolicyReturnWindowDays = x.PolicyReturnWindowDays,
            PolicyReceivingIssueWindowDays = x.PolicyReceivingIssueWindowDays,
            PolicyRequireReturnApproval = x.PolicyRequireReturnApproval,
            PolicySource = (short)x.PolicySource,
            ReturnExpiresAtUtc = x.ReturnExpiresAtUtc,
            CreatedAtUtc = x.CreatedAtUtc
        };

    public static void Apply(ConnectedPoReturnEligibilityBucket x, ConnectedPoReturnEligibilityBucketRecord r)
    {
        r.QuantityAllocated = x.QuantityAllocated;
    }

    public static ConnectedPoReturnAllocation ToDomain(ConnectedPoReturnAllocationRecord r) =>
        ConnectedPoReturnAllocation.Rehydrate(
            ConnectedPoReturnAllocationId.From(r.Id),
            ReturnBatchLineId.From(r.ReturnBatchLineId),
            ConnectedPoReturnEligibilityBucketId.From(r.EligibilityBucketId),
            r.Quantity,
            r.CreatedAtUtc);

    public static ConnectedPoReturnAllocationRecord ToRecord(ConnectedPoReturnAllocation x) =>
        new()
        {
            Id = x.Id.Value,
            ReturnBatchLineId = x.ReturnBatchLineId.Value,
            EligibilityBucketId = x.EligibilityBucketId.Value,
            Quantity = x.Quantity,
            CreatedAtUtc = x.CreatedAtUtc
        };
}
