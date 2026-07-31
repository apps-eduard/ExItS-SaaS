using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class PlatformCredentialTokenTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 31, 14, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Token_is_single_use_and_expires()
    {
        var token = PlatformCredentialToken.Create(
            PlatformUserId.New(),
            PlatformCredentialTokenPurpose.PasswordReset,
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData("tok"u8.ToArray())),
            T0,
            TimeSpan.FromMinutes(30));

        Assert.True(token.IsRedeemable(T0.AddMinutes(1)));
        token.Consume(T0.AddMinutes(1));
        Assert.False(token.IsRedeemable(T0.AddMinutes(2)));
    }
}
