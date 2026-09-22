using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class ConnectedBuyerCatalogProjectionTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-20T08:00:00Z");

    private static CatalogProduct Sellable(string name = "Apple") =>
        CatalogProduct.Create(Org, name, UnitOfMeasure.Piece, 12m, Now);

    [Fact]
    public void Tracked_eligible_product_is_effectively_shared_under_AllEligible()
    {
        var product = Sellable();
        Assert.True(ConnectedBuyerCatalogProjection.IsEligible(product, isInventoryTracked: true));
        Assert.True(ConnectedBuyerCatalogProjection.IsEffectivelyShared(
            CatalogSharingMode.AllEligible, product, isInventoryTracked: true, share: null));
        Assert.Equal(
            "Shared",
            ConnectedBuyerCatalogProjection.SharingStatus(
                CatalogSharingMode.AllEligible, product, true, null));
    }

    [Fact]
    public void Untracked_product_is_not_eligible_or_effectively_shared()
    {
        var product = Sellable();
        Assert.False(ConnectedBuyerCatalogProjection.IsEligible(product, isInventoryTracked: false));
        Assert.False(ConnectedBuyerCatalogProjection.IsEffectivelyShared(
            CatalogSharingMode.AllEligible, product, isInventoryTracked: false, share: null));
        Assert.Equal(
            "Ineligible",
            ConnectedBuyerCatalogProjection.SharingStatus(
                CatalogSharingMode.AllEligible, product, false, null));
    }

    [Fact]
    public void Explicit_exclusion_survives_tracking_cycle_preference()
    {
        var product = Sellable();
        var share = ConnectedBuyerProductShare.Share(
            ConnectedSupplierRelationshipId.New(),
            PosOrganizationId.From(Guid.NewGuid()),
            Org,
            product.Id,
            Now);
        share.Unshare(Now);

        Assert.True(ConnectedBuyerCatalogProjection.IsExplicitlyExcluded(share));
        Assert.False(ConnectedBuyerCatalogProjection.IsEffectivelyShared(
            CatalogSharingMode.AllEligible, product, true, share));
        Assert.Equal(
            "Excluded",
            ConnectedBuyerCatalogProjection.SharingStatus(
                CatalogSharingMode.AllEligible, product, true, share));

        // Tracking off then on: exclusion preference remains.
        Assert.Equal(
            "Ineligible",
            ConnectedBuyerCatalogProjection.SharingStatus(
                CatalogSharingMode.AllEligible, product, false, share));
        Assert.Equal(
            "Excluded",
            ConnectedBuyerCatalogProjection.SharingStatus(
                CatalogSharingMode.AllEligible, product, true, share));
    }

    [Fact]
    public void SelectedOnly_does_not_auto_share_without_row()
    {
        var product = Sellable();
        Assert.False(ConnectedBuyerCatalogProjection.IsEffectivelyShared(
            CatalogSharingMode.SelectedOnly, product, true, share: null));
        Assert.Equal(
            "NotShared",
            ConnectedBuyerCatalogProjection.SharingStatus(
                CatalogSharingMode.SelectedOnly, product, true, null));
    }

    [Fact]
    public void MapForManagement_never_reports_NotTracked_plus_Shared()
    {
        var product = Sellable();
        var relationship = ConnectedSupplierRelationship.Request(
            PosOrganizationId.From(Guid.NewGuid()),
            Org,
            Now);
        relationship.Approve(Now);
        relationship.ConfigureCatalogSharing(CatalogSharingMode.AllEligible, null, Now);

        var dto = ConnectedSupplierMapper.MapForManagement(
            relationship,
            product,
            share: null,
            exposure: null,
            categoryName: null,
            isInventoryTracked: false);

        Assert.False(dto.IsInventoryTracked);
        Assert.False(dto.IsEligible);
        Assert.False(dto.IsEffectivelyShared);
        Assert.False(dto.IsShared);
        Assert.Equal("Ineligible", dto.SharingStatus);
        Assert.False(dto.CanShare);
        Assert.False(dto.CanStopSharing);
    }

    [Fact]
    public void MapForManagement_tracked_AllEligible_is_Shared()
    {
        var product = Sellable();
        var relationship = ConnectedSupplierRelationship.Request(
            PosOrganizationId.From(Guid.NewGuid()),
            Org,
            Now);
        relationship.Approve(Now);
        relationship.ConfigureCatalogSharing(CatalogSharingMode.AllEligible, null, Now);

        var dto = ConnectedSupplierMapper.MapForManagement(
            relationship,
            product,
            share: null,
            exposure: null,
            categoryName: null,
            isInventoryTracked: true);

        Assert.True(dto.IsInventoryTracked);
        Assert.True(dto.IsEligible);
        Assert.True(dto.IsEffectivelyShared);
        Assert.True(dto.IsShared);
        Assert.Equal("Shared", dto.SharingStatus);
        Assert.Equal(12m, dto.SellingPrice);
        Assert.Equal(12m, dto.ResolvedPoPrice);
        Assert.True(dto.HasValidPoPrice);
        Assert.False(dto.CanShare);
        Assert.True(dto.CanStopSharing);
        Assert.NotNull(dto.EffectiveSupplierOrderPrice);
    }

    [Fact]
    public void ResolvePoPrice_selling_200_null_default_is_valid()
    {
        var product = CatalogProduct.Create(Org, "Apple", UnitOfMeasure.Piece, 200m, Now);
        Assert.Null(product.DefaultConnectedPoPrice);
        Assert.Equal(200m, ConnectedBuyerCatalogProjection.ResolvePoPrice(product));
        Assert.True(ConnectedBuyerCatalogProjection.HasValidPoPrice(product));
    }

    [Fact]
    public void ResolvePoPrice_default_po_takes_precedence_over_selling()
    {
        var product = CatalogProduct.Create(Org, "Apple", UnitOfMeasure.Piece, 200m, Now);
        product.SetDefaultConnectedPoPrice(180m, Now);
        Assert.Equal(180m, ConnectedBuyerCatalogProjection.ResolvePoPrice(product));
        Assert.True(ConnectedBuyerCatalogProjection.HasValidPoPrice(product));
    }

    [Fact]
    public void ResolvePoPrice_both_missing_is_invalid()
    {
        var product = CatalogProduct.Create(Org, "Zero", UnitOfMeasure.Piece, 0m, Now);
        Assert.Null(ConnectedBuyerCatalogProjection.ResolvePoPrice(product));
        Assert.False(ConnectedBuyerCatalogProjection.HasValidPoPrice(product));
    }

    [Fact]
    public void Untracked_row_cannot_share_or_select()
    {
        var product = Sellable();
        Assert.False(ConnectedBuyerCatalogProjection.CanShare(
            CatalogSharingMode.AllEligible, product, isInventoryTracked: false, share: null));
        Assert.False(ConnectedBuyerCatalogProjection.CanStopSharing(
            CatalogSharingMode.AllEligible, product, isInventoryTracked: false, share: null));
        Assert.False(ConnectedBuyerCatalogProjection.IsSelectableForBulk(
            CatalogSharingMode.AllEligible, product, isInventoryTracked: false, share: null));
    }

    [Fact]
    public void Missing_price_excluded_product_cannot_share_until_priced()
    {
        var product = CatalogProduct.Create(Org, "NoPrice", UnitOfMeasure.Piece, 0m, Now);
        var share = ConnectedBuyerProductShare.Share(
            ConnectedSupplierRelationshipId.New(),
            PosOrganizationId.From(Guid.NewGuid()),
            Org,
            product.Id,
            Now);
        share.Unshare(Now);

        Assert.False(ConnectedBuyerCatalogProjection.HasValidPoPrice(product));
        Assert.False(ConnectedBuyerCatalogProjection.CanShare(
            CatalogSharingMode.AllEligible, product, true, share));
        Assert.False(ConnectedBuyerCatalogProjection.CanStopSharing(
            CatalogSharingMode.AllEligible, product, true, share));

        product.SetDefaultConnectedPoPrice(15m, Now);
        Assert.True(ConnectedBuyerCatalogProjection.HasValidPoPrice(product));
        Assert.True(ConnectedBuyerCatalogProjection.CanShare(
            CatalogSharingMode.AllEligible, product, true, share));
    }

    [Fact]
    public void Shared_product_can_stop_sharing_even_without_list_price()
    {
        // Currently shared (AllEligible, no exclusion) — Stop sharing stays available.
        var product = CatalogProduct.Create(Org, "SharedNoPrice", UnitOfMeasure.Piece, 0m, Now);
        Assert.False(ConnectedBuyerCatalogProjection.HasValidPoPrice(product));
        Assert.True(ConnectedBuyerCatalogProjection.CanStopSharing(
            CatalogSharingMode.AllEligible, product, true, share: null));
        Assert.False(ConnectedBuyerCatalogProjection.CanShare(
            CatalogSharingMode.AllEligible, product, true, share: null));
        Assert.True(ConnectedBuyerCatalogProjection.IsSelectableForBulk(
            CatalogSharingMode.AllEligible, product, true, share: null));
    }

    [Fact]
    public void MapForManagement_Apple_selling_200_never_needs_price_when_shared()
    {
        var product = CatalogProduct.Create(Org, "Apple", UnitOfMeasure.Piece, 200m, Now);
        var relationship = ConnectedSupplierRelationship.Request(
            PosOrganizationId.From(Guid.NewGuid()),
            Org,
            Now);
        relationship.Approve(Now);
        relationship.ConfigureCatalogSharing(CatalogSharingMode.AllEligible, null, Now);

        var dto = ConnectedSupplierMapper.MapForManagement(
            relationship,
            product,
            share: null,
            exposure: null,
            categoryName: null,
            isInventoryTracked: true);

        Assert.Equal(product.Id.Value, dto.SupplierProductId);
        Assert.Equal(200m, dto.SellingPrice);
        Assert.Null(dto.DefaultPoPrice);
        Assert.Equal(200m, dto.ResolvedPoPrice);
        Assert.True(dto.HasValidPoPrice);
        Assert.True(dto.IsInventoryTracked);
        Assert.True(dto.IsEligible);
        Assert.True(dto.IsEffectivelyShared);
        Assert.False(dto.IsExplicitlyExcluded);
        Assert.False(dto.CanShare);
        Assert.True(dto.CanStopSharing);
        Assert.Equal("Shared", dto.SharingStatus);
        Assert.Equal(200m, dto.EffectiveSupplierOrderPrice);
    }

    [Fact]
    public void HasSharedCatalog_AllEligible_ignores_ineligible_exclusions_via_eligible_only_math()
    {
        Assert.True(ConnectedSupplierCommerceReadiness.HasSharedCatalog(
            CatalogSharingMode.AllEligible, eligibleCount: 9, explicitSharedCount: 0, excludedCount: 0));
        Assert.False(ConnectedSupplierCommerceReadiness.HasSharedCatalog(
            CatalogSharingMode.AllEligible, eligibleCount: 9, explicitSharedCount: 0, excludedCount: 9));
    }
}
