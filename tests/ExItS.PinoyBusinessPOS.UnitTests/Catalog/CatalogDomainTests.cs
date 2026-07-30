using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class CatalogDomainTests
{
    private static readonly PosOrganizationId OrgA =
        PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));

    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-30T08:00:00Z");

    [Fact]
    public void Create_product_defaults_to_active_and_trims_name()
    {
        var product = CatalogProduct.Create(OrgA, "  Kopiko Black  ", UnitOfMeasure.Sachet, 12m, Now);

        Assert.Equal("Kopiko Black", product.Name);
        Assert.Equal(CatalogProductStatus.Active, product.Status);
        Assert.Equal(UnitOfMeasure.Sachet, product.UnitOfMeasure);
        Assert.Equal(12m, product.SellingPrice);
        Assert.Null(product.Sku);
        Assert.Null(product.NormalizedSku);
        Assert.Null(product.Barcode);
        Assert.Null(product.CategoryId);
        Assert.Equal(OrgA, product.OrganizationId);
    }

    [Fact]
    public void Create_product_requires_name()
    {
        var ex = Assert.Throws<DomainException>(() =>
            CatalogProduct.Create(OrgA, "   ", UnitOfMeasure.Piece, 1m, Now));
        Assert.Equal(DomainErrorCodes.InvalidProductName, ex.ErrorCode);
    }

    [Fact]
    public void Product_name_and_description_have_length_bounds()
    {
        var longName = new string('x', CatalogProduct.NameMaxLength + 1);
        var nameError = Assert.Throws<DomainException>(() =>
            CatalogProduct.Create(OrgA, longName, UnitOfMeasure.Piece, 1m, Now));
        Assert.Equal(DomainErrorCodes.InvalidProductName, nameError.ErrorCode);

        var longDescription = new string('x', CatalogProduct.DescriptionMaxLength + 1);
        var descriptionError = Assert.Throws<DomainException>(() =>
            CatalogProduct.Create(OrgA, "Rice", UnitOfMeasure.Kilogram, 1m, Now, description: longDescription));
        Assert.Equal(DomainErrorCodes.InvalidProductDescription, descriptionError.ErrorCode);
    }

    [Fact]
    public void Sku_keeps_display_casing_and_normalizes_uppercase()
    {
        var product = CatalogProduct.Create(OrgA, "Rice", UnitOfMeasure.Kilogram, 55m, Now, sku: " rc-25kg_a.1/b ");

        Assert.Equal("rc-25kg_a.1/b", product.Sku);
        Assert.Equal("RC-25KG_A.1/B", product.NormalizedSku);
    }

    [Theory]
    [InlineData("has space")]
    [InlineData("bad*char")]
    [InlineData("semi;colon")]
    public void Sku_rejects_characters_outside_the_allowed_charset(string sku)
    {
        var ex = Assert.Throws<DomainException>(() =>
            CatalogProduct.Create(OrgA, "Rice", UnitOfMeasure.Kilogram, 1m, Now, sku: sku));
        Assert.Equal(DomainErrorCodes.InvalidProductSku, ex.ErrorCode);
    }

    [Fact]
    public void Sku_rejects_values_longer_than_sixty_four_characters()
    {
        var ex = Assert.Throws<DomainException>(() =>
            CatalogProduct.Create(
                OrgA,
                "Rice",
                UnitOfMeasure.Kilogram,
                1m,
                Now,
                sku: new string('A', CatalogProduct.SkuMaxLength + 1)));
        Assert.Equal(DomainErrorCodes.InvalidProductSku, ex.ErrorCode);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1)]
    public void Selling_price_cannot_be_negative(decimal price)
    {
        var ex = Assert.Throws<DomainException>(() =>
            CatalogProduct.Create(OrgA, "Rice", UnitOfMeasure.Kilogram, price, Now));
        Assert.Equal(DomainErrorCodes.InvalidProductSellingPrice, ex.ErrorCode);
    }

    [Fact]
    public void Selling_price_allows_zero_but_rejects_more_than_two_decimals()
    {
        Assert.Equal(0m, CatalogProduct.Create(OrgA, "Sample", UnitOfMeasure.Piece, 0m, Now).SellingPrice);

        var ex = Assert.Throws<DomainException>(() =>
            CatalogProduct.Create(OrgA, "Sample", UnitOfMeasure.Piece, 10.123m, Now));
        Assert.Equal(DomainErrorCodes.InvalidProductSellingPrice, ex.ErrorCode);
    }

    [Fact]
    public void Unit_of_measure_codes_are_controlled_and_case_insensitive_on_parse()
    {
        Assert.Equal(11, UnitOfMeasures.Codes.Count);
        Assert.Equal(UnitOfMeasure.Kilogram, UnitOfMeasures.Parse("kilogram"));
        Assert.Equal("Kilogram", UnitOfMeasures.ToCode(UnitOfMeasure.Kilogram));

        var ex = Assert.Throws<DomainException>(() => UnitOfMeasures.Parse("Crate"));
        Assert.Equal(DomainErrorCodes.InvalidUnitOfMeasure, ex.ErrorCode);
        Assert.False(UnitOfMeasures.TryParse(null, out _));
    }

    [Fact]
    public void Product_deactivate_and_reactivate_guard_repeat_transitions()
    {
        var product = CatalogProduct.Create(OrgA, "Rice", UnitOfMeasure.Kilogram, 55m, Now);

        product.Deactivate(Now.AddMinutes(1));
        Assert.Equal(CatalogProductStatus.Inactive, product.Status);

        var repeat = Assert.Throws<DomainException>(() => product.Deactivate(Now.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.InvalidProductStatusTransition, repeat.ErrorCode);

        product.Reactivate(Now.AddMinutes(3));
        Assert.Equal(CatalogProductStatus.Active, product.Status);

        var repeatReactivate = Assert.Throws<DomainException>(() => product.Reactivate(Now.AddMinutes(4)));
        Assert.Equal(DomainErrorCodes.InvalidProductStatusTransition, repeatReactivate.ErrorCode);
    }

    [Fact]
    public void Inactive_product_cannot_be_edited_before_reactivation()
    {
        var product = CatalogProduct.Create(OrgA, "Rice", UnitOfMeasure.Kilogram, 55m, Now);
        product.Deactivate(Now.AddMinutes(1));

        var ex = Assert.Throws<DomainException>(() => product.UpdateDetails(
            "Rice Premium",
            null,
            null,
            null,
            null,
            UnitOfMeasure.Kilogram,
            60m,
            Now.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.ProductNotActive, ex.ErrorCode);
    }

    [Fact]
    public void Product_has_no_stock_or_sales_state()
    {
        var propertyNames = typeof(CatalogProduct)
            .GetProperties()
            .Select(p => p.Name)
            .ToArray();

        Assert.DoesNotContain(propertyNames, n => n.Contains("Stock", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, n => n.Contains("Quantity", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, n => n.Contains("OnHand", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, n => n.Contains("Cost", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, n => n.Contains("Tax", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, n => n.Contains("Discount", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Category_normalizes_name_for_uniqueness_with_uppercase_invariant_trim()
    {
        var category = ProductCategory.Create(OrgA, "  Beverages  ", Now);

        Assert.Equal("Beverages", category.Name);
        Assert.Equal("BEVERAGES", category.NormalizedName);
        Assert.Equal(ProductCategoryStatus.Active, category.Status);
        Assert.Equal(category.NormalizedName, ProductCategory.NormalizeForLookup("beverages"));
    }

    [Fact]
    public void Category_requires_name_and_enforces_max_length()
    {
        var blank = Assert.Throws<DomainException>(() => ProductCategory.Create(OrgA, " ", Now));
        Assert.Equal(DomainErrorCodes.InvalidCategoryName, blank.ErrorCode);

        var tooLong = Assert.Throws<DomainException>(() =>
            ProductCategory.Create(OrgA, new string('x', ProductCategory.NameMaxLength + 1), Now));
        Assert.Equal(DomainErrorCodes.InvalidCategoryName, tooLong.ErrorCode);
    }

    [Fact]
    public void Inactive_category_cannot_be_renamed_and_transitions_are_guarded()
    {
        var category = ProductCategory.Create(OrgA, "Beverages", Now);
        category.Deactivate(Now.AddMinutes(1));

        var rename = Assert.Throws<DomainException>(() => category.Rename("Drinks", Now.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.CategoryNotActive, rename.ErrorCode);

        var repeat = Assert.Throws<DomainException>(() => category.Deactivate(Now.AddMinutes(3)));
        Assert.Equal(DomainErrorCodes.InvalidCategoryStatusTransition, repeat.ErrorCode);

        category.Reactivate(Now.AddMinutes(4));
        Assert.Equal(ProductCategoryStatus.Active, category.Status);
    }

    [Fact]
    public void Identity_value_objects_reject_empty_guids()
    {
        Assert.Throws<DomainException>(() => CatalogProductId.From(Guid.Empty));
        Assert.Throws<DomainException>(() => ProductCategoryId.From(Guid.Empty));
    }
}
