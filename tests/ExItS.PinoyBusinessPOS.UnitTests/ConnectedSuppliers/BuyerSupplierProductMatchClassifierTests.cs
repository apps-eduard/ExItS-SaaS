using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class BuyerSupplierProductMatchClassifierTests
{
    private static readonly PosOrganizationId Buyer =
        PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly DateTimeOffset Now =
        new(2026, 8, 17, 8, 0, 0, TimeSpan.Zero);

    private const string ValidBarcodeA = "4006381333931";
    private const string ValidBarcodeB = "036000291452";

    [Fact]
    public void Exact_name_sku_barcode_uom_is_ready_and_can_auto_link()
    {
        var product = Product("Premium Rice", "SUP-RICE", UnitOfMeasure.Kilogram, ValidBarcodeA);

        var result = BuyerSupplierProductMatchClassifier.Classify(
            " premium rice ",
            "sup-rice",
            ValidBarcodeA,
            "Kilogram",
            [product]);

        Assert.Equal(BuyerSupplierProductMatchStatus.Ready, result.Status);
        Assert.True(result.CanAutoLink);
        Assert.Equal(product.Id.Value, result.CandidateBuyerProductId);
        Assert.True(result.Evidence.NameMatched);
        Assert.True(result.Evidence.SkuMatched);
        Assert.True(result.Evidence.BarcodeMatched);
        Assert.True(result.Evidence.UnitCompatible);
    }

    [Fact]
    public void Exact_identifiers_with_incompatible_uom_is_review_never_auto_link()
    {
        var product = Product("Premium Rice", "SUP-RICE", UnitOfMeasure.Piece, ValidBarcodeA);

        var result = BuyerSupplierProductMatchClassifier.Classify(
            "Premium Rice",
            "SUP-RICE",
            ValidBarcodeA,
            "Kilogram",
            [product]);

        Assert.Equal(BuyerSupplierProductMatchStatus.Review, result.Status);
        Assert.False(result.CanAutoLink);
        Assert.Equal(product.Id.Value, result.CandidateBuyerProductId);
        Assert.False(result.Evidence.UnitCompatible);
    }

    [Fact]
    public void Name_and_sku_only_is_review_never_auto_link()
    {
        var product = Product("Premium Rice", "SUP-RICE", UnitOfMeasure.Kilogram, barcode: null);

        var result = BuyerSupplierProductMatchClassifier.Classify(
            "Premium Rice",
            "SUP-RICE",
            supplierBarcode: null,
            "Kilogram",
            [product]);

        Assert.Equal(BuyerSupplierProductMatchStatus.Review, result.Status);
        Assert.False(result.CanAutoLink);
        Assert.True(result.Evidence.NameMatched);
        Assert.True(result.Evidence.SkuMatched);
        Assert.False(result.Evidence.BarcodeMatched);
    }

    [Fact]
    public void Name_only_is_weak_review_never_auto_link()
    {
        var product = Product("Premium Rice", "OTHER", UnitOfMeasure.Kilogram);

        var result = BuyerSupplierProductMatchClassifier.Classify(
            "Premium Rice",
            supplierSku: null,
            supplierBarcode: null,
            "Kilogram",
            [product]);

        Assert.Equal(BuyerSupplierProductMatchStatus.Review, result.Status);
        Assert.False(result.CanAutoLink);
        Assert.True(result.Evidence.NameMatched);
        Assert.False(result.Evidence.SkuMatched);
        Assert.False(result.Evidence.BarcodeMatched);
    }

    [Fact]
    public void No_match_is_new()
    {
        var product = Product("Other Item", "OTHER", UnitOfMeasure.Piece, ValidBarcodeB);

        var result = BuyerSupplierProductMatchClassifier.Classify(
            "Premium Rice",
            "SUP-RICE",
            ValidBarcodeA,
            "Kilogram",
            [product]);

        Assert.Equal(BuyerSupplierProductMatchStatus.New, result.Status);
        Assert.False(result.CanAutoLink);
        Assert.Null(result.CandidateBuyerProductId);
    }

    [Fact]
    public void Barcode_points_to_a_sku_points_to_b_is_conflict()
    {
        var productA = Product("Product A", "SKU-A", UnitOfMeasure.Kilogram, ValidBarcodeA);
        var productB = Product("Product B", "SKU-B", UnitOfMeasure.Kilogram, ValidBarcodeB);

        var result = BuyerSupplierProductMatchClassifier.Classify(
            "Something Else",
            "SKU-B",
            ValidBarcodeA,
            "Kilogram",
            [productA, productB]);

        Assert.Equal(BuyerSupplierProductMatchStatus.Conflict, result.Status);
        Assert.False(result.CanAutoLink);
        Assert.Null(result.CandidateBuyerProductId);
    }

    [Fact]
    public void Already_linked_is_already_linked_not_auto_link()
    {
        var product = Product("Premium Rice", "SUP-RICE", UnitOfMeasure.Kilogram, ValidBarcodeA);

        var result = BuyerSupplierProductMatchClassifier.Classify(
            "Premium Rice",
            "SUP-RICE",
            ValidBarcodeA,
            "Kilogram",
            [product],
            existingLinkedBuyerProductId: product.Id.Value);

        Assert.Equal(BuyerSupplierProductMatchStatus.AlreadyLinked, result.Status);
        Assert.False(result.CanAutoLink);
        Assert.Equal(product.Id.Value, result.CandidateBuyerProductId);
    }

    [Fact]
    public void Missing_any_identifier_never_auto_links_even_when_partial_matches()
    {
        var product = Product("Premium Rice", "SUP-RICE", UnitOfMeasure.Kilogram, ValidBarcodeA);

        var missingBarcode = BuyerSupplierProductMatchClassifier.Classify(
            "Premium Rice",
            "SUP-RICE",
            null,
            "Kilogram",
            [product]);
        var missingSku = BuyerSupplierProductMatchClassifier.Classify(
            "Premium Rice",
            null,
            ValidBarcodeA,
            "Kilogram",
            [product]);

        Assert.False(missingBarcode.CanAutoLink);
        Assert.False(missingSku.CanAutoLink);
        Assert.Equal(BuyerSupplierProductMatchStatus.Review, missingBarcode.Status);
        Assert.Equal(BuyerSupplierProductMatchStatus.Review, missingSku.Status);
    }

    private static CatalogProduct Product(
        string name,
        string? sku,
        UnitOfMeasure unitOfMeasure = UnitOfMeasure.Kilogram,
        string? barcode = null,
        decimal sellingPrice = 50m) =>
        CatalogProduct.Create(Buyer, name, unitOfMeasure, sellingPrice, Now, sku: sku, barcode: barcode);
}
