using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;

namespace ExItS.PinoyBusinessPOS.UnitTests.Payments;

public sealed class RepaymentCheckClearingTests
{
    private static readonly PosOrganizationId OrgA = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly POSCustomerId CustomerA = POSCustomerId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly Guid Actor = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid Actor2 = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-15T12:00:00Z");

    [Fact]
    public void Cash_reduces_outstanding_immediately()
    {
        var repayment = Repayment.Create(OrgA, CustomerA, 100m, null, Actor, Now);
        Assert.Equal(UtangPaymentMethod.Cash, repayment.PaymentMethod);
        Assert.Equal(UtangCheckClearingStatus.None, repayment.CheckClearingStatus);
        Assert.True(repayment.ReducesOutstanding);
    }

    [Fact]
    public void Check_pending_does_not_reduce_outstanding()
    {
        var repayment = Repayment.Create(
            OrgA,
            CustomerA,
            500m,
            "Check",
            Actor,
            Now,
            paymentMethod: UtangPaymentMethod.Check,
            checkNumber: "001245",
            bankName: "BDO",
            checkDate: new DateOnly(2026, 9, 20));

        Assert.Equal(UtangCheckClearingStatus.PendingClearing, repayment.CheckClearingStatus);
        Assert.False(repayment.ReducesOutstanding);
        Assert.Equal("001245", repayment.CheckNumber);
        Assert.Equal("BDO", repayment.BankName);
    }

    [Fact]
    public void Check_cleared_reduces_outstanding_exactly_once_and_is_idempotent()
    {
        var repayment = CreatePendingCheck();
        Assert.True(repayment.MarkCleared(Actor2, Now.AddHours(1)));
        Assert.True(repayment.ReducesOutstanding);
        Assert.Equal(Actor2, repayment.ClearedBy);
        Assert.NotNull(repayment.ClearedAtUtc);

        Assert.False(repayment.MarkCleared(Actor, Now.AddHours(2)));
        Assert.Equal(Actor2, repayment.ClearedBy);
    }

    [Fact]
    public void Bounced_and_cancelled_do_not_reduce_outstanding()
    {
        var bounced = CreatePendingCheck();
        bounced.MarkBounced(Actor2, Now.AddMinutes(1), "NSF");
        Assert.False(bounced.ReducesOutstanding);
        Assert.Equal(UtangCheckClearingStatus.Bounced, bounced.CheckClearingStatus);
        Assert.Equal("NSF", bounced.BounceReason);

        var cancelled = CreatePendingCheck();
        cancelled.CancelCheck(Actor2, Now.AddMinutes(1), "Void");
        Assert.False(cancelled.ReducesOutstanding);
        Assert.Equal(UtangCheckClearingStatus.Cancelled, cancelled.CheckClearingStatus);
    }

    [Fact]
    public void Invalid_clearing_transitions_are_rejected()
    {
        var bounced = CreatePendingCheck();
        bounced.MarkBounced(Actor2, Now.AddMinutes(1));
        var ex = Assert.Throws<DomainException>(() => bounced.MarkCleared(Actor, Now.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.InvalidUtangCheckClearingTransition, ex.ErrorCode);

        var cash = Repayment.Create(OrgA, CustomerA, 10m, null, Actor, Now);
        Assert.Equal(
            DomainErrorCodes.InvalidUtangCheckClearingTransition,
            Assert.Throws<DomainException>(() => cash.MarkCleared(Actor, Now.AddMinutes(1))).ErrorCode);
    }

    [Fact]
    public void Business_check_follows_same_semantics()
    {
        var buyer = PosOrganizationId.From(Guid.Parse("55555555-5555-5555-5555-555555555555"));
        var connectionId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var repayment = BusinessRepayment.Create(
            OrgA,
            buyer,
            connectionId,
            200m,
            null,
            Actor,
            Now,
            paymentMethod: UtangPaymentMethod.Check,
            checkNumber: "99",
            bankName: "BPI",
            checkDate: new DateOnly(2026, 9, 21));

        Assert.False(repayment.ReducesOutstanding);
        Assert.True(repayment.MarkCleared(Actor2, Now.AddHours(1)));
        Assert.True(repayment.ReducesOutstanding);
    }

    private static Repayment CreatePendingCheck() =>
        Repayment.Create(
            OrgA,
            CustomerA,
            500m,
            null,
            Actor,
            Now,
            paymentMethod: UtangPaymentMethod.Check,
            checkNumber: "001245",
            bankName: "BDO",
            checkDate: new DateOnly(2026, 9, 20));
}
