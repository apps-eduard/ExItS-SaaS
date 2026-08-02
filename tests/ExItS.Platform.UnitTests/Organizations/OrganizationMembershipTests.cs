using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class OrganizationMembershipTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset T1 = T0.AddMinutes(5);

    [Fact]
    public void Create_valid_membership()
    {
        var membership = OrganizationMembership.Create(
            PlatformOrganizationId.New(),
            PlatformUserId.New(),
            OrganizationRole.OrganizationOwner,
            T0);

        Assert.Equal(MembershipStatus.Active, membership.Status);
        Assert.Equal(OrganizationRole.OrganizationOwner, membership.Role);
    }

    [Fact]
    public void Create_rejects_null_ids()
    {
        Assert.Throws<ArgumentNullException>(() =>
            OrganizationMembership.Create(null!, PlatformUserId.New(), OrganizationRole.OrganizationMember, T0));
        Assert.Throws<ArgumentNullException>(() =>
            OrganizationMembership.Create(PlatformOrganizationId.New(), null!, OrganizationRole.OrganizationMember, T0));
    }

    [Fact]
    public void Role_change_suspension_reactivation_and_removal()
    {
        var membership = OrganizationMembership.Create(
            PlatformOrganizationId.New(),
            PlatformUserId.New(),
            OrganizationRole.OrganizationMember,
            T0);

        membership.ChangeRole(OrganizationRole.OrganizationAdministrator, T1);
        Assert.Equal(OrganizationRole.OrganizationAdministrator, membership.Role);

        var t2 = T1.AddMinutes(1);
        membership.Suspend(t2);
        Assert.Equal(MembershipStatus.Suspended, membership.Status);

        var t3 = t2.AddMinutes(1);
        membership.Reactivate(t3);
        Assert.Equal(MembershipStatus.Active, membership.Status);

        var t4 = t3.AddMinutes(1);
        membership.Remove(t4, "staff left");
        Assert.Equal(MembershipStatus.Removed, membership.Status);

        var t5 = t4.AddMinutes(1);
        membership.Reactivate(t5, reason: "returned");
        Assert.Equal(MembershipStatus.Active, membership.Status);
        Assert.Null(membership.RemovedAtUtc);
    }

    [Fact]
    public void Deactivated_membership_can_move_to_suspended_and_requires_reason_to_deactivate()
    {
        var membership = OrganizationMembership.Create(
            PlatformOrganizationId.New(),
            PlatformUserId.New(),
            OrganizationRole.OrganizationMember,
            T0);
        Assert.Throws<DomainException>(() => membership.Remove(T1, " "));
        membership.Remove(T1, "exit");
        membership.Suspend(T1.AddMinutes(1), "hold");
        Assert.Equal(MembershipStatus.Suspended, membership.Status);
        Assert.Null(membership.RemovedAtUtc);

        membership.Reactivate(T1.AddMinutes(2));
        membership.Remove(T1.AddMinutes(3), "exit again");
        var role = Assert.Throws<DomainException>(() =>
            membership.ChangeRole(OrganizationRole.OrganizationOwner, T1.AddMinutes(4)));
        Assert.Equal(DomainErrorCodes.MembershipNotActive, role.ErrorCode);
    }
}
