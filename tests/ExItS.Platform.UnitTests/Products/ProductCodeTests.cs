using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.UnitTests.Products;

public sealed class ProductCodeTests
{
    [Theory]
    [InlineData("healthcare", "healthcare")]
    [InlineData("HealthCare", "healthcare")]
    [InlineData(" pinoy-business-pos ", "pinoy-business-pos")]
    public void Create_normalizes_valid_codes(string input, string expected)
    {
        var code = ProductCode.Create(input);
        Assert.Equal(expected, code.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("-healthcare")]
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
        var a = ProductCode.Create("HealthCare");
        var b = ProductCode.Create("healthcare");
        Assert.Equal(a, b);
        Assert.Equal(ProductCode.HealthCare, a.Value);
        Assert.Equal(ProductCode.PinoyBusinessPos, ProductCode.Create("pinoy-business-pos").Value);
    }

    [Fact]
    public void ProductAccess_grant_and_revoke()
    {
        var utc = new DateTimeOffset(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);
        var access = ProductAccess.Grant(
            PlatformOrganizationId.New(),
            ProductCode.Create(ProductCode.HealthCare),
            utc,
            PlatformUserId.New());

        Assert.Equal(ProductAccessStatus.Active, access.Status);
        access.Revoke(utc.AddMinutes(1));
        Assert.Equal(ProductAccessStatus.Revoked, access.Status);
    }
}
