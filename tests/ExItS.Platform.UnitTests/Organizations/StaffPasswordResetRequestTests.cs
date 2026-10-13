using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class StaffPasswordResetRequestTests
{
    private static readonly DateTimeOffset T0 = new(2026, 10, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Approve_lets_the_password_change_only_after_an_organization_decision()
    {
        var request = Sample();
        var approver = PlatformUserId.New();

        request.Approve(approver, T0.AddHours(1));

        Assert.Equal(StaffPasswordResetRequestStatus.Approved, request.Status);
        Assert.Equal(approver, request.DecidedByUserId);
        request.Complete(T0.AddHours(2));
        Assert.Equal(StaffPasswordResetRequestStatus.Completed, request.Status);
    }

    [Fact]
    public void Staff_member_cannot_approve_their_own_reset()
    {
        var staffUserId = PlatformUserId.New();
        var request = Sample(staffUserId);

        var error = Assert.Throws<DomainException>(() => request.Approve(staffUserId, T0.AddMinutes(1)));

        Assert.Equal(DomainErrorCodes.StaffPasswordResetSelfDenied, error.ErrorCode);
        Assert.Equal(StaffPasswordResetRequestStatus.Pending, request.Status);
    }

    [Fact]
    public void Complete_requires_approval()
    {
        var request = Sample();

        var error = Assert.Throws<DomainException>(() => request.Complete(T0.AddMinutes(1)));

        Assert.Equal(DomainErrorCodes.InvalidStaffPasswordResetStatusTransition, error.ErrorCode);
    }

    private static StaffPasswordResetRequest Sample(PlatformUserId? staffUserId = null) =>
        StaffPasswordResetRequest.Create(
            PlatformOrganizationId.New(),
            staffUserId ?? PlatformUserId.New(),
            OrganizationMembershipId.New(),
            PlatformUserId.New(),
            T0);
}
