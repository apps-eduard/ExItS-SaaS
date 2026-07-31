using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class PlatformUserCredentialTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);
    private static readonly PlatformUserId UserId = PlatformUserId.New();

    [Fact]
    public void Create_sets_hash_and_clears_lockout()
    {
        var credential = PlatformUserCredential.Create(UserId, "hash-value", PlatformUserCredential.Pbkdf2Sha256V1, T0);
        Assert.Equal("hash-value", credential.PasswordHash);
        Assert.Equal(0, credential.FailedAccessCount);
        Assert.Null(credential.LockoutEndUtc);
        Assert.False(string.IsNullOrWhiteSpace(credential.SecurityStamp));
    }

    [Fact]
    public void RegisterFailedAccess_locks_after_threshold()
    {
        var credential = PlatformUserCredential.Create(UserId, "hash-value", PlatformUserCredential.Pbkdf2Sha256V1, T0);
        credential.RegisterFailedAccess(3, TimeSpan.FromMinutes(15), T0);
        credential.RegisterFailedAccess(3, TimeSpan.FromMinutes(15), T0.AddSeconds(1));
        Assert.False(credential.IsLockedOut(T0.AddSeconds(2)));
        credential.RegisterFailedAccess(3, TimeSpan.FromMinutes(15), T0.AddSeconds(2));
        Assert.True(credential.IsLockedOut(T0.AddSeconds(3)));
        Assert.Equal(0, credential.FailedAccessCount);
    }

    [Fact]
    public void ReplacePasswordHash_rotates_security_stamp()
    {
        var credential = PlatformUserCredential.Create(UserId, "hash-a", PlatformUserCredential.Pbkdf2Sha256V1, T0);
        var stamp = credential.SecurityStamp;
        credential.ReplacePasswordHash("hash-b", PlatformUserCredential.Pbkdf2Sha256V1, T0.AddMinutes(1));
        Assert.Equal("hash-b", credential.PasswordHash);
        Assert.NotEqual(stamp, credential.SecurityStamp);
    }

    [Fact]
    public void Create_rejects_empty_hash()
    {
        var ex = Assert.Throws<DomainException>(() =>
            PlatformUserCredential.Create(UserId, " ", PlatformUserCredential.Pbkdf2Sha256V1, T0));
        Assert.Equal(DomainErrorCodes.InvalidAccountStatusTransition, ex.ErrorCode);
    }
}
