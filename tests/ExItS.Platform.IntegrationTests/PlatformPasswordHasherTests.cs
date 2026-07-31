using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Infrastructure.Identity;

namespace ExItS.Platform.IntegrationTests;

public sealed class PlatformPasswordHasherTests
{
    private readonly IPlatformPasswordHasher _hasher = new Pbkdf2PlatformPasswordHasher();

    [Fact]
    public void Hash_and_verify_round_trip()
    {
        var hash = _hasher.HashPassword("Correct-Horse-Battery-9!");
        Assert.Equal(PlatformUserCredential.Pbkdf2Sha256V1, _hasher.Algorithm);
        Assert.True(_hasher.VerifyHashedPassword(hash, "Correct-Horse-Battery-9!"));
        Assert.False(_hasher.VerifyHashedPassword(hash, "wrong-password"));
    }
}
