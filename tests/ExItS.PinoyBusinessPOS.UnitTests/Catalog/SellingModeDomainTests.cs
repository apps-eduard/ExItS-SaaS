using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class SellingModeDomainTests
{
    private static readonly PosOrganizationId Org =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    private static readonly Guid GlobalProductId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TemplateId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-08-05T00:00:00Z");

    [Fact]
    public void CatalogProduct_Create_defaults_SellingMode_PerItem()
    {
        var product = CatalogProduct.Create(Org, "Custom", UnitOfMeasure.Piece, 10m, Now);

        Assert.Equal(SellingMode.PerItem, product.SellingMode);
    }

    [Fact]
    public void CatalogProduct_Create_PerItem_with_Piece_is_valid()
    {
        var product = CatalogProduct.Create(
            Org,
            "Sachet Drink",
            UnitOfMeasure.Piece,
            12m,
            Now,
            sellingMode: SellingMode.PerItem);

        Assert.Equal(SellingMode.PerItem, product.SellingMode);
        Assert.Equal(UnitOfMeasure.Piece, product.UnitOfMeasure);
    }

    [Fact]
    public void CatalogProduct_Create_ByWeight_with_Kilogram_allows_null_barcode()
    {
        var product = CatalogProduct.Create(
            Org,
            "Tomato",
            UnitOfMeasure.Kilogram,
            120m,
            Now,
            barcode: null,
            sellingMode: SellingMode.ByWeight);

        Assert.Equal(SellingMode.ByWeight, product.SellingMode);
        Assert.Equal(UnitOfMeasure.Kilogram, product.UnitOfMeasure);
        Assert.Null(product.Barcode);
    }

    [Fact]
    public void CatalogProduct_Create_ByWeight_with_Bottle_throws_InvalidSellingModeUnit()
    {
        var ex = Assert.Throws<DomainException>(() =>
            CatalogProduct.Create(
                Org,
                "Tomato",
                UnitOfMeasure.Bottle,
                120m,
                Now,
                sellingMode: SellingMode.ByWeight));

        Assert.Equal(DomainErrorCodes.InvalidSellingModeUnit, ex.ErrorCode);
    }

    [Fact]
    public void CreateImportedSnapshot_preserves_ByWeight_Kilogram_and_provenance()
    {
        var sourceCategoryId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var imported = CatalogProduct.CreateImportedSnapshot(
            Org,
            "Tomato",
            UnitOfMeasure.Kilogram,
            120m,
            GlobalProductId,
            CatalogSource.Template,
            Now,
            platformTemplateId: TemplateId,
            sourceGlobalCategoryId: sourceCategoryId,
            sellingMode: SellingMode.ByWeight);

        Assert.Equal(SellingMode.ByWeight, imported.SellingMode);
        Assert.Equal(UnitOfMeasure.Kilogram, imported.UnitOfMeasure);
        Assert.Equal(GlobalProductId, imported.PlatformGlobalProductId);
        Assert.Equal(TemplateId, imported.PlatformTemplateId);
        Assert.Equal(CatalogSource.Template, imported.CatalogSource);
        Assert.Equal(Now, imported.CatalogImportedAt);
        Assert.Equal(sourceCategoryId, imported.SourceGlobalCategoryId);
    }

    [Fact]
    public void UpdateDetails_preserves_SellingMode_when_passed_explicitly_and_rejects_incompatible_unit()
    {
        var product = CatalogProduct.Create(
            Org,
            "Tomato",
            UnitOfMeasure.Kilogram,
            120m,
            Now,
            sellingMode: SellingMode.ByWeight);

        product.UpdateDetails(
            "Tomato Local",
            null,
            null,
            null,
            null,
            UnitOfMeasure.Kilogram,
            130m,
            Now.AddMinutes(1),
            SellingMode.ByWeight);

        Assert.Equal("Tomato Local", product.Name);
        Assert.Equal(130m, product.SellingPrice);
        Assert.Equal(SellingMode.ByWeight, product.SellingMode);

        var ex = Assert.Throws<DomainException>(() =>
            product.UpdateDetails(
                "Tomato Local",
                null,
                null,
                null,
                null,
                UnitOfMeasure.Bottle,
                130m,
                Now.AddMinutes(2),
                SellingMode.ByWeight));

        Assert.Equal(DomainErrorCodes.InvalidSellingModeUnit, ex.ErrorCode);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SellingModes_Parse_blank_defaults_to_PerItem(string? text)
    {
        Assert.Equal(SellingMode.PerItem, SellingModes.Parse(text));
    }
}
