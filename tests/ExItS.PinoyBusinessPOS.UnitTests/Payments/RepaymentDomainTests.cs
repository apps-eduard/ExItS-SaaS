using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;

namespace ExItS.PinoyBusinessPOS.UnitTests.Payments;

public sealed class RepaymentDomainTests
{
    private static readonly PosOrganizationId OrgA = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly POSCustomerId CustomerA = POSCustomerId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly Guid Actor = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-30T12:00:00Z");

    [Fact]
    public void Create_allows_optional_remarks_and_requires_positive_amount()
    {
        var repayment = Repayment.Create(OrgA, CustomerA, 50.25m, null, Actor, Now);
        Assert.Equal(50.25m, repayment.Amount);
        Assert.Null(repayment.Remarks);
        Assert.Equal(RepaymentStatus.Active, repayment.Status);
        Assert.Equal(Actor, repayment.RecordedBy);
    }

    [Fact]
    public void Create_rejects_zero_and_over_scale()
    {
        Assert.Equal(
            DomainErrorCodes.InvalidRepaymentAmount,
            Assert.Throws<DomainException>(() => Repayment.Create(OrgA, CustomerA, 0m, null, Actor, Now)).ErrorCode);
        Assert.Equal(
            DomainErrorCodes.InvalidRepaymentAmount,
            Assert.Throws<DomainException>(() => Repayment.Create(OrgA, CustomerA, 1.001m, null, Actor, Now)).ErrorCode);
    }

    [Fact]
    public void Reverse_requires_reason_and_blocks_duplicate()
    {
        var repayment = Repayment.Create(OrgA, CustomerA, 20m, "Partial", Actor, Now);
        var reverser = Guid.Parse("44444444-4444-4444-4444-444444444444");
        repayment.Reverse("Mistake", reverser, Now.AddMinutes(1));
        Assert.Equal(RepaymentStatus.Reversed, repayment.Status);
        Assert.Equal(reverser, repayment.ReversedBy);

        var again = Assert.Throws<DomainException>(() => repayment.Reverse("Again", reverser, Now.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.InvalidRepaymentStatusTransition, again.ErrorCode);
    }

    [Fact]
    public void Amount_and_remarks_are_immutable_after_create()
    {
        Assert.Null(typeof(Repayment).GetProperty(nameof(Repayment.Amount))!.SetMethod);
        Assert.Null(typeof(Repayment).GetProperty(nameof(Repayment.Remarks))!.SetMethod);
        Assert.Null(typeof(Repayment).GetProperty(nameof(Repayment.OrganizationId))!.SetMethod);
        Assert.Null(typeof(Repayment).GetProperty(nameof(Repayment.CustomerId))!.SetMethod);
    }
}
