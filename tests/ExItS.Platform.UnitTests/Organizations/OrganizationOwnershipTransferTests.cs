using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.UnitTests.Organizations;

public sealed class OrganizationOwnershipTransferTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_pending_transfer_with_7_day_expiry()
    {
        var from = PlatformUserId.New();
        var to = PlatformUserId.New();
        var transfer = OrganizationOwnershipTransfer.Create(
            PlatformOrganizationId.New(),
            from,
            to,
            T0);

        Assert.Equal(OrganizationOwnershipTransferStatus.Pending, transfer.Status);
        Assert.Equal(T0.AddDays(7), transfer.ExpiresAtUtc);
        Assert.Equal(from, transfer.FromOwnerUserId);
        Assert.Equal(to, transfer.ToUserId);
    }

    [Fact]
    public void Create_rejects_self_transfer()
    {
        var user = PlatformUserId.New();
        var ex = Assert.Throws<DomainException>(() =>
            OrganizationOwnershipTransfer.Create(PlatformOrganizationId.New(), user, user, T0));
        Assert.Equal(DomainErrorCodes.OwnershipTransferSelfDenied, ex.ErrorCode);
    }

    [Fact]
    public void Accept_decline_cancel_and_expire_transitions()
    {
        var from = PlatformUserId.New();
        var to = PlatformUserId.New();
        var org = PlatformOrganizationId.New();

        var accept = OrganizationOwnershipTransfer.Create(org, from, to, T0);
        accept.Accept(to, T0.AddHours(1));
        Assert.Equal(OrganizationOwnershipTransferStatus.Accepted, accept.Status);
        Assert.NotNull(accept.AcceptedAtUtc);
        Assert.NotNull(accept.CompletedAtUtc);

        var decline = OrganizationOwnershipTransfer.Create(org, from, to, T0);
        decline.Decline(to, T0.AddHours(1));
        Assert.Equal(OrganizationOwnershipTransferStatus.Declined, decline.Status);

        var cancel = OrganizationOwnershipTransfer.Create(org, from, to, T0);
        cancel.Cancel(from, T0.AddHours(1));
        Assert.Equal(OrganizationOwnershipTransferStatus.Cancelled, cancel.Status);

        var expire = OrganizationOwnershipTransfer.Create(org, from, to, T0);
        expire.MarkExpired(T0.AddDays(8));
        Assert.Equal(OrganizationOwnershipTransferStatus.Expired, expire.Status);
    }

    [Fact]
    public void Wrong_actor_cannot_accept_or_decline_or_cancel()
    {
        var from = PlatformUserId.New();
        var to = PlatformUserId.New();
        var other = PlatformUserId.New();
        var transfer = OrganizationOwnershipTransfer.Create(PlatformOrganizationId.New(), from, to, T0);

        Assert.Equal(
            DomainErrorCodes.OwnershipTransferActorMismatch,
            Assert.Throws<DomainException>(() => transfer.Accept(other, T0.AddMinutes(1))).ErrorCode);
        Assert.Equal(
            DomainErrorCodes.OwnershipTransferActorMismatch,
            Assert.Throws<DomainException>(() => transfer.Decline(other, T0.AddMinutes(1))).ErrorCode);
        Assert.Equal(
            DomainErrorCodes.OwnershipTransferActorMismatch,
            Assert.Throws<DomainException>(() => transfer.Cancel(other, T0.AddMinutes(1))).ErrorCode);
    }

    [Fact]
    public void Expired_pending_rejects_accept()
    {
        var from = PlatformUserId.New();
        var to = PlatformUserId.New();
        var transfer = OrganizationOwnershipTransfer.Create(PlatformOrganizationId.New(), from, to, T0);
        var ex = Assert.Throws<DomainException>(() => transfer.Accept(to, T0.AddDays(8)));
        Assert.Equal(DomainErrorCodes.OwnershipTransferExpired, ex.ErrorCode);
        Assert.Equal(OrganizationOwnershipTransferStatus.Expired, transfer.Status);
    }
}
