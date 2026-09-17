using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.UnitTests.Payments;

public sealed class BusinessCreditPaymentAllocatorTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-09-01T10:00:00Z");
    private static readonly DateTimeOffset T1 = DateTimeOffset.Parse("2026-09-02T10:00:00Z");
    private static readonly Guid IdA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid IdB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static BusinessCreditPaymentAllocator.OpenReceivable Rec(
        Guid id,
        decimal remaining,
        DateOnly? due,
        DateTimeOffset created) =>
        new(id, remaining, due, created, "remark");

    [Fact]
    public void Auto_500_across_747_and_1088_leaves_two_open()
    {
        var open = new[]
        {
            Rec(IdA, 747m, new DateOnly(2026, 9, 10), T0),
            Rec(IdB, 1088m, new DateOnly(2026, 9, 20), T1),
        };
        var lines = BusinessCreditPaymentAllocator.AllocateAutomatically(open, 500m);
        Assert.Single(lines);
        Assert.Equal(IdA, lines[0].CreditEntryId);
        Assert.Equal(500m, lines[0].Amount);
        Assert.Equal(2, open.Count(o => o.RemainingUnpaidAmount - lines.Where(l => l.CreditEntryId == o.CreditEntryId).Sum(l => l.Amount) > 0));
    }

    [Fact]
    public void Auto_750_across_747_and_1088_pays_first_fully_and_applies_3_to_second()
    {
        var open = new[]
        {
            Rec(IdA, 747m, new DateOnly(2026, 9, 10), T0),
            Rec(IdB, 1088m, new DateOnly(2026, 9, 20), T1),
        };
        var lines = BusinessCreditPaymentAllocator.AllocateAutomatically(open, 750m);
        Assert.Equal(2, lines.Count);
        Assert.Equal(747m, lines.Single(l => l.CreditEntryId == IdA).Amount);
        Assert.Equal(3m, lines.Single(l => l.CreditEntryId == IdB).Amount);
        var remainingB = 1088m - 3m;
        Assert.Equal(1085m, remainingB);
    }

    [Fact]
    public void Auto_exact_full_payment_clears_all()
    {
        var open = new[]
        {
            Rec(IdA, 747m, new DateOnly(2026, 9, 10), T0),
            Rec(IdB, 1088m, new DateOnly(2026, 9, 20), T1),
        };
        var lines = BusinessCreditPaymentAllocator.AllocateAutomatically(open, 1835m);
        Assert.Equal(2, lines.Count);
        Assert.Equal(1835m, lines.Sum(l => l.Amount));
    }

    [Fact]
    public void Auto_prefers_older_due_date_before_created_order()
    {
        var open = new[]
        {
            Rec(IdB, 100m, new DateOnly(2026, 9, 20), T0), // older created, later due
            Rec(IdA, 100m, new DateOnly(2026, 9, 10), T1), // newer created, earlier due
        };
        var lines = BusinessCreditPaymentAllocator.AllocateAutomatically(open, 50m);
        Assert.Single(lines);
        Assert.Equal(IdA, lines[0].CreditEntryId);
    }

    [Fact]
    public void Manual_single_and_multi_validate()
    {
        var open = new[]
        {
            Rec(IdA, 747m, new DateOnly(2026, 9, 10), T0),
            Rec(IdB, 1088m, new DateOnly(2026, 9, 20), T1),
        };
        var single = BusinessCreditPaymentAllocator.ValidateManual(
            open,
            [new BusinessCreditPaymentAllocator.AllocationLine(IdB, 100m)],
            100m);
        Assert.Single(single);
        Assert.Equal(IdB, single[0].CreditEntryId);

        var multi = BusinessCreditPaymentAllocator.ValidateManual(
            open,
            [
                new BusinessCreditPaymentAllocator.AllocationLine(IdA, 200m),
                new BusinessCreditPaymentAllocator.AllocationLine(IdB, 300m),
            ],
            500m);
        Assert.Equal(2, multi.Count);
    }

    [Fact]
    public void Manual_rejects_over_allocation_and_sum_mismatch()
    {
        var open = new[]
        {
            Rec(IdA, 747m, new DateOnly(2026, 9, 10), T0),
            Rec(IdB, 1088m, new DateOnly(2026, 9, 20), T1),
        };

        var over = Assert.Throws<DomainException>(() =>
            BusinessCreditPaymentAllocator.ValidateManual(
                open,
                [new BusinessCreditPaymentAllocator.AllocationLine(IdA, 800m)],
                800m));
        Assert.Equal(DomainErrorCodes.RepaymentAllocationExceedsReceivable, over.ErrorCode);

        var mismatch = Assert.Throws<DomainException>(() =>
            BusinessCreditPaymentAllocator.ValidateManual(
                open,
                [new BusinessCreditPaymentAllocator.AllocationLine(IdA, 100m)],
                200m));
        Assert.Equal(DomainErrorCodes.RepaymentAllocationSumMismatch, mismatch.ErrorCode);
    }

    [Fact]
    public void Auto_rejects_overpayment()
    {
        var open = new[]
        {
            Rec(IdA, 747m, new DateOnly(2026, 9, 10), T0),
        };
        var ex = Assert.Throws<DomainException>(() =>
            BusinessCreditPaymentAllocator.AllocateAutomatically(open, 800m));
        Assert.Equal(DomainErrorCodes.RepaymentExceedsOutstanding, ex.ErrorCode);
    }
}
