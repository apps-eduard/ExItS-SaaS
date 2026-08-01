using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Identity;

public sealed class PlatformAuthSessionTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 31, 12, 0, 0, TimeSpan.Zero);

    private static PlatformAuthSession CreateSession(
        AccountClass accountClass = AccountClass.Personal,
        PlatformOrganizationId? organizationId = null) =>
        PlatformAuthSession.Create(
            PlatformUserId.New(),
            AccountProfileId.New(),
            accountClass,
            tokenHash: Convert.ToHexString(System.Security.Cryptography.SHA256.HashData("token"u8.ToArray())),
            securityStampAtIssue: Guid.NewGuid().ToString("N"),
            utcNow: T0,
            idleLifetime: TimeSpan.FromMinutes(30),
            absoluteLifetime: TimeSpan.FromHours(12),
            selectedOrganizationId: organizationId);

    [Fact]
    public void Create_and_activity_respects_absolute_cap()
    {
        var session = PlatformAuthSession.Create(
            PlatformUserId.New(),
            AccountProfileId.New(),
            AccountClass.Platform,
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
        Assert.Equal(AccountClass.Platform, session.AccountClass);
        Assert.Equal(AllowedScope.Platform, session.AllowedScope);
    }

    [Fact]
    public void Revoke_deactivates_session()
    {
        var session = CreateSession();
        session.Revoke(T0.AddMinutes(1));
        Assert.False(session.IsActive(T0.AddMinutes(2)));
    }

    [Fact]
    public void Select_and_clear_organization_context_requires_organization_class()
    {
        var session = CreateSession(AccountClass.Organization);
        var organizationId = PlatformOrganizationId.New();
        session.SelectOrganization(organizationId);
        Assert.Equal(organizationId, session.SelectedOrganizationId);

        session.ClearSelectedOrganization();
        Assert.Null(session.SelectedOrganizationId);
    }

    [Fact]
    public void Personal_session_cannot_select_organization()
    {
        var session = CreateSession(AccountClass.Personal);
        Assert.Throws<DomainException>(() =>
            session.SelectOrganization(PlatformOrganizationId.New()));
    }
}
