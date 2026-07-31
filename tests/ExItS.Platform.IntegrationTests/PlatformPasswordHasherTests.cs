using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Infrastructure.Identity;

namespace ExItS.Platform.IntegrationTests;

public sealed class PlatformPasswordHasherTests
{
    private readonly IPlatformPasswordHasher _hasher = new AspNetCorePlatformPasswordHasher();

    [Fact]
    public void Hash_and_verify_round_trip_uses_aspnet_core_password_hasher()
    {
        var hash = _hasher.HashPassword("Correct-Horse-Battery-9!");
        Assert.Equal(PlatformUserCredential.AspNetCoreIdentityV3, _hasher.Algorithm);
        Assert.Equal(PlatformPasswordVerificationResult.Success, _hasher.VerifyHashedPassword(hash, "Correct-Horse-Battery-9!"));
        Assert.Equal(PlatformPasswordVerificationResult.Failed, _hasher.VerifyHashedPassword(hash, "wrong-password"));
    }

    [Fact]
    public void Different_hashes_for_same_password_due_to_random_salt()
    {
        var a = _hasher.HashPassword("Correct-Horse-Battery-9!");
        var b = _hasher.HashPassword("Correct-Horse-Battery-9!");
        Assert.NotEqual(a, b);
    }
}

public sealed class BootstrapSecretComparerTests
{
    [Fact]
    public void EqualsConfigured_accepts_matching_secret()
    {
        const string secret = "0123456789abcdef0123456789abcdef";
        Assert.True(BootstrapSecretComparer.EqualsConfigured(secret, secret));
        Assert.False(BootstrapSecretComparer.EqualsConfigured(secret, "wrong-secret-value-xxxxxxxxxxxxxxx"));
        Assert.False(BootstrapSecretComparer.EqualsConfigured(secret, null));
    }
}
