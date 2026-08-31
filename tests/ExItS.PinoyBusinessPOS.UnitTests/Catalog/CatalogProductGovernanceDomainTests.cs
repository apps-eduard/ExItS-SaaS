using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class CatalogProductGovernanceDomainTests
{
    private static readonly PosOrganizationId OrgA =
        PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private static readonly PosBranchId BranchA =
        PosBranchId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-08-31T12:00:00Z");

    [Fact]
    public void PGDF_DOM_01_Create_defaults_OrganizationStandard()
    {
        var product = CatalogProduct.Create(OrgA, "Kopiko", UnitOfMeasure.Sachet, 12m, Now);

        Assert.Equal(CatalogProductScope.OrganizationStandard, product.Scope);
        Assert.Null(product.OriginBranchId);
    }

    [Fact]
    public void PGDF_DOM_02_CreateImportedSnapshot_defaults_OrganizationStandard()
    {
        var product = CatalogProduct.CreateImportedSnapshot(
            OrgA,
            "Imported Rice",
            UnitOfMeasure.Kilogram,
            50m,
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            CatalogSource.GlobalSearch,
            Now);

        Assert.Equal(CatalogProductScope.OrganizationStandard, product.Scope);
        Assert.Null(product.OriginBranchId);
        Assert.Equal(CatalogSource.GlobalSearch, product.CatalogSource);
    }

    [Fact]
    public void PGDF_DOM_03_OrganizationStandard_without_origin_is_valid()
    {
        var product = CatalogProduct.Create(
            OrgA,
            "Standard",
            UnitOfMeasure.Piece,
            1m,
            Now,
            scope: CatalogProductScope.OrganizationStandard,
            originBranchId: null);

        Assert.Equal(CatalogProductScope.OrganizationStandard, product.Scope);
        Assert.Null(product.OriginBranchId);
    }

    [Fact]
    public void PGDF_DOM_04_BranchLocal_with_origin_is_valid()
    {
        var product = CatalogProduct.Create(
            OrgA,
            "Local Bangus",
            UnitOfMeasure.Kilogram,
            180m,
            Now,
            scope: CatalogProductScope.BranchLocal,
            originBranchId: BranchA);

        Assert.Equal(CatalogProductScope.BranchLocal, product.Scope);
        Assert.Equal(BranchA, product.OriginBranchId);
    }

    [Fact]
    public void PGDF_DOM_05_BranchLocal_without_origin_rejected()
    {
        var ex = Assert.Throws<DomainException>(() =>
            CatalogProduct.Create(
                OrgA,
                "Broken Local",
                UnitOfMeasure.Piece,
                1m,
                Now,
                scope: CatalogProductScope.BranchLocal,
                originBranchId: null));

        Assert.Equal(DomainErrorCodes.InvalidCatalogProductOriginBranch, ex.ErrorCode);
    }

    [Fact]
    public void PGDF_DOM_06_OrganizationStandard_with_retained_origin_rehydrates()
    {
        var id = CatalogProductId.New();
        var product = CatalogProduct.Rehydrate(
            id,
            OrgA,
            "Promoted",
            null,
            null,
            null,
            null,
            null,
            UnitOfMeasure.Piece,
            10m,
            CatalogProductStatus.Active,
            Now,
            Now,
            scope: CatalogProductScope.OrganizationStandard,
            originBranchId: BranchA);

        Assert.Equal(CatalogProductScope.OrganizationStandard, product.Scope);
        Assert.Equal(BranchA, product.OriginBranchId);
    }

    [Fact]
    public void PGDF_DOM_07_Unknown_persisted_scope_rejected()
    {
        var ex = Assert.Throws<DomainException>(() => CatalogProductScopes.Parse("Global"));
        Assert.Equal(DomainErrorCodes.InvalidCatalogProductScope, ex.ErrorCode);

        Assert.Throws<DomainException>(() => CatalogProductScopes.Parse("Local"));
        Assert.Throws<DomainException>(() => CatalogProductScopes.Parse("NotAScope"));
    }

    [Fact]
    public void PGDF_DOM_08_Scope_and_origin_survive_rehydrate_round_trip()
    {
        var created = CatalogProduct.Create(
            OrgA,
            "RoundTrip",
            UnitOfMeasure.Piece,
            5m,
            Now,
            scope: CatalogProductScope.BranchLocal,
            originBranchId: BranchA);

        var rehydrated = CatalogProduct.Rehydrate(
            created.Id,
            created.OrganizationId,
            created.Name,
            created.Description,
            created.Sku,
            created.NormalizedSku,
            created.Barcode,
            created.CategoryId,
            created.UnitOfMeasure,
            created.SellingPrice,
            created.Status,
            created.CreatedAtUtc,
            created.UpdatedAtUtc,
            scope: created.Scope,
            originBranchId: created.OriginBranchId);

        Assert.Equal(CatalogProductScope.BranchLocal, rehydrated.Scope);
        Assert.Equal(BranchA, rehydrated.OriginBranchId);
    }

    [Fact]
    public void CatalogProductScopes_codes_exclude_Global_and_Local_aliases()
    {
        Assert.Equal(
            ["OrganizationStandard", "BranchLocal"],
            CatalogProductScopes.Codes);
        Assert.DoesNotContain("Global", CatalogProductScopes.Codes);
        Assert.DoesNotContain("Local", CatalogProductScopes.Codes);
    }
}
