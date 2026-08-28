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
        Assert.True(product.CanBePurchased);
        Assert.True(product.CanBeSold);
        Assert.False(product.CanBeUsedAsIngredient);
        Assert.False(product.IsProduced);
        Assert.Equal(ProductUsageCapabilities.BuyAndSellCode, product.UsagePreset);
    }

    [Fact]
    public void Connected_buyer_availability_does_not_auto_init_retail_and_preserves_staged_price()
    {
        var product = CatalogProduct.Create(OrgA, "Rice", UnitOfMeasure.Kilogram, 55.13m, Now);

        product.EnableConnectedBuyerAvailability(Now.AddMinutes(1));
        Assert.True(product.CanExposeToConnectedBuyers);
        Assert.False(product.IsBlockedFromConnectedBuyers);
        Assert.Null(product.DefaultConnectedPoPrice);

        product.SetDefaultConnectedPoPrice(55.13m, Now.AddMinutes(2));
        product.UpdateSellingPrice(60m, Now.AddMinutes(3));
        product.DisableConnectedBuyerAvailability(Now.AddMinutes(4));
        Assert.True(product.IsBlockedFromConnectedBuyers);
        Assert.Equal(55.13m, product.DefaultConnectedPoPrice);

        product.EnableConnectedBuyerAvailability(Now.AddMinutes(5));
        Assert.False(product.IsBlockedFromConnectedBuyers);
        Assert.Equal(55.13m, product.DefaultConnectedPoPrice);

        product.SetDefaultConnectedPoPrice(48.456m, Now.AddMinutes(6));
        Assert.Equal(48.46m, product.DefaultConnectedPoPrice);
        Assert.Equal(60m, product.SellingPrice);
    }

    [Fact]
    public void Create_product_defaults_to_eligible_for_connected_buyers()
    {
        var product = CatalogProduct.Create(OrgA, "Coke", UnitOfMeasure.Piece, 65m, Now);
        Assert.True(product.CanExposeToConnectedBuyers);
        Assert.False(product.IsBlockedFromConnectedBuyers);
        Assert.Null(product.DefaultConnectedPoPrice);
    }

    [Fact]
    public void Allow_does_not_initialize_default_po_from_selling_price()
    {
        var coke = CatalogProduct.Create(OrgA, "Coke", UnitOfMeasure.Piece, 65m, Now);
        var sprite = CatalogProduct.Create(OrgA, "Sprite", UnitOfMeasure.Piece, 63m, Now);
        var pepsi = CatalogProduct.Create(OrgA, "Pepsi", UnitOfMeasure.Piece, 60m, Now);

        coke.EnableConnectedBuyerAvailability(Now.AddMinutes(1));
        sprite.AllowForConnectedBuyers(Now.AddMinutes(1));
        pepsi.EnableConnectedBuyerAvailability(Now.AddMinutes(1));

        Assert.Null(coke.DefaultConnectedPoPrice);
        Assert.Null(sprite.DefaultConnectedPoPrice);
        Assert.Null(pepsi.DefaultConnectedPoPrice);
        Assert.True(coke.CanExposeToConnectedBuyers);
        Assert.False(sprite.IsBlockedFromConnectedBuyers);
    }

    [Fact]
    public void Default_po_price_can_be_staged_while_blocked()
    {
        var product = CatalogProduct.Create(OrgA, "Coke", UnitOfMeasure.Piece, 100m, Now);
        product.BlockFromConnectedBuyers(Now.AddMinutes(1));
        Assert.False(product.CanExposeToConnectedBuyers);
        Assert.True(product.IsBlockedFromConnectedBuyers);

        product.SetDefaultConnectedPoPrice(88m, Now.AddMinutes(2));
        Assert.True(product.IsBlockedFromConnectedBuyers);
        Assert.Equal(88m, product.DefaultConnectedPoPrice);

        product.AllowForConnectedBuyers(Now.AddMinutes(3));
        Assert.True(product.CanExposeToConnectedBuyers);
        Assert.False(product.IsBlockedFromConnectedBuyers);
        Assert.Equal(88m, product.DefaultConnectedPoPrice);
    }

    [Fact]
    public void Disable_and_reenable_preserves_initialized_default_po_price()
    {
        var product = CatalogProduct.Create(OrgA, "Coke", UnitOfMeasure.Piece, 100m, Now);
        product.SetDefaultConnectedPoPrice(90m, Now.AddMinutes(1));
        product.UpdateSellingPrice(110m, Now.AddMinutes(2));
        product.DisableConnectedBuyerAvailability(Now.AddMinutes(3));
        Assert.False(product.CanExposeToConnectedBuyers);
        Assert.True(product.IsBlockedFromConnectedBuyers);
        Assert.Equal(90m, product.DefaultConnectedPoPrice);

        product.EnableConnectedBuyerAvailability(Now.AddMinutes(4));
        Assert.True(product.CanExposeToConnectedBuyers);
        Assert.False(product.IsBlockedFromConnectedBuyers);
        Assert.Equal(90m, product.DefaultConnectedPoPrice);
        Assert.Equal(110m, product.SellingPrice);
    }

    [Fact]
    public void Re_allow_already_allowed_does_not_overwrite_default_po()
    {
        var product = CatalogProduct.Create(OrgA, "Coke", UnitOfMeasure.Piece, 100m, Now);
        product.SetDefaultConnectedPoPrice(90m, Now.AddMinutes(1));
        product.UpdateSellingPrice(110m, Now.AddMinutes(2));
        product.EnableConnectedBuyerAvailability(Now.AddMinutes(3));
        Assert.Equal(90m, product.DefaultConnectedPoPrice);
        Assert.False(product.IsBlockedFromConnectedBuyers);
    }

    [Fact]
    public void Block_and_allow_keep_can_expose_as_inverse()
    {
        var product = CatalogProduct.Create(OrgA, "Coke", UnitOfMeasure.Piece, 100m, Now);
        Assert.Equal(!product.IsBlockedFromConnectedBuyers, product.CanExposeToConnectedBuyers);

        product.BlockFromConnectedBuyers(Now.AddMinutes(1));
        Assert.True(product.IsBlockedFromConnectedBuyers);
        Assert.False(product.CanExposeToConnectedBuyers);

        product.AllowForConnectedBuyers(Now.AddMinutes(2));
        Assert.False(product.IsBlockedFromConnectedBuyers);
        Assert.True(product.CanExposeToConnectedBuyers);
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
            null,
            UnitOfMeasure.Kilogram,
            60m,
            Now.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.ProductNotActive, ex.ErrorCode);
    }

    [Fact]
    public void UpdateSellingPrice_changes_current_price_only_and_skips_unchanged()
    {
        var product = CatalogProduct.Create(
            OrgA,
            "Tomato",
            UnitOfMeasure.Kilogram,
            120m,
            Now,
            sellingMode: SellingMode.ByWeight);

        Assert.True(product.UpdateSellingPrice(135m, Now.AddMinutes(5)));
        Assert.Equal(135m, product.SellingPrice);
        Assert.Equal(Now.AddMinutes(5), product.UpdatedAtUtc);
        Assert.Equal(SellingMode.ByWeight, product.SellingMode);
        Assert.Equal("Tomato", product.Name);

        Assert.False(product.UpdateSellingPrice(135m, Now.AddMinutes(10)));
        Assert.Equal(Now.AddMinutes(5), product.UpdatedAtUtc);

        Assert.True(product.UpdateSellingPrice(0m, Now.AddMinutes(15)));
        Assert.Equal(0m, product.SellingPrice);

        var negative = Assert.Throws<DomainException>(() => product.UpdateSellingPrice(-1m, Now.AddMinutes(20)));
        Assert.Equal(DomainErrorCodes.InvalidProductSellingPrice, negative.ErrorCode);

        var precision = Assert.Throws<DomainException>(() => product.UpdateSellingPrice(10.123m, Now.AddMinutes(21)));
        Assert.Equal(DomainErrorCodes.InvalidProductSellingPrice, precision.ErrorCode);

        product.Deactivate(Now.AddMinutes(30));
        var inactive = Assert.Throws<DomainException>(() => product.UpdateSellingPrice(11m, Now.AddMinutes(31)));
        Assert.Equal(DomainErrorCodes.ProductNotActive, inactive.ErrorCode);
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
    public void Brand_normalizes_name_for_uniqueness_with_uppercase_invariant_trim()
    {
        var brand = ProductBrand.Create(OrgA, "  Nestle  ", Now);

        Assert.Equal("Nestle", brand.Name);
        Assert.Equal("NESTLE", brand.NormalizedName);
        Assert.Equal(ProductBrandStatus.Active, brand.Status);
        Assert.Equal(brand.NormalizedName, ProductBrand.NormalizeForLookup("nestle"));
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
    public void Brand_requires_name_and_enforces_max_length()
    {
        var blank = Assert.Throws<DomainException>(() => ProductBrand.Create(OrgA, " ", Now));
        Assert.Equal(DomainErrorCodes.InvalidBrandName, blank.ErrorCode);

        var tooLong = Assert.Throws<DomainException>(() =>
            ProductBrand.Create(OrgA, new string('x', ProductBrand.NameMaxLength + 1), Now));
        Assert.Equal(DomainErrorCodes.InvalidBrandName, tooLong.ErrorCode);
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
    public void Inactive_brand_cannot_be_renamed_and_transitions_are_guarded()
    {
        var brand = ProductBrand.Create(OrgA, "Nestle", Now);
        brand.Deactivate(Now.AddMinutes(1));

        var rename = Assert.Throws<DomainException>(() => brand.Rename("Unilever", Now.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.BrandNotActive, rename.ErrorCode);

        var repeat = Assert.Throws<DomainException>(() => brand.Deactivate(Now.AddMinutes(3)));
        Assert.Equal(DomainErrorCodes.InvalidBrandStatusTransition, repeat.ErrorCode);

        brand.Reactivate(Now.AddMinutes(4));
        Assert.Equal(ProductBrandStatus.Active, brand.Status);
    }

    [Fact]
    public void Identity_value_objects_reject_empty_guids()
    {
        Assert.Throws<DomainException>(() => CatalogProductId.From(Guid.Empty));
        Assert.Throws<DomainException>(() => ProductCategoryId.From(Guid.Empty));
        Assert.Throws<DomainException>(() => ProductBrandId.From(Guid.Empty));
        Assert.Throws<DomainException>(() => ProductUnitId.From(Guid.Empty));
    }
}
