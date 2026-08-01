using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class OrganizationInvitationTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_resend_accept_is_single_use_and_email_bound()
    {
        var orgId = PlatformOrganizationId.New();
        var (invitation, token) = OrganizationInvitation.Create(
            orgId,
            "Ada@Example.com",
            OrganizationRole.OrganizationMember,
            T0);

        Assert.Equal(InvitationStatus.Pending, invitation.Status);
        Assert.Equal("ada@example.com", invitation.NormalizedEmail);
        Assert.Equal(OrganizationInvitation.HashToken(token), invitation.TokenHash);

        var resent = invitation.Resend(T0.AddHours(1));
        Assert.NotEqual(token, resent);
        Assert.Equal(OrganizationInvitation.HashToken(resent), invitation.TokenHash);

        var userId = PlatformUserId.New();
        invitation.Accept(userId, "ada@example.com", T0.AddHours(2));
        Assert.Equal(InvitationStatus.Accepted, invitation.Status);
        Assert.Equal(userId, invitation.AcceptedByUserId);

        var ex = Assert.Throws<DomainException>(() => invitation.Accept(PlatformUserId.New(), "ada@example.com", T0.AddHours(3)));
        Assert.Equal(DomainErrorCodes.InvalidInvitationStatusTransition, ex.ErrorCode);
    }

    [Fact]
    public void Expired_pending_invitation_cannot_be_accepted()
    {
        var (invitation, token) = OrganizationInvitation.Create(
            PlatformOrganizationId.New(),
            "user@example.com",
            OrganizationRole.OrganizationAdministrator,
            T0,
            lifetime: TimeSpan.FromHours(1));

        Assert.True(invitation.IsExpired(T0.AddHours(2)));
        var ex = Assert.Throws<DomainException>(() =>
            invitation.Accept(PlatformUserId.New(), "user@example.com", T0.AddHours(2)));
        Assert.Equal(DomainErrorCodes.InvitationExpired, ex.ErrorCode);
        Assert.Equal(InvitationStatus.Expired, invitation.Status);
        Assert.Equal(OrganizationInvitation.HashToken(token), invitation.TokenHash);
    }

    [Fact]
    public void Revoke_blocks_further_use()
    {
        var (invitation, _) = OrganizationInvitation.Create(
            PlatformOrganizationId.New(),
            "user@example.com",
            OrganizationRole.OrganizationMember,
            T0);
        invitation.Revoke(T0.AddMinutes(5));
        Assert.Equal(InvitationStatus.Revoked, invitation.Status);
        var ex = Assert.Throws<DomainException>(() => invitation.Resend(T0.AddMinutes(6)));
        Assert.Equal(DomainErrorCodes.InvalidInvitationStatusTransition, ex.ErrorCode);
    }
}
