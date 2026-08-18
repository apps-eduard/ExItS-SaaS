using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class DeviceRecoveryCredentialDomainTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 18, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateRotated_preserves_absolute_expiry_boundary()
    {
        var userId = PlatformUserId.New();
        var original = PlatformDeviceRecoveryCredential.Create(
            userId,
            "device-a",
            "hash-1",
            "stamp",
            T0,
            TimeSpan.FromDays(30),
            TimeSpan.FromDays(90));

        var rotated = PlatformDeviceRecoveryCredential.CreateRotated(
            original,
            "hash-2",
            "stamp",
            T0.AddDays(60),
            TimeSpan.FromDays(30));

        Assert.Equal(original.AbsoluteExpiresAtUtc, rotated.AbsoluteExpiresAtUtc);
        Assert.Equal(original.RotationVersion + 1, rotated.RotationVersion);
        Assert.Equal(userId, rotated.UserId);
        Assert.Equal("device-a", rotated.InstallationDeviceId);
    }

    [Fact]
    public void CreateRotated_refreshes_idle_without_sliding_absolute_expiry()
    {
        var userId = PlatformUserId.New();
        var original = PlatformDeviceRecoveryCredential.Create(
            userId,
            "device-a",
            "hash-1",
            "stamp",
            T0,
            TimeSpan.FromDays(30),
            TimeSpan.FromDays(90));
        var exchangeAt = T0.AddDays(10);

        var rotated = PlatformDeviceRecoveryCredential.CreateRotated(
            original,
            "hash-2",
            "stamp",
            exchangeAt,
            TimeSpan.FromDays(30));

        Assert.Equal(exchangeAt.AddDays(30), rotated.IdleExpiresAtUtc);
        Assert.Equal(T0.AddDays(90), rotated.AbsoluteExpiresAtUtc);
    }

    [Fact]
    public void CreateRotated_rejects_revoked_previous()
    {
        var original = PlatformDeviceRecoveryCredential.Create(
            PlatformUserId.New(),
            "device-a",
            "hash-1",
            "stamp",
            T0,
            TimeSpan.FromDays(30),
            TimeSpan.FromDays(90));
        original.Revoke(T0.AddMinutes(1));

        var ex = Assert.Throws<DomainException>(() =>
            PlatformDeviceRecoveryCredential.CreateRotated(
                original,
                "hash-2",
                "stamp",
                T0.AddMinutes(2),
                TimeSpan.FromDays(30)));

        Assert.Equal(DomainErrorCodes.RecoveryCredentialInvalid, ex.ErrorCode);
    }
}
