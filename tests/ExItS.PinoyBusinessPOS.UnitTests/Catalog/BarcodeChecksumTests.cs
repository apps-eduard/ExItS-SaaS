using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class BarcodeChecksumTests
{
    [Theory]
    [InlineData("96385074")]          // EAN-8
    [InlineData("036000291452")]      // UPC-A
    [InlineData("4006381333931")]     // EAN-13
    [InlineData("00012345600012")]    // GTIN-14
    public void Valid_gs1_barcodes_pass_check_digit(string barcode)
    {
        Assert.True(BarcodeChecksum.IsValid(barcode));
        Assert.Equal(barcode, CatalogProduct.NormalizeOptionalBarcode(barcode));
    }

    [Theory]
    [InlineData("96385075")]
    [InlineData("036000291453")]
    [InlineData("4006381333932")]
    [InlineData("00012345600013")]
    public void Wrong_check_digit_is_rejected(string barcode)
    {
        Assert.False(BarcodeChecksum.IsValid(barcode));
        var ex = Assert.Throws<DomainException>(() => CatalogProduct.NormalizeOptionalBarcode(barcode));
        Assert.Equal(DomainErrorCodes.InvalidProductBarcode, ex.ErrorCode);
    }

    [Theory]
    [InlineData("123456789")]
    [InlineData("1234567890")]
    [InlineData("12345678901")]
    public void Non_gs1_lengths_are_accepted_without_check_digit_verification(string barcode)
    {
        Assert.False(BarcodeChecksum.HasVerifiableCheckDigit(barcode.Length));
        Assert.Equal(barcode, CatalogProduct.NormalizeOptionalBarcode(barcode));
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("123456789012345")]
    public void Lengths_outside_eight_to_fourteen_are_rejected(string barcode)
    {
        var ex = Assert.Throws<DomainException>(() => CatalogProduct.NormalizeOptionalBarcode(barcode));
        Assert.Equal(DomainErrorCodes.InvalidProductBarcode, ex.ErrorCode);
    }

    [Theory]
    [InlineData("4006381-33393")]
    [InlineData("ABCDEFGH")]
    public void Non_digit_barcodes_are_rejected(string barcode)
    {
        var ex = Assert.Throws<DomainException>(() => CatalogProduct.NormalizeOptionalBarcode(barcode));
        Assert.Equal(DomainErrorCodes.InvalidProductBarcode, ex.ErrorCode);
    }

    [Fact]
    public void Compute_check_digit_matches_gs1_reference_payloads()
    {
        Assert.Equal(4, BarcodeChecksum.ComputeCheckDigit("9638507"));
        Assert.Equal(2, BarcodeChecksum.ComputeCheckDigit("03600029145"));
        Assert.Equal(1, BarcodeChecksum.ComputeCheckDigit("400638133393"));
    }

    [Fact]
    public void Blank_barcode_is_optional()
    {
        Assert.Null(CatalogProduct.NormalizeOptionalBarcode(null));
        Assert.Null(CatalogProduct.NormalizeOptionalBarcode("   "));
    }
}
