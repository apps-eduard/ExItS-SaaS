using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class PlatformAccessTokenTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 31, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Bind_and_clear_product_context()
    {
        var token = PlatformAccessToken.Create(
            PlatformUserId.New(),
            tokenHash: Convert.ToHexString(System.Security.Cryptography.SHA256.HashData("atok"u8.ToArray())),
            securityStampAtIssue: Guid.NewGuid().ToString("N"),
            utcNow: T0,
            lifetime: TimeSpan.FromHours(8));

        var organizationId = PlatformOrganizationId.New();
        token.BindProductContext(organizationId, "pinoy-business-pos");
        Assert.Equal(organizationId, token.OrganizationId);
        Assert.Equal("pinoy-business-pos", token.ProductCode);

        token.ClearProductContext();
        Assert.Null(token.OrganizationId);
        Assert.Null(token.ProductCode);
    }

    [Fact]
    public void Revoke_deactivates_token()
    {
        var token = PlatformAccessToken.Create(
            PlatformUserId.New(),
            tokenHash: Convert.ToHexString(System.Security.Cryptography.SHA256.HashData("atok"u8.ToArray())),
            securityStampAtIssue: Guid.NewGuid().ToString("N"),
            utcNow: T0,
            lifetime: TimeSpan.FromHours(8));

        token.Revoke(T0.AddMinutes(1));
        Assert.False(token.IsActive(T0.AddMinutes(2)));
    }
}
