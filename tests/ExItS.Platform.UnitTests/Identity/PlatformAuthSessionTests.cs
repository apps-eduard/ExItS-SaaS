using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class PlatformAuthSessionTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_and_activity_respects_absolute_cap()
    {
        var session = PlatformAuthSession.Create(
            PlatformUserId.New(),
            tokenHash: Convert.ToHexString(System.Security.Cryptography.SHA256.HashData("token"u8.ToArray())),
            securityStampAtIssue: Guid.NewGuid().ToString("N"),
            utcNow: T0,
            idleLifetime: TimeSpan.FromMinutes(30),
            absoluteLifetime: TimeSpan.FromHours(1));

        Assert.True(session.IsActive(T0.AddMinutes(5)));
        session.RecordActivity(T0.AddMinutes(20), TimeSpan.FromMinutes(30));
        Assert.Equal(T0.AddMinutes(50), session.ExpiresAtUtc);

        session.RecordActivity(T0.AddMinutes(45), TimeSpan.FromMinutes(30));
        Assert.Equal(T0.AddHours(1), session.ExpiresAtUtc);
    }

    [Fact]
    public void Revoke_deactivates_session()
    {
        var session = PlatformAuthSession.Create(
            PlatformUserId.New(),
            tokenHash: Convert.ToHexString(System.Security.Cryptography.SHA256.HashData("token"u8.ToArray())),
            securityStampAtIssue: Guid.NewGuid().ToString("N"),
            utcNow: T0,
            idleLifetime: TimeSpan.FromMinutes(30),
            absoluteLifetime: TimeSpan.FromHours(12));

        session.Revoke(T0.AddMinutes(1));
        Assert.False(session.IsActive(T0.AddMinutes(2)));
    }
}
