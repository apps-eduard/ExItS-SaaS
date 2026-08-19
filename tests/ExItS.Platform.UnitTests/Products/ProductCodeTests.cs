using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.UnitTests.Products;

public sealed class ProductCodeTests
{
    [Theory]
    [InlineData("other-product", "other-product")]
    [InlineData("Other-Product", "other-product")]
    [InlineData(" pinoy-business-pos ", "pinoy-business-pos")]
    public void Create_normalizes_valid_codes(string input, string expected)
    {
        var code = ProductCode.Create(input);
        Assert.Equal(expected, code.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("-other-product")]
    [InlineData("pinoy_business_pos")]
    [InlineData("pinoy--pos")]
    public void Create_rejects_invalid_codes(string input)
    {
        var ex = Assert.Throws<DomainException>(() => ProductCode.Create(input));
        Assert.Equal(DomainErrorCodes.InvalidProductCode, ex.ErrorCode);
    }

    [Fact]
    public void Equality_is_stable_on_normalized_value()
    {
        var a = ProductCode.Create("Other-Product");
        var b = ProductCode.Create("other-product");
        Assert.Equal(a, b);
        Assert.Equal("other-product", a.Value);
        Assert.Equal(ProductCode.PinoyBusinessPos, ProductCode.Create("pinoy-business-pos").Value);
        Assert.Equal(ProductCode.PinoyLoanManager, ProductCode.Create("pinoy-loan-manager").Value);
    }

    [Fact]
    public void PinoyLoanManager_constant_is_independent_valid_product_code()
    {
        Assert.Equal("pinoy-loan-manager", ProductCode.PinoyLoanManager);
        Assert.NotEqual(ProductCode.PinoyBusinessPos, ProductCode.PinoyLoanManager);
        var code = ProductCode.Create(ProductCode.PinoyLoanManager);
        Assert.Equal("pinoy-loan-manager", code.Value);
        Assert.Equal(code, ProductCode.Create("Pinoy-Loan-Manager"));
    }

    [Fact]
    public void ProductAccess_grant_and_revoke()
    {
        var utc = new DateTimeOffset(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);
        var access = ProductAccess.Grant(
            PlatformOrganizationId.New(),
            ProductCode.Create("other-product"),
            utc,
            PlatformUserId.New());

        Assert.Equal(ProductAccessStatus.Active, access.Status);
        access.Revoke(utc.AddMinutes(1));
        Assert.Equal(ProductAccessStatus.Revoked, access.Status);
    }
}
