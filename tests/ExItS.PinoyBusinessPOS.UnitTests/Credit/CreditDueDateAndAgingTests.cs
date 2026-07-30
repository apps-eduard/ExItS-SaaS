using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Credit;

public sealed class CreditDueDateAndAgingTests
{
    private static readonly PosOrganizationId OrgA = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly POSCustomerId CustomerA = POSCustomerId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-07-01T10:00:00Z");
    private static readonly DateTimeOffset T1 = DateTimeOffset.Parse("2026-07-02T10:00:00Z");
    private static readonly DateTimeOffset T2 = DateTimeOffset.Parse("2026-07-03T10:00:00Z");
    private static readonly DateOnly Effective = new(2026, 7, 30);
    private static readonly Guid Actor = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void Due_date_is_optional_and_can_be_set_changed_and_cleared_with_history()
    {
        var entry = CreditEntry.Create(OrgA, CustomerA, 100m, "Rice", T0);
        Assert.Null(entry.CurrentDueDate);

        var set = CreditDueDateChange.Create(OrgA, entry.Id, CustomerA, null, new DateOnly(2026, 8, 15), "Agreed due date", Actor, T1);
        entry.ApplyCurrentDueDate(set.NewDueDate);
        Assert.Equal(new DateOnly(2026, 8, 15), entry.CurrentDueDate);
        Assert.Equal(100m, entry.Amount);
        Assert.Equal("Rice", entry.Remarks);
        Assert.Equal(T0, entry.CreatedAtUtc);

        var change = CreditDueDateChange.Create(
            OrgA, entry.Id, CustomerA, entry.CurrentDueDate, new DateOnly(2026, 8, 1), "Customer asked earlier", Actor, T2);
        entry.ApplyCurrentDueDate(change.NewDueDate);
        Assert.Equal(new DateOnly(2026, 8, 1), entry.CurrentDueDate);

        var clear = CreditDueDateChange.Create(OrgA, entry.Id, CustomerA, entry.CurrentDueDate, null, "Cleared after talk", Actor, T2.AddMinutes(1));
        entry.ApplyCurrentDueDate(clear.NewDueDate);
        Assert.Null(entry.CurrentDueDate);
    }

    [Fact]
    public void Due_date_change_requires_reason_actor_and_must_differ()
    {
        var entryId = CreditEntryId.New();
        Assert.Equal(
            DomainErrorCodes.InvalidCreditDueDateReason,
            Assert.Throws<DomainException>(() =>
                CreditDueDateChange.Create(OrgA, entryId, CustomerA, null, new DateOnly(2026, 8, 1), "  ", Actor, T0)).ErrorCode);

        Assert.Equal(
            DomainErrorCodes.InvalidCreditDueDateActor,
            Assert.Throws<DomainException>(() =>
                CreditDueDateChange.Create(OrgA, entryId, CustomerA, null, new DateOnly(2026, 8, 1), "ok", Guid.Empty, T0)).ErrorCode);

        Assert.Equal(
            DomainErrorCodes.CreditDueDateUnchanged,
            Assert.Throws<DomainException>(() =>
                CreditDueDateChange.Create(OrgA, entryId, CustomerA, new DateOnly(2026, 8, 1), new DateOnly(2026, 8, 1), "same", Actor, T0)).ErrorCode);
    }

    [Fact]
    public void Reversed_credits_cannot_receive_due_dates_and_are_never_overdue()
    {
        var entry = CreditEntry.Create(OrgA, CustomerA, 50m, "Goods", T0);
        entry.ApplyCurrentDueDate(new DateOnly(2026, 7, 1));
        entry.Reverse("Mistake", T1);

        Assert.Equal(
            DomainErrorCodes.CreditDueDateNotAllowedOnReversed,
            Assert.Throws<DomainException>(() => entry.ApplyCurrentDueDate(new DateOnly(2026, 8, 1))).ErrorCode);

        var aged = CreditFifoAging.AgeCredits([entry], 0m, Effective);
        Assert.Single(aged);
        Assert.Equal(nameof(CreditDueStatus.Reversed), aged[0].DueStatus);
        Assert.False(aged[0].IsOverdue);
        Assert.Equal(0m, aged[0].RemainingUnpaidAmount);
    }

    [Fact]
    public void Fifo_partial_repayment_offsets_oldest_credit_first_with_id_tiebreak()
    {
        var earlyId = CreditEntryId.From(Guid.Parse("00000000-0000-0000-0000-000000000001"));
        var laterSameTimeId = CreditEntryId.From(Guid.Parse("00000000-0000-0000-0000-000000000002"));
        var c1 = CreditEntry.Rehydrate(earlyId, OrgA, CustomerA, 100m, "First", CreditEntryStatus.Active, T0, null, null, new DateOnly(2026, 7, 1));
        var c2 = CreditEntry.Rehydrate(laterSameTimeId, OrgA, CustomerA, 80m, "Second", CreditEntryStatus.Active, T0, null, null, new DateOnly(2026, 7, 10));
        var c3 = CreditEntry.Create(OrgA, CustomerA, 40m, "Third", T1);
        c3.ApplyCurrentDueDate(new DateOnly(2026, 8, 15));

        var aged = CreditFifoAging.AgeCredits([c3, c2, c1], 120m, Effective);
        var byId = aged.ToDictionary(a => a.CreditEntryId);

        Assert.Equal(0m, byId[earlyId.Value].RemainingUnpaidAmount);
        Assert.Equal(nameof(CreditDueStatus.Paid), byId[earlyId.Value].DueStatus);
        Assert.False(byId[earlyId.Value].IsOverdue);

        Assert.Equal(60m, byId[laterSameTimeId.Value].RemainingUnpaidAmount);
        Assert.True(byId[laterSameTimeId.Value].IsOverdue);

        Assert.Equal(40m, byId[c3.Id.Value].RemainingUnpaidAmount);
        Assert.Equal(nameof(CreditDueStatus.Upcoming), byId[c3.Id.Value].DueStatus);
        Assert.False(byId[c3.Id.Value].IsOverdue);

        var summary = CreditFifoAging.BuildCustomerSummary(
            CustomerA.Value, OrgA.Value, aged, 220m, 120m, 3, 1, 4);
        Assert.Equal(100m, summary.OutstandingAmount);
        Assert.Equal(60m, summary.OverdueAmount);
        Assert.Equal(1, summary.OverdueCreditCount);
        Assert.Equal(new DateOnly(2026, 7, 10), summary.EarliestOverdueDate);
        Assert.Equal(new DateOnly(2026, 8, 15), summary.NextUpcomingDueDate);
        Assert.Equal(0, summary.CreditsWithoutDueDateCount);
    }

    [Fact]
    public void Fully_offset_credit_is_not_overdue_and_past_dates_are_overdue_when_unpaid()
    {
        var entry = CreditEntry.Create(OrgA, CustomerA, 75m, "Past due", T0);
        entry.ApplyCurrentDueDate(new DateOnly(2026, 7, 1));

        var paid = CreditFifoAging.AgeCredits([entry], 75m, Effective);
        Assert.Equal(nameof(CreditDueStatus.Paid), paid[0].DueStatus);
        Assert.False(paid[0].IsOverdue);

        var unpaid = CreditFifoAging.AgeCredits([entry], 0m, Effective);
        Assert.Equal(nameof(CreditDueStatus.Overdue), unpaid[0].DueStatus);
        Assert.True(unpaid[0].IsOverdue);
        Assert.Equal(75m, unpaid[0].RemainingUnpaidAmount);
    }

    [Fact]
    public void Due_today_due_soon_and_no_due_date_are_distinct_from_overdue()
    {
        var today = CreditEntry.Create(OrgA, CustomerA, 10m, "Today", T0);
        today.ApplyCurrentDueDate(Effective);
        var soon = CreditEntry.Create(OrgA, CustomerA, 10m, "Soon", T1);
        soon.ApplyCurrentDueDate(Effective.AddDays(3));
        var none = CreditEntry.Create(OrgA, CustomerA, 10m, "None", T2);

        var aged = CreditFifoAging.AgeCredits([today, soon, none], 0m, Effective);
        Assert.Equal(nameof(CreditDueStatus.DueToday), aged.Single(a => a.Remarks == "Today").DueStatus);
        Assert.Equal(nameof(CreditDueStatus.DueSoon), aged.Single(a => a.Remarks == "Soon").DueStatus);
        Assert.Equal(nameof(CreditDueStatus.NoDueDate), aged.Single(a => a.Remarks == "None").DueStatus);
        Assert.All(aged, a => Assert.False(a.IsOverdue));

        var summary = CreditFifoAging.BuildCustomerSummary(
            CustomerA.Value, OrgA.Value, aged, 30m, 0m, 3, 0, 3);
        Assert.Equal(0m, summary.OverdueAmount);
        Assert.Equal(0, summary.OverdueCreditCount);
        Assert.Equal(1, summary.CreditsWithoutDueDateCount);
        Assert.Equal(Effective, summary.NextUpcomingDueDate);
    }

    [Fact]
    public void Effective_business_date_uses_utc_calendar_date()
    {
        Assert.Equal(new DateOnly(2026, 7, 30), CreditFifoAging.EffectiveBusinessDateUtc(DateTimeOffset.Parse("2026-07-30T23:59:00Z")));
        Assert.Equal(new DateOnly(2026, 7, 30), CreditFifoAging.EffectiveBusinessDateUtc(DateTimeOffset.Parse("2026-07-30T12:00:00+08:00")));
    }
}
