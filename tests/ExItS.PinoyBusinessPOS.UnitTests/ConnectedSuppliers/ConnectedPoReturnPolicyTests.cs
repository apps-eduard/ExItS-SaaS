using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Purchasing;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class ConnectedPoReturnPolicyTests
{
    private static readonly PosOrganizationId Seller =
        PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly PosOrganizationId Buyer =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly DateTimeOffset ReceivedAt = new(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Org_default_applies_when_no_override_exists()
    {
        var settings = OrganizationConnectedCommerceSettings.CreateDefault(Seller, ReceivedAt);
        settings.ConfigureReturnPolicy(true, 7, 2, true, [], ReceivedAt);

        var policy = ConnectedPoReturnPolicyResolver.ResolveFromSettings(
            settings,
            categoryId: null,
            ConnectedPoReturnPolicyMode.UseDefault,
            null,
            null);

        Assert.True(policy.ReturnsAllowed);
        Assert.Equal(7, policy.ReturnWindowDays);
        Assert.Equal(ConnectedPoReturnPolicySource.Organization, policy.Source);
    }

    [Fact]
    public void Category_override_beats_org_default()
    {
        var categoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var settings = OrganizationConnectedCommerceSettings.CreateDefault(Seller, ReceivedAt);
        settings.ConfigureReturnPolicy(
            true,
            7,
            2,
            true,
            [new OrganizationConnectedCommerceCategoryReturnRule(
                categoryId,
                ConnectedPoReturnPolicyMode.Custom,
                ReturnsAllowed: true,
                ReturnWindowDays: 3)],
            ReceivedAt);

        var policy = ConnectedPoReturnPolicyResolver.ResolveFromSettings(
            settings,
            categoryId,
            ConnectedPoReturnPolicyMode.UseDefault,
            null,
            null);

        Assert.Equal(3, policy.ReturnWindowDays);
        Assert.Equal(ConnectedPoReturnPolicySource.Category, policy.Source);
    }

    [Fact]
    public void Product_custom_beats_category()
    {
        var categoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var settings = OrganizationConnectedCommerceSettings.CreateDefault(Seller, ReceivedAt);
        settings.ConfigureReturnPolicy(
            true,
            7,
            2,
            true,
            [new OrganizationConnectedCommerceCategoryReturnRule(
                categoryId,
                ConnectedPoReturnPolicyMode.Custom,
                true,
                3)],
            ReceivedAt);

        var policy = ConnectedPoReturnPolicyResolver.ResolveFromSettings(
            settings,
            categoryId,
            ConnectedPoReturnPolicyMode.Custom,
            true,
            14);

        Assert.Equal(14, policy.ReturnWindowDays);
        Assert.Equal(ConnectedPoReturnPolicySource.Product, policy.Source);
    }

    [Fact]
    public void Product_non_returnable_beats_everything()
    {
        var categoryId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var settings = OrganizationConnectedCommerceSettings.CreateDefault(Seller, ReceivedAt);
        settings.ConfigureReturnPolicy(
            true,
            7,
            2,
            true,
            [new OrganizationConnectedCommerceCategoryReturnRule(
                categoryId,
                ConnectedPoReturnPolicyMode.Custom,
                true,
                3)],
            ReceivedAt);

        var policy = ConnectedPoReturnPolicyResolver.ResolveFromSettings(
            settings,
            categoryId,
            ConnectedPoReturnPolicyMode.NonReturnable,
            null,
            null);

        Assert.False(policy.ReturnsAllowed);
        Assert.Equal(ConnectedPoReturnPolicySource.Product, policy.Source);
    }

    [Fact]
    public void Snapshot_is_independent_of_later_live_policy_change()
    {
        var policyAtReceipt = new EffectiveConnectedPoReturnPolicy(
            true, 7, 2, true, ConnectedPoReturnPolicySource.Organization);
        var expires = ConnectedPoReturnPolicyResolver.ComputeReturnExpiresAtUtc(ReceivedAt, 7);
        var laterPolicy = new EffectiveConnectedPoReturnPolicy(
            false, null, 2, true, ConnectedPoReturnPolicySource.Organization);

        Assert.True(policyAtReceipt.ReturnsAllowed);
        Assert.False(laterPolicy.ReturnsAllowed);
        Assert.Equal(ReceivedAt.AddDays(7), expires);
    }

    [Fact]
    public void Return_window_starts_from_goods_receipt_not_fulfillment()
    {
        var expires = ConnectedPoReturnPolicyResolver.ComputeReturnExpiresAtUtc(ReceivedAt, 7);
        Assert.Equal(new DateTimeOffset(2026, 9, 28, 12, 0, 0, TimeSpan.Zero), expires);
    }

    [Fact]
    public void Voluntary_return_allowed_inside_window_and_rejected_after()
    {
        var policy = new EffectiveConnectedPoReturnPolicy(true, 7, 2, true, ConnectedPoReturnPolicySource.Organization);
        Assert.True(ConnectedPoReturnPolicyResolver.IsWithinVoluntaryReturnWindow(policy, ReceivedAt, ReceivedAt.AddDays(6)));
        Assert.False(ConnectedPoReturnPolicyResolver.IsWithinVoluntaryReturnWindow(policy, ReceivedAt, ReceivedAt.AddDays(8)));
    }

    [Fact]
    public void Partial_receipts_have_independent_expiry_and_fifo_allocation()
    {
        var poId = PurchaseOrderId.New();
        var poLineId = PurchaseOrderLineId.New();
        var policy = new EffectiveConnectedPoReturnPolicy(true, 7, 2, true, ConnectedPoReturnPolicySource.Organization);
        var first = BuildBucket(poId, poLineId, 5m, ReceivedAt, policy);
        var second = BuildBucket(poId, poLineId, 5m, ReceivedAt.AddDays(4), policy);

        var slices = ConnectedPoReturnBucketAllocator.AllocateFifo(
            [first, second], 6m, ReceivedAt.AddDays(5));

        Assert.Equal(2, slices.Count);
        Assert.Equal(first.Id, slices[0].Bucket.Id);
        Assert.Equal(5m, slices[0].Quantity);
        Assert.Equal(second.Id, slices[1].Bucket.Id);
        Assert.Equal(1m, slices[1].Quantity);
    }

    [Fact]
    public void Expired_bucket_is_not_voluntarily_eligible()
    {
        var policy = new EffectiveConnectedPoReturnPolicy(true, 7, 2, true, ConnectedPoReturnPolicySource.Organization);
        var bucket = BuildBucket(PurchaseOrderId.New(), PurchaseOrderLineId.New(), 5m, ReceivedAt, policy);
        Assert.False(bucket.IsVoluntarilyEligibleAt(ReceivedAt.AddDays(8)));
        var ex = Assert.Throws<DomainException>(() =>
            ConnectedPoReturnBucketAllocator.AllocateFifo([bucket], 1m, ReceivedAt.AddDays(8)));
        Assert.Equal(DomainErrorCodes.ConnectedPoReturnQuantityExceedsEligible, ex.ErrorCode);
    }

    [Fact]
    public void Non_returnable_bucket_blocks_voluntary_allocation()
    {
        var policy = new EffectiveConnectedPoReturnPolicy(
            false, null, 2, true, ConnectedPoReturnPolicySource.Product);
        var bucket = BuildBucket(PurchaseOrderId.New(), PurchaseOrderLineId.New(), 5m, ReceivedAt, policy);
        Assert.False(bucket.IsVoluntarilyEligibleAt(ReceivedAt));
        Assert.False(policy.ReturnsAllowed);
    }

    [Fact]
    public void Allocation_cannot_exceed_remaining_quantity()
    {
        var policy = new EffectiveConnectedPoReturnPolicy(true, null, 2, true, ConnectedPoReturnPolicySource.Organization);
        var bucket = BuildBucket(PurchaseOrderId.New(), PurchaseOrderLineId.New(), 4m, ReceivedAt, policy);
        bucket.Allocate(3m);
        var ex = Assert.Throws<DomainException>(() => bucket.Allocate(2m));
        Assert.Equal(DomainErrorCodes.ConnectedPoReturnQuantityExceedsEligible, ex.ErrorCode);
    }

    [Fact]
    public void Catalog_product_configure_return_policy_modes()
    {
        var product = CatalogProduct.Create(Seller, "Widget", UnitOfMeasure.Piece, 10m, ReceivedAt);
        product.ConfigureReturnPolicy(ConnectedPoReturnPolicyMode.NonReturnable, null, null, ReceivedAt);
        Assert.Equal(ConnectedPoReturnPolicyMode.NonReturnable, product.ReturnPolicyMode);

        product.ConfigureReturnPolicy(ConnectedPoReturnPolicyMode.Custom, true, 14, ReceivedAt.AddMinutes(1));
        Assert.Equal(ConnectedPoReturnPolicyMode.Custom, product.ReturnPolicyMode);
        Assert.True(product.ReturnPolicyReturnsAllowed);
        Assert.Equal(14, product.ReturnPolicyWindowDays);
    }

    private static ConnectedPoReturnEligibilityBucket BuildBucket(
        PurchaseOrderId purchaseOrderId,
        PurchaseOrderLineId purchaseOrderLineId,
        decimal qty,
        DateTimeOffset receivedAt,
        EffectiveConnectedPoReturnPolicy policy)
    {
        var expires = policy.ReturnsAllowed
            ? ConnectedPoReturnPolicyResolver.ComputeReturnExpiresAtUtc(receivedAt, policy.ReturnWindowDays)
            : null;

        return ConnectedPoReturnEligibilityBucket.Rehydrate(
            ConnectedPoReturnEligibilityBucketId.New(),
            Buyer,
            Seller,
            purchaseOrderId,
            purchaseOrderLineId,
            GoodsReceiptId.New(),
            GoodsReceiptLineId.New(),
            CatalogProductId.New(),
            CatalogProductId.New(),
            qty,
            quantityAllocated: 0m,
            receivedAt,
            policy.ReturnsAllowed,
            policy.ReturnWindowDays,
            policy.ReceivingIssueWindowDays,
            policy.RequireReturnApproval,
            policy.Source,
            expires,
            receivedAt);
    }
}
