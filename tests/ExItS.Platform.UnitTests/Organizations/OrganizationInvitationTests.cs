using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.UnitTests.Support;
using ExItS.Platform.UnitTests.TestSupport;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class OrganizationInvitationTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateOrganizationInvitation_does_not_provision_a_user()
    {
        var clock = new FixedClock(T0);
        var uow = new NoOpUnitOfWork();
        var users = new InMemoryPlatformUserRepository();
        var orgs = new InMemoryPlatformOrganizationRepository();
        var invitations = new InMemoryOrganizationInvitationRepository();
        var messages = new CapturingAuthOutboundMessageSink();

        var org = (await new CreatePlatformOrganization(orgs, new FakePublicOrganizationIdGenerator(), uow, clock)
            .ExecuteAsync("Invite Org", "invite-org")).Value!;
        var usersBefore = users.AddCount;

        var create = new CreateOrganizationInvitation(
            orgs,
            invitations,
            users,
            new FakePublicOrganizationIdGenerator(),
            messages,
            uow,
            clock);

        var result = await create.ExecuteAsync(
            org.Id,
            "new.staff@example.com",
            OrganizationRole.OrganizationMember,
            invitedByUserId: null,
            actorMembershipRole: OrganizationRole.OrganizationOwner,
            actorHasPlatformManageMemberships: true);

        Assert.True(result.IsSuccess, result.ErrorMessage);
        Assert.Equal(usersBefore, users.AddCount);
        Assert.NotNull(result.Value!.AcceptToken);
        Assert.Equal(InvitationStatus.Pending.ToString(), result.Value.Status);
        Assert.Null(await users.GetByNormalizedEmailAsync("new.staff@example.com"));

        var outbound = messages.LastOfKind(PlatformAuthOutboundMessageKinds.OrganizationStaffInvitation);
        Assert.NotNull(outbound);
        Assert.Equal("new.staff@example.com", outbound!.ContactEmail);
        Assert.Equal("Invite Org", outbound.OrganizationName);
        Assert.Equal("Staff", outbound.RoleDisplay);
        Assert.Equal(result.Value.AcceptToken, outbound.OpaqueToken);
        Assert.Null(outbound.StaffLogin);
    }

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

    [Fact]
    public void Create_stores_invitee_snapshot_and_keeps_org_role_separate_from_product_role()
    {
        var (invitation, _) = OrganizationInvitation.Create(
            PlatformOrganizationId.New(),
            "cashier@example.com",
            OrganizationRole.OrganizationMember,
            T0,
            inviteeDisplayName: "Ana Reyes",
            firstName: "Ana",
            lastName: "Reyes",
            branch: "Makati",
            productRole: "Cashier");

        Assert.Equal(OrganizationRole.OrganizationMember, invitation.Role);
        Assert.Equal("Staff", OrganizationRoleDisplay.ToDisplayLabel(invitation.Role));
        Assert.Equal("Ana Reyes", invitation.InviteeDisplayName);
        Assert.Equal("Ana", invitation.FirstName);
        Assert.Equal("Reyes", invitation.LastName);
        Assert.Equal("Makati", invitation.Branch);
        Assert.Equal("Cashier", invitation.ProductRole);
        Assert.NotEqual(invitation.Role.ToString(), invitation.ProductRole);
    }
}
